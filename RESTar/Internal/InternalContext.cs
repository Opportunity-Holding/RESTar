using System;
using RESTar.Requests;
using RESTar.WebSockets;

namespace RESTar.Internal
{
    internal class InternalContext : Context
    {
        protected override WebSocket CreateWebSocket() => throw new NotImplementedException();
        protected override bool IsWebSocketUpgrade { get; } = false;
        internal InternalContext(Client client = null) : base(client ?? Client.Internal) { }
    }
}