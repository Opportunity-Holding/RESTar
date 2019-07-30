using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.WebSockets;
using static RESTar.Method;

namespace RESTar.Admin
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="IDeleter{T}" />
    /// <summary>
    /// An entity resource containing all the currently open WebSockets
    /// </summary>
    [RESTar(GET, DELETE, Description = description)]
    public class WebSocket : ISelector<WebSocket>, IDeleter<WebSocket>
    {
        private const string description = "Lists all connected WebSockets";

        /// <summary>
        /// The unique WebSocket ID
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// The type name of the terminal currently connected to the WebSocket 
        /// </summary>
        public string TerminalType { get; private set; }

        /// <summary>
        /// An object describing the terminal
        /// </summary>
        public JObject Terminal { get; private set; }

        /// <summary>
        /// An object describing the client
        /// </summary>
        public JObject Client { get; private set; }

        /// <summary>
        /// Does this WebSocket instance represent the currently connected client websocket?
        /// </summary>
        public bool IsThis { get; private set; }

        private WebSockets.WebSocket _WebSocket { get; set; }

        /// <inheritdoc />
        public IEnumerable<WebSocket> Select(IRequest<WebSocket> request) => WebSocketController
            .AllSockets
            .Values
            .Select(socket => new WebSocket
            {
                Id = socket.TraceId,
                IsThis = socket.TraceId == request.Context.WebSocket?.TraceId,
                TerminalType = socket.TerminalResource?.Name,
                Client = JObject.FromObject(socket.GetAppProfile(), JsonProvider.Serializer),
                Terminal = socket.Terminal == null ? null : JObject.FromObject(socket.Terminal, JsonProvider.Serializer),
                _WebSocket = socket
            })
            .Where(request.Conditions);

        /// <inheritdoc />
        public int Delete(IRequest<WebSocket> request)
        {
            var count = 0;
            foreach (var entity in request.GetInputEntities())
            {
                entity._WebSocket.Disconnect();
                count += 1;
            }
            return count;
        }
    }
}