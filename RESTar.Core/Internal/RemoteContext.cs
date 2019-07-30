using System;
using RESTar.Requests;
using RESTar.WebSockets;

namespace RESTar.Internal
{
    internal class RemoteContext : Context
    {
        internal string ServiceRoot { get; }
        internal string ApiKey { get; }
        internal bool HasApiKey { get; }
        protected override WebSocket CreateWebSocket() => throw new NotImplementedException();
        protected override bool IsWebSocketUpgrade { get; } = false;

        public override IRequest CreateRequest(string uri, Method method = Method.GET, byte[] body = null, Headers headers = null)
        {
            return new RemoteRequest(this, method, uri, body, headers);
        }

        public override IRequest<T> CreateRequest<T>(Method method = Method.GET, string protocolId = "restar", string viewName = null) =>
            throw new InvalidOperationException("Cannot create generic requests in remote contexts");

        internal RemoteContext(string serviceRoot, string apiKey = null) : base(Client.Remote)
        {
            ServiceRoot = serviceRoot.TrimEnd('/');
            ApiKey = apiKey;
            HasApiKey = ApiKey != null;
        }
    }
}