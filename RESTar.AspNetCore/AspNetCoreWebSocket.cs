using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RESTar.Requests;
using RESTar.WebSockets;
using WebSocket = RESTar.WebSockets.WebSocket;

namespace RESTar.AspNetCore
{
    internal class AspNetCoreWebSocket : WebSocket
    {
        private HttpContext HttpContext { get; }
        private System.Net.WebSockets.WebSocket WebSocket { get; set; }

        public AspNetCoreWebSocket(HttpContext httpContext, string webSocketId, Client client) : base(webSocketId, client)
        {
            HttpContext = httpContext;
        }

        protected override void Send(string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            var segment = new ArraySegment<byte>(buffer);
            WebSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None).Wait();
        }

        protected override void Send(byte[] data, bool isText, int offset, int length)
        {
            var segment = new ArraySegment<byte>(data);
            WebSocket.SendAsync(segment, isText ? WebSocketMessageType.Text : WebSocketMessageType.Binary, true, CancellationToken.None).Wait();
        }

        protected override bool IsConnected => !WebSocket.CloseStatus.HasValue;

        protected override async Task SendUpgrade()
        {
            WebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await ManageReceieveLoopAsync();
            WebSocketController.HandleDisconnect(Id);
        }

        public async Task ManageReceieveLoopAsync()
        {
            try
            {
                while (await ReceiveStreamAsync() is Stream stream && !WebSocket.CloseStatus.HasValue)
                {
                    var bytes = stream.ReadFully();
                    var str = Encoding.UTF8.GetString(bytes);
                    try
                    {
                        await WebSocketController.HandleTextInput(Id, str);
                    }
                    catch (Exception e)
                    {
                        Send(e.Message);
                        Dispose();
                    }
                }
            }
            catch { }
        }

        protected override async void OnDispose()
        {
            switch (WebSocket?.State)
            {
                case null:
                case WebSocketState.Aborted:
                case WebSocketState.Closed: return;

                default:
                {
                    using var socket = WebSocket;
                    await socket.CloseAsync
                    (
                        closeStatus: socket.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                        statusDescription: socket.CloseStatusDescription,
                        cancellationToken: CancellationToken.None
                    );
                    break;
                }
            }
        }

        #region WebSocket message helpers

        private async Task<Stream> ReceiveStreamAsync(CancellationToken ct = default)
        {
            var buffer = new ArraySegment<byte>(new byte[4096]);
            var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                ct.ThrowIfCancellationRequested();
                result = await WebSocket.ReceiveAsync(buffer, ct);
                if (buffer.Array != null)
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
            }
            while (!result.EndOfMessage);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        private async Task SendStreamAsync(MemoryStream dataStream, CancellationToken ct = default)
        {
            try
            {
                var segment = new ArraySegment<byte>(dataStream.ToArray());
                await WebSocket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
            }
            catch { }
        }

        #endregion

        protected override void DisconnectWebSocket(string message = null)
        {
            WebSocket.CloseAsync(WebSocketCloseStatus.Empty, message ?? "WebSocket closed", CancellationToken.None);
        }
    }
}