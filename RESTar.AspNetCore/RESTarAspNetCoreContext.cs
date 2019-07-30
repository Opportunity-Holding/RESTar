using System;
using Microsoft.AspNetCore.Http;
using RESTar.Requests;
using RESTar.WebSockets;

namespace RESTar.AspNetCore
{
    internal class RESTarAspNetCoreContext : Context
    {
        private HttpContext HttpContext { get; }

        public RESTarAspNetCoreContext(Client client, HttpContext httpContext) : base(client)
        {
            HttpContext = httpContext;
        }
        protected override bool IsWebSocketUpgrade => HttpContext.WebSockets.IsWebSocketRequest;

        protected override WebSocket CreateWebSocket()
        {
            return new AspNetCoreWebSocket(HttpContext, Guid.NewGuid().ToString("N"), Client);
        }
    }
}