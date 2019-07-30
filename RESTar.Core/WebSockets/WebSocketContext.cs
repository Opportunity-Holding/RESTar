using System;
using RESTar.Requests;

namespace RESTar.WebSockets
{
    internal class WebSocketContext : Context
    {
        protected override WebSocket CreateWebSocket() => throw new NotImplementedException();
        protected override bool IsWebSocketUpgrade { get; } = false;

        internal WebSocketContext(WebSocket webSocket, Client client) : base(client)
        {
            WebSocket = webSocket;
            Client.IsInWebSocket = true;
        }
    }
}