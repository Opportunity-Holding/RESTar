﻿using System.Net;
using RESTar.WebSockets;

namespace RESTar.Requests
{
    /// <summary>
    /// Describes the origin type of a request
    /// </summary>
    public enum OriginType
    {
        /// <summary>
        /// The request originated from within the RESTar application
        /// </summary>
        Internal,

        /// <summary>
        /// The request originated from outside the RESTar application
        /// </summary>
        External,

        /// <summary>
        /// The request originated from a WebSocket
        /// </summary>
        Shell
    }

    /// <summary>
    /// Describes the origin and basic TCP connection parameters of a request
    /// </summary>
    public class TCPConnection
    {
        /// <summary>
        /// The origin type
        /// </summary>
        public OriginType Origin { get; internal set; }

        /// <summary>
        /// The client IP address that made the request (null for internal requests)
        /// </summary>
        public IPAddress ClientIP { get; internal set; }

        /// <summary>
        /// If the client was forwarded by a proxy, this property contains the proxy's IP address. Otherwise null.
        /// </summary>
        public IPAddress ProxyIP { get; internal set; }

        /// <summary>
        /// The host, as defined in the incoming request
        /// </summary>
        public string Host { get; internal set; }

        /// <summary>
        /// Was the request sent over HTTPS?
        /// </summary>
        public bool HTTPS { get; internal set; }

        /// <summary>
        /// The internal location
        /// </summary>
        public static readonly TCPConnection Internal = new TCPConnection {Host = $"localhost:{Admin.Settings._Port}"};

        /// <summary>
        /// Is the origin internal?
        /// </summary>
        public bool IsInternal => Origin == OriginType.Internal;

        /// <summary>
        /// Is the origin external?
        /// </summary>
        public bool IsExternal => Origin == OriginType.External;

        private IWebSocket webSocket;

        /// <summary>
        /// The ID of the websocket connected with this TCP connection
        /// </summary>
        public IWebSocket WebSocket
        {
            get => webSocket;
            internal set
            {
                WebSocketController.Add(value);
                webSocket = value;
            }
        }

        internal IWebSocketInternal WebSocketInternal => (IWebSocketInternal) WebSocket;

        internal bool NeedsUpgrade => HasWebSocket && WebSocket.Status == WebSocketStatus.Waiting;

        /// <summary>
        /// Does this TCP connection have a WebSocket?
        /// </summary>
        public bool HasWebSocket => WebSocket != null;

        internal TCPConnection() { }
    }
}