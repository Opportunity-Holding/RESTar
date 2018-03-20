﻿using System;
using RESTar.Requests;
using RESTar.WebSockets;

namespace RESTar.Logging
{
    internal class WebSocketEvent : ILogable
    {
        public LogEventType LogEventType { get; }
        public string TraceId { get; }
        public string LogMessage { get; }
        public string LogContent { get; }
        public Client Client { get; }
        public Headers Headers { get; }
        public string HeadersStringCache { get; set; }
        private IWebSocketInternal WebSocket { get; }
        public bool ExcludeHeaders { get; }
        public DateTime LogTime { get; }

        public WebSocketEvent(LogEventType direction, IWebSocketInternal webSocket, string content = null, int length = 0)
        {
            LogEventType = direction;
            TraceId = webSocket.TraceId;
            WebSocket = webSocket;
            ExcludeHeaders = false;
            LogTime = DateTime.Now;
            switch (direction)
            {
                case LogEventType.WebSocketInput:
                    LogMessage = $"Received {length} bytes";
                    break;
                case LogEventType.WebSocketOutput:
                    LogMessage = $"Sent {length} bytes";
                    break;
                case LogEventType.WebSocketOpen:
                    LogMessage = "WebSocket opened";
                    break;
                case LogEventType.WebSocketClose:
                    LogMessage = "WebSocket closed";
                    break;
            }
            LogContent = content;
            Client = webSocket.Client;
            Headers = webSocket.Headers;
        }
    }
}