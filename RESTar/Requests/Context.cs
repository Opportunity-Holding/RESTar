using System;
using System.Linq;
using RESTar.Internal;
using RESTar.Meta;
using RESTar.Results;
using RESTar.WebSockets;
using Starcounter;
using static RESTar.Method;
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
        internal bool IsBottomOfStack => StackDepth < 1;

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
        public virtual IRequest<T> CreateRequest<T>(Method method = GET, string protocolId = "restar", string viewName = null) where T : class
        {
            var resource = Resource<T>.SafeGet ?? throw new UnknownResource(typeof(T).RESTarTypeName());
            var parameters = new RequestParameters(this, method, resource, protocolId, viewName);
            return new Request<T>(resource, parameters);
        }

        /// <summary>
        /// Creates a request in this context for a given resource, using the given method and optional protocol id and 
        /// view name. If the protocol ID is null, the default protocol will be used. T must be a registered resource type.
        /// </summary>
        /// <param name="resource">The resource to create a request for</param>
        /// <param name="method">The method to perform, for example GET</param>
        /// <param name="protocolId">An optional protocol ID, defining the protocol to use for the request. If the 
        /// protocol ID is null, the default protocol will be used.</param>0
        /// <param name="viewName">An optional view name to use when selecting entities from the resource</param>
        public virtual IRequest<T> CreateRequest<T>(IResource<T> resource, Method method = GET, string protocolId = "restar", string viewName = null)
            where T : class
        {
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
        public virtual IRequest CreateRequest(string uri, Method method = GET, byte[] body = null, Headers headers = null)
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
        /// <param name="uriComponents">The URI components of the uri, if valid. Otherwise null</param>
        public bool UriIsValid(string uri, out Error error, out IResource resource, out IUriComponents uriComponents)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            var parameters = new RequestParameters(this, (Method) (-1), uri, null, null);
            uriComponents = null;
            if (parameters.Error != null)
            {
                var invalidParametersRequest = new InvalidParametersRequest(parameters);
                error = (Error) parameters.Error.AsResultOf(invalidParametersRequest);
                resource = null;
                return false;
            }
            resource = parameters.IResource;
            IRequest request = Construct((dynamic) resource, parameters);
            if (request.IsValid)
            {
                uriComponents = request.UriComponents;
                error = null;
                return true;
            }
            error = request.Evaluate() as Error;
            return false;
        }

        /// <summary>
        /// Returns true if and only if the current context can be used to make a request with the given method,
        /// to the given resource.
        /// </summary>
        /// <param name="method">The method the check access for</param>
        /// <param name="resource">The resource to check access to</param>
        /// <param name="error">An object describing the error, if the method is not allowed</param>
        /// <returns></returns>
        public bool MethodIsAllowed(Method method, IResource resource, out MethodNotAllowed error)
        {
            if (method < GET || method > HEAD)
                throw new ArgumentException($"Invalid method value {method} for request");
            if (resource?.AvailableMethods.Contains(method) != true)
            {
                error = new MethodNotAllowed(method, resource, false);
                return false;
            }
            if (Client.AccessRights[resource]?.Contains(method) == true)
            {
                error = null;
                return true;
            }
            error = new MethodNotAllowed(method, resource, true);
            return false;
        }

        private static IRequest Construct<T>(IResource<T> r, RequestParameters p) where T : class => new Request<T>(r, p);

        /// <summary>
        /// Use this method to check the origin of an incoming OPTIONS request. This will check the contents
        /// of the Origin header against allowed CORS origins. If the URI is valid, a body is included with the
        /// response, describing the selected resource.
        /// </summary>
        /// <param name="uri">The URI of the request</param>
        /// <param name="headers">The headers of the request</param>
        /// <returns></returns>
        public ISerializedResult GetOptions(string uri, Headers headers)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            var parameters = new RequestParameters(this, uri, headers);
            return Options.Create(parameters).Serialize();
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
        internal static Context Webhook(string apiKey, out Error error)
        {
            WebhookContext.TryCreate(apiKey, out var context, out error);
            return context;
        }

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