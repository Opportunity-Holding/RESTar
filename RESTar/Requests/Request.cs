using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using RESTar.Internal;
using RESTar.Internal.Auth;
using static RESTar.Method;
using static RESTar.ErrorCodes;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Meta.Internal;
using RESTar.Resources.Operations;
using RESTar.Results;
using Binary = RESTar.Results.Binary;
using IResource = RESTar.Meta.IResource;

namespace RESTar.Requests
{
    internal class Request<T> : IRequest, IRequest<T>, IEntityRequest<T>, ITraceable where T : class
    {
        private bool IsEvaluating { get; set; }
        public ITarget<T> Target { get; }
        public Type TargetType { get; }
        public bool HasConditions => !(_conditions?.Count > 0);
        private Headers _responseHeaders;
        public Headers ResponseHeaders => _responseHeaders ?? (_responseHeaders = new Headers());
        public Cookies Cookies => Context.Client.Cookies;
        public Headers Headers { get; }
        public CachedProtocolProvider CachedProtocolProvider { get; }
        private Exception Error { get; }
        public bool IsValid => Error == null;
        public IResource<T> Resource { get; }
        public TimeSpan TimeElapsed => Stopwatch.Elapsed;
        private Stopwatch Stopwatch { get; }

        public Func<IEnumerable<T>> Selector { private get; set; }
        public Func<IEnumerable<T>, IEnumerable<T>> Updater { private get; set; }

        public Func<IEnumerable<T>> EntitiesProducer { get; set; }

        IEntityResource<T> IEntityRequest<T>.EntityResource => Resource as IEntityResource<T>;
        Func<IEnumerable<T>, IEnumerable<T>> IEntityRequest<T>.GetUpdater() => Updater;
        IEnumerable<T> IRequest<T>.GetInputEntities() => EntitiesProducer?.Invoke() ?? new T[0];
        Func<IEnumerable<T>> IEntityRequest<T>.GetSelector() => Selector;
        IResource IRequest.Resource => Resource;
        private Method method;

        public Method Method
        {
            get => method;
            set
            {
                if (IsEvaluating) return;
                method = value;
            }
        }

        public TData GetClientData<TData>(string key)
        {
            if (Context.Client.ResourceClientDataMappings.TryGetValue(Resource, out var data) && data.TryGetValue(key, out var value))
                return (TData) value;
            return default;
        }

        public void SetClientData<TData>(string key, TData value)
        {
            if (!Context.Client.ResourceClientDataMappings.TryGetValue(Resource, out var data))
                data = Context.Client.ResourceClientDataMappings[Resource] = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            data[key] = value;
        }

        private List<Condition<T>> _conditions;

        public List<Condition<T>> Conditions
        {
            get => _conditions ?? (_conditions = new List<Condition<T>>());
            set => _conditions = value;
        }

        private MetaConditions _metaConditions;

        public MetaConditions MetaConditions
        {
            get => _metaConditions ?? (_metaConditions = new MetaConditions());
            set => _metaConditions = value;
        }

        private Func<Body> BodyFunc { get; set; }
        private Body _body;

        public Body GetBody()
        {
            if (BodyFunc != null)
            {
                _body = BodyFunc();
                BodyFunc = null;
            }
            return _body;
        }

        private void SetBody(Body value)
        {
            if (IsEvaluating)
                throw new InvalidOperationException("Cannot set the request body whilst the request is evaluating");
            BodyFunc = null;
            _body = value;
        }

        public void SetBody(object content, ContentType? contentType = null) => SetBody(new Body
        (
            stream: content.ToStream(contentType, this),
            protocolProvider: CachedProtocolProvider
        ));

        public IUriComponents UriComponents => new UriComponents
        (
            resourceSpecifier: Resource.Name,
            viewName: Target is IView ? Target.Name : null,
            conditions: Conditions,
            metaConditions: MetaConditions.AsConditionList(),
            protocolProvider: CachedProtocolProvider.ProtocolProvider,
            macro: Parameters.UriComponents.Macro
        );

        #region Parameter bindings

        public RequestParameters Parameters { get; }

        public string TraceId => Parameters.TraceId;
        public Context Context => Parameters.Context;
        public bool IsWebSocketUpgrade => Parameters.IsWebSocketUpgrade;
        public IMacro Macro => Parameters.UriComponents.Macro;

        #endregion

        #region ILogable

        private ILogable LogItem => Parameters;
        MessageType ILogable.MessageType => LogItem.MessageType;
        string ILogable.LogMessage => LogItem.LogMessage;
        string ILogable.LogContent => LogItem.LogContent;
        public DateTime LogTime { get; } = DateTime.Now;

        string ILogable.HeadersStringCache
        {
            get => LogItem.HeadersStringCache;
            set => LogItem.HeadersStringCache = value;
        }

        bool ILogable.ExcludeHeaders => LogItem.ExcludeHeaders;

        #endregion

        public IEntities<T> EvaluateToEntities()
        {
            var result = Evaluate();
            if (result is Error e) throw e;
            return (IEntities<T>) result;
        }

        public IResult Evaluate()
        {
            if (!IsValid) return Error.AsResultOf(this);
            if (!Context.MethodIsAllowed(Method, Resource, out var error)) return error.AsResultOf(this);
            if (IsWebSocketUpgrade)
            {
                try
                {
                    if (!CachedProtocolProvider.ProtocolProvider.IsCompliant(this, out var reason))
                        return new NotCompliantWithProtocol(CachedProtocolProvider.ProtocolProvider, reason).AsResultOf(this);
                }
                catch (NotImplementedException) { }
            }
            if (IsEvaluating) throw new InfiniteLoop();
            var result = RunEvaluation();
            result.Headers.Elapsed = TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            if (Headers.Metadata == "full" && result.Metadata is string metadata)
                result.Headers.Metadata = metadata;
            result.Headers.Version = RESTarConfig.Version;
            if (result is InfiniteLoop loop && !Context.IsBottomOfStack)
                throw loop;
            return result;
        }

        private IResult RunEvaluation()
        {
            try
            {
                Context.IncreaseDepth();
                IsEvaluating = true;

                switch (Resource)
                {
                    case ITerminalResource<T> terminal:
                        if (!Context.HasWebSocket)
                            throw new UpgradeRequired(terminal.Name);
                        if (IsWebSocketUpgrade)
                            return MakeWebSocketUpgrade(terminal);
                        return SwitchTerminal(terminal);

                    case IBinaryResource<T> binary:
                        var (stream, contentType) = binary.SelectBinary(this);
                        return new Binary(this, stream, contentType);

                    case IEntityResource<T> entity:
                        if (entity.RequiresAuthentication)
                            this.RunResourceAuthentication(entity);
                        if (MetaConditions.SafePost != null)
                        {
                            if (!entity.CanSelect) throw new SafePostNotSupported("(no selector implemented)");
                            if (!entity.CanUpdate) throw new SafePostNotSupported("(no updater implemented)");
                        }
                        var result = EntityOperations<T>.GetEvaluator(Method).Invoke(this);
                        ResponseHeaders.ForEach(h => result.Headers[h.Key.StartsWith("X-") ? h.Key : "X-" + h.Key] = h.Value);
                        if (RESTarConfig.AllowAllOrigins)
                            result.Headers.AccessControlAllowOrigin = "*";
                        else if (Headers.Origin is string origin)
                            result.Headers.AccessControlAllowOrigin = origin;
                        if (!IsWebSocketUpgrade) return result;
                        var serialized = result.Serialize();
                        Context.WebSocket.Open();
                        Context.WebSocket.SendResult(serialized);
                        Context.WebSocket.Disconnect();
                        return new WebSocketUpgradeSuccessful(this);

                    case var other: throw new UnknownResource(other.Name);
                }
            }
            catch (Exception exception)
            {
                var result = exception.AsResultOf(this);
                ResponseHeaders.ForEach(h => result.Headers[h.Key.StartsWith("X-") ? h.Key : "X-" + h.Key] = h.Value);
                return result;
            }
            finally
            {
                Context.DecreaseDepth();
                IsEvaluating = false;
            }
        }

        private ISerializedResult SwitchTerminal(ITerminalResource<T> resource)
        {
            var _resource = (Meta.Internal.TerminalResource<T>) resource;
            var newTerminal = _resource.MakeTerminal(Conditions);
            Context.WebSocket.ConnectTo(newTerminal, resource);
            newTerminal.Open();
            return new SwitchedTerminal(this);
        }

        private ISerializedResult MakeWebSocketUpgrade(ITerminalResource<T> resource)
        {
            var _resource = (Meta.Internal.TerminalResource<T>) resource;
            var terminal = _resource.MakeTerminal(Conditions);
            Context.WebSocket.SetContext(this);
            Context.WebSocket.ConnectTo(terminal, resource);
            Context.WebSocket.Open();
            terminal.Open();
            return new WebSocketUpgradeSuccessful(this);
        }

        internal Request(IResource<T> resource, RequestParameters parameters)
        {
            Parameters = parameters;
            Resource = resource;
            Target = resource;
            TargetType = typeof(T);
            Method = parameters.Method;
            Headers = parameters.Headers;
            Stopwatch = parameters.Stopwatch;
            CachedProtocolProvider = parameters.CachedProtocolProvider;

            try
            {
                if (resource.IsInternal && Context.Client.Origin != OriginType.Internal)
                    throw new ResourceIsInternal(resource);
                if (Resource is IEntityResource<T> entityResource)
                {
                    MetaConditions = MetaConditions.Parse(parameters.UriComponents.MetaConditions, entityResource);
                    if (parameters.UriComponents.ViewName != null)
                    {
                        if (!entityResource.ViewDictionary.TryGetValue(parameters.UriComponents.ViewName, out var view))
                            throw new UnknownView(parameters.UriComponents.ViewName, entityResource);
                        Target = view;
                    }
                }
                if (parameters.UriComponents.Conditions.Count > 0)
                    Conditions = Condition<T>.Parse(parameters.UriComponents.Conditions, Target);
                if (parameters.Headers.UnsafeOverride)
                {
                    MetaConditions.Unsafe = true;
                    parameters.Headers.UnsafeOverride = false;
                }
                var defaultContentType = CachedProtocolProvider.DefaultInputProvider.ContentType;
                if (Headers.Source == null)
                {
                    if (!Parameters.HasBody) return;
                    SetBody(new Body
                    (
                        stream: new RESTarStream
                        (
                            contentType: Headers.ContentType ?? defaultContentType,
                            buffer: Parameters.BodyBytes
                        ),
                        protocolProvider: CachedProtocolProvider
                    ));
                }
                else
                {
                    Body getBodyFromExternalSourceSync()
                    {
                        try
                        {
                            Body body;
                            var source = new HeaderRequestParameters(Headers.Source);
                            if (source.Method != GET) throw new InvalidSyntax(InvalidSource, "Only GET is allowed in Source headers");
                            if (source.IsInternal)
                            {
                                var result = Context.CreateRequest(source.URI, source.Method, null, source.Headers)
                                    .Evaluate();
                                if (!(result is IEntities)) throw new InvalidExternalSource(source.URI, result.LogMessage);
                                var serialized = result.Serialize();
                                if (serialized is NoContent) throw new InvalidExternalSource(source.URI, "Response was empty");
                                body = new Body
                                (
                                    stream: new RESTarStream
                                    (
                                        contentType: serialized.Headers.ContentType ?? CachedProtocolProvider.DefaultInputProvider.ContentType,
                                        stream: serialized.Body
                                    ),
                                    protocolProvider: CachedProtocolProvider
                                );
                            }
                            else
                            {
                                if (source.Headers.Accept == null) source.Headers.Accept = defaultContentType;
                                var request = new HttpRequest(this, source, null);
                                var response = request.GetResponseAsync().Result ?? throw new InvalidExternalSource(source.URI, "No response");
                                if (response.StatusCode >= HttpStatusCode.BadRequest) throw new InvalidExternalSource(source.URI, response.LogMessage);
                                if (response.Body.CanSeek && response.Body.Length == 0)
                                    throw new InvalidExternalSource(source.URI, "Response was empty");
                                body = new Body
                                (
                                    stream: new RESTarStream
                                    (
                                        contentType: response.Headers.ContentType ?? defaultContentType,
                                        stream: response.Body
                                    ),
                                    protocolProvider: CachedProtocolProvider
                                );
                            }
                            return body;
                        }
                        catch (HttpRequestException re)
                        {
                            BodyFunc = null;
                            throw new InvalidSyntax(InvalidSource, $"{re.Message} in the Source header");
                        }
                        catch
                        {
                            BodyFunc = null;
                            throw;
                        }
                    }

                    BodyFunc = getBodyFromExternalSourceSync;
                }
            }
            catch (Exception e)
            {
                Error = e;
            }
        }

        private Request
        (
            Method method,
            IResource<T> resource,
            ITarget<T> target,
            Type targetType,
            CachedProtocolProvider cachedProtocolProvider,
            RequestParameters parameters,
            Body body,
            Headers headers,
            MetaConditions metaConditions,
            List<Condition<T>> conditions,
            Exception error,
            string protocol
        )
        {
            Method = method;
            Resource = resource;
            Target = target;
            TargetType = targetType;
            Parameters = parameters;
            _body = body;
            Headers = headers;
            MetaConditions = metaConditions;
            Conditions = conditions;
            Error = error;
            Stopwatch = Stopwatch.StartNew();
            CachedProtocolProvider = protocol != null
                ? ProtocolController.ResolveProtocolProvider(protocol)
                : cachedProtocolProvider;
        }

        public IRequest GetCopy(string newProtocol = null) => new Request<T>
        (
            method: Method,
            resource: Resource,
            target: Target,
            targetType: TargetType,
            cachedProtocolProvider: CachedProtocolProvider,
            parameters: Parameters,
            body: GetBody().GetCopy(newProtocol),
            headers: Headers.GetCopy(),
            metaConditions: MetaConditions.GetCopy(),
            conditions: Conditions.ToList(),
            error: Error,
            protocol: newProtocol
        );

        public void Dispose()
        {
            BodyFunc = null;
            GetBody().Dispose();
        }
    }
}