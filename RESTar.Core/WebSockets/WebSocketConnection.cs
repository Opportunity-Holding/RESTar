using System;
using System.Collections.Generic;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Results;

namespace RESTar.WebSockets
{
    internal class WebSocketConnection : IWebSocket, IDisposable
    {
        private IWebSocketInternal duringSuspend;
        private IWebSocketInternal _webSocket;

        internal IWebSocketInternal WebSocket
        {
            get => _webSocket ?? throw new WebSocketNotConnectedException();
            private set => _webSocket = value;
        }

        internal ITerminalResource Resource { get; private set; }
        internal ITerminal Terminal { get; private set; }
        public string TraceId { get; }
        public Context Context { get; private set; }

        private bool IsSuspended { get; set; }

        internal WebSocketConnection(WebSocket webSocket, ITerminal terminal, ITerminalResource resource)
        {
            TraceId = webSocket.Id;
            Context = webSocket.Context;
            if (webSocket == null || webSocket.Status == WebSocketStatus.Closed)
                throw new WebSocketNotConnectedException();
            WebSocket = webSocket;
            Resource = resource;
            Terminal = terminal;
            Terminal.WebSocket = this;
        }

        internal void Suspend()
        {
            if (IsSuspended) return;
            duringSuspend?.Dispose();
            duringSuspend = WebSocket;
            WebSocket = new WebSocketQueue(duringSuspend);
            IsSuspended = true;
        }

        internal void Unsuspend()
        {
            if (!IsSuspended || !(WebSocket is WebSocketQueue queue)) return;
            IsSuspended = false;
            WebSocket = duringSuspend;
            duringSuspend = null;
            queue.ActionQueue.ForEach(a => a());
        }

        public void Dispose()
        {
            Terminal.Dispose();
            WebSocket = null;
            Resource = null;
            Terminal = null;
            Context = null;
        }

        #region IWebSocket

        /// <inheritdoc />
        public void SendText(string data) => WebSocket.SendText(data);

        /// <inheritdoc />
        public void SendText(byte[] data, int offset, int length) => WebSocket.SendText(data, offset, length);

        /// <inheritdoc />
        public void SendBinary(byte[] data, int offset, int length) => WebSocket.SendBinary(data, offset, length);

        /// <inheritdoc />
        public void SendJson(object i, bool at = false, bool? p = null, bool ig = false) => WebSocket.SendJson(i, at, p, ig);

        /// <inheritdoc />
        public void SendResult(IResult r, TimeSpan? t = null, bool w = false, bool d = true) => WebSocket.SendResult(r, t, w, d);

        /// <inheritdoc />
        public void StreamResult(ISerializedResult result, int messageSize, TimeSpan? timeElapsed = null, bool writeHeaders = false,
            bool disposeResult = true) => WebSocket.StreamResult(result, messageSize, timeElapsed, writeHeaders, disposeResult);

        /// <inheritdoc />
        public void SendException(Exception exception) => WebSocket.SendException(exception);

        /// <inheritdoc />
        public Headers Headers => WebSocket.Headers;

        /// <inheritdoc />
        public ReadonlyCookies Cookies => WebSocket.Cookies;

        /// <inheritdoc />
        public void DirectToShell(IEnumerable<Condition<Shell>> assignments = null) => WebSocket.DirectToShell(assignments);

        /// <inheritdoc />
        public void DirectTo<T>(ITerminalResource<T> terminalResource, ICollection<Condition<T>> assignments = null) where T : class, ITerminal =>
            WebSocket.DirectTo(terminalResource, assignments);

        /// <inheritdoc />
        public WebSocketStatus Status => IsSuspended ? WebSocketStatus.Suspended : WebSocket.Status;

        #endregion
    }
}