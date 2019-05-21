using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Results;

namespace RESTar.WebSockets
{
    /// <inheritdoc cref="IWebSocket" />
    /// <inheritdoc cref="IWebSocketInternal" />
    /// /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// A WebSocket wrapper that puts all void returning operations in a queue for later execution
    /// </summary>
    internal class WebSocketQueue : IWebSocket, IWebSocketInternal, IDisposable
    {
        internal ConcurrentQueue<Action> ActionQueue { get; }
        internal IWebSocketInternal ToQueueFor { get; }

        public string TraceId { get; }
        public Context Context { get; }
        public WebSocketStatus Status => ToQueueFor.Status;

        /// <inheritdoc />
        public void SetStatus(WebSocketStatus status) => ActionQueue.Enqueue(() => ToQueueFor.SetStatus(status));

        public Headers Headers => ToQueueFor.Headers;

        public ReadonlyCookies Cookies => ToQueueFor.Cookies;

        public WebSocketQueue(IWebSocketInternal webSocket)
        {
            ActionQueue = new ConcurrentQueue<Action>();
            ToQueueFor = webSocket;
            TraceId = webSocket.TraceId;
            Context = webSocket.Context;
        }

        public void SendTextRaw(string text) => ActionQueue.Enqueue(() => ToQueueFor.SendTextRaw(text));
        public void SendText(string d) => ActionQueue.Enqueue(() => ToQueueFor.SendText(d));
        public void SendText(byte[] d, int o, int l) => ActionQueue.Enqueue(() => ToQueueFor.SendText(d, o, l));
        public void SendBinary(byte[] d, int o, int l) => ActionQueue.Enqueue(() => ToQueueFor.SendBinary(d, o, l));
        public void SendJson(object i, bool a = false, bool? p = null, bool ig = false) => ActionQueue.Enqueue(() => ToQueueFor.SendJson(i, a, p, ig));
        public void SendException(Exception exception) => ActionQueue.Enqueue(() => ToQueueFor.SendException(exception));

        public void SendResult(IResult r, TimeSpan? t = null, bool w = false, bool d = true) =>
            ActionQueue.Enqueue(() => ToQueueFor.SendResult(r, t, w, d));

        public void StreamResult(ISerializedResult r, int m, TimeSpan? t = null, bool w = false, bool d = true) =>
            ActionQueue.Enqueue(() => ToQueueFor.StreamResult(r, m, t, w, d));

        public void DirectToShell(IEnumerable<Condition<Shell>> a = null) => ActionQueue.Enqueue(() => ToQueueFor.DirectToShell(a));

        public void DirectTo<T>(ITerminalResource<T> t, ICollection<Condition<T>> a = null) where T : class, ITerminal =>
            ActionQueue.Enqueue(() => ToQueueFor.DirectTo(t, a));

        public void Disconnect(string message) => ActionQueue.Enqueue(() => ToQueueFor.Disconnect(message));

        public void Dispose() => ActionQueue.Enqueue(() =>
        {
            var webSocket = (WebSocket) ToQueueFor;
            webSocket.Dispose();
        });
    }
}