﻿using System;
using RESTar.Internal;
using RESTar.Meta;
using RESTar.Results;
using RESTar.WebSockets;
using Starcounter;
using IResource = RESTar.Meta.IResource;
using WebSocket = RESTar.WebSockets.WebSocket;

namespace RESTar.Requests
{
    /// <summary>
    /// Requests are run from inside contexts. Contexts guard against infinite recursion 
    /// and define the root for each ITraceable tree. They also hold WebSocket connections
    /// and Client access rights.
    /// </summary>
    public abstract class Context
    {
        internal string InitialTraceId { get; }
        private const int MaximumStackDepth = 500;
        private WebSocket webSocket;
        private int StackDepth;
        internal bool IsBottomIfStack => StackDepth < 1;

        internal void IncreaseDepth()
        {
            if (StackDepth == MaximumStackDepth)
                throw new InfiniteLoop();
            StackDepth += 1;
        }

        internal void DecreaseDepth()
        {
            StackDepth -= 1;
        }

        /// <summary>
        /// The websocket connected with this context
        /// </summary>
        internal WebSocket WebSocket
        {
            get => webSocket;
            set
            {
                WebSocketController.Add(value);
                webSocket = value;
            }
        }

        #region Abstract

        /// <summary>
        /// Should return true if and only if the request is a WebSocket upgrade request
        /// </summary>
        protected abstract bool IsWebSocketUpgrade { get; }

        /// <summary>
        /// Gets a WebSocket instance for a given Context
        /// </summary>
        protected abstract WebSocket CreateWebSocket();

        #endregion

        /// <summary>
        /// Does this context have a WebSocket connected?
        /// </summary>
        public bool HasWebSocket => WebSocket != null;

        /// <summary>
        /// The client of the context
        /// </summary>
        public Client Client { get; }

        /// <summary>
        /// Creates a request in this context for a resource type, using the given method and optional protocol id and 
        /// view name. If the protocol ID is null, the default protocol will be used. T must be a registered resource type.
        /// </summary>
        /// <param name="method">The method to perform, for example GET</param>
        /// <param name="protocolId">An optional protocol ID, defining the protocol to use for the request. If the 
        /// protocol ID is null, the default protocol will be used.</param>
        /// <param name="viewName">An optional view name to use when selecting entities from the resource</param>
        public virtual IRequest<T> CreateRequest<T>(Method method = Method.GET, string protocolId = null, string viewName = null) where T : class
        {
            var resource = Resource<T>.SafeGet ?? throw new UnknownResource(typeof(T).RESTarTypeName());
            var parameters = new RequestParameters(this, method, resource, protocolId, viewName);
            return new Request<T>(resource, parameters);
        }

        /// <summary>
        /// Creates a request in this context using the given parameters.
        /// </summary>
        /// <param name="uri">The URI of the request</param>
        /// <param name="method">The method to perform</param>
        /// <param name="body">The body of the request</param>
        /// <param name="headers">The headers of the request</param>
        public virtual IRequest CreateRequest(string uri, Method method = Method.GET, byte[] body = null, Headers headers = null)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (IsWebSocketUpgrade)
            {
                WebSocket = CreateWebSocket();
                WebSocket.Context = this;
            }
            var parameters = new RequestParameters(this, method, uri, body, headers);
            if (!parameters.IsValid) return new InvalidParametersRequest(parameters);
            return Construct((dynamic) parameters.IResource, parameters);
        }

        /// <summary>
        /// Validates a request URI and returns true if valid. If invalid, the error is returned in the 
        /// out parameter.
        /// </summary>
        /// <param name="uri">The URI if the request</param>
        /// <param name="error">A RESTarError describing the error, or null if valid</param>
        /// <param name="resource">The resource referenced in the URI</param>
        /// <param name="formattedUri">A properly formatted version of the uri, if valid. Otherwise null.</param>
        public bool UriIsValid(string uri, out Results.Error error, out IResource resource, out string formattedUri)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            var parameters = new RequestParameters(this, (Method) (-1), uri, null, null);
            formattedUri = null;
            if (parameters.Error != null)
            {
                error = parameters.Error.AsError();
                resource = null;
                return false;
            }
            resource = parameters.IResource;
            IRequest request = Construct((dynamic) resource, parameters);
            if (request.IsValid)
            {
                formattedUri = request.UriComponents.ToString();
                error = null;
                return true;
            }
            error = request.Evaluate() as Results.Error;
            return false;
        }

        private static IRequest Construct<T>(IResource<T> r, RequestParameters p) where T : class => new Request<T>(r, p);

        /// <summary>
        /// Use this method to check the origin of an incoming OPTIONS request. This will check the contents
        /// of the Origin header against allowed CORS origins.
        /// </summary>
        /// <param name="uri">The URI of the request</param>
        /// <param name="headers">The headers of the request</param>
        /// <returns></returns>
        public ISerializedResult CheckOrigin(string uri, Headers headers)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            var parameters = new RequestParameters(this, uri, headers);
            var origin = parameters.Headers.Origin;
            if (!parameters.IsValid || !Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
                return new InvalidOrigin();
            if (RESTarConfig.AllowAllOrigins || RESTarConfig.AllowedOrigins.Contains(originUri))
                return new AcceptOrigin(origin, parameters);
            return new InvalidOrigin();
        }

        /// <summary>
        /// Creates a new context for a client.
        /// </summary>
        /// <param name="client">The client of the context</param>
        protected Context(Client client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            InitialTraceId = NextId;
            StackDepth = 0;
        }

        /// <summary>
        /// The context of internal root-level access requests
        /// </summary>
        public static Context Root => new InternalContext();

        /// <summary>
        /// The context of internal webhook requests
        /// </summary>
        internal static Context Webhook(Client client) => new InternalContext(client);

        /// <summary>
        /// The context of a remote request to some external RESTar service
        /// </summary>
        /// <param name="serviceRoot">The URI of the remote RESTar service, for example https://my-service.com:8282/rest</param>
        /// <param name="apiKey">The API key to use in remote request to this service</param>
        public static Context Remote(string serviceRoot, string apiKey = null) => new RemoteContext(serviceRoot, apiKey);

        private static ulong IdNr;
        private static string NextId => DbHelper.Base64EncodeObjectNo(IdNr += 1);
    }
}