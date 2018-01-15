﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.WebSockets;
using static RESTar.Admin.ConsoleStatus;
using Action = RESTar.Requests.Action;

namespace RESTar.Admin
{
    internal enum ConsoleStatus : byte
    {
        PAUSED,
        ACTIVE
    }

    [RESTar(Description = "The console")]
    internal class Console : ITerminal
    {
        private struct _ { }

        private static readonly IDictionary<Console, _> Consoles;
        static Console() => Consoles = new ConcurrentDictionary<Console, _>();

        internal ConsoleStatus Status;
        private IWebSocketInternal WebSocketInternal;

        public IWebSocket WebSocket
        {
            private get => WebSocketInternal;
            set
            {
                WebSocketInternal = (IWebSocketInternal) value;
                Consoles[this] = default;
                Status = PAUSED;
                SendConsoleInit();
            }
        }

        public void Dispose() => Consoles.Remove(this);

        public void HandleBinaryInput(byte[] input) => throw new NotImplementedException();
        public bool SupportsTextInput { get; } = true;
        public bool SupportsBinaryInput { get; } = false;

        private void SendConsoleInit() => WebSocket
            .SendText("### Welcome to the RESTar WebSocket console! ###\n\n" +
                      ">>> Status: PAUSED\n\n" +
                      "> To begin, type BEGIN\n" +
                      "> To pause, type PAUSE\n" +
                      "> To close, type CLOSE\n");

        public void HandleTextInput(string input)
        {
            switch (input.ToUpperInvariant().Trim())
            {
                case "": break;
                case "BEGIN":
                    Status = ACTIVE;
                    WebSocket.SendText("Status: ACTIVE\n");
                    break;
                case "PAUSE":
                    Status = PAUSED;
                    WebSocket.SendText("Status: PAUSED\n");
                    break;
                case "EXIT":
                case "QUIT":
                case "DISCONNECT":
                case "CLOSE":
                    WebSocket.SendText("Status: CLOSED\n");
                    WebSocket.Disconnect();
                    break;
                case var unrecognized:
                    WebSocket.SendText($"Unknown command '{unrecognized}'");
                    break;
            }
        }

        internal static void LogHTTPRequest(string requestId, Action action, string uri, IPAddress clientIpAddress)
        {
            SendToAll($"=> [{requestId}] {action} '{uri}' from '{clientIpAddress}'  @ {DateTime.Now:O}");
        }

        internal static void LogHTTPResult(string requestId, IFinalizedResult result)
        {
            var info = result.Headers["RESTar-Info"];
            var errorInfo = result.Headers["ErrorInfo"];
            var tail = "";
            if (info != null)
                tail += $". {info}";
            if (errorInfo != null)
                tail += $". See {errorInfo}";
            SendToAll($"<= [{requestId}] {result.StatusCode.ToCode()}: '{result.StatusDescription}'. " +
                      $"{tail}  @ {DateTime.Now:O}");
        }

        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.ff";
        private static string _DateTime => DateTime.Now.ToString(DateTimeFormat);

        internal static void LogWebSocketOpen(IWebSocketInternal webSocket)
        {
            SendToAll($"# New WebSocket {webSocket.Id} opened to '{webSocket.TerminalResource.FullName}' " +
                      $"from '{webSocket.TcpConnection.ClientIP}' at {webSocket.Opened.ToString(DateTimeFormat)}");
        }

        internal static void LogWebSocketClosed(IWebSocketInternal webSocket)
        {
            SendToAll($"# Closed WebSocket {webSocket.Id} to '{webSocket.TerminalResource.FullName}' " +
                      $"from '{webSocket.TcpConnection.ClientIP}' at {webSocket.Closed.ToString(DateTimeFormat)}");
        }

        internal static void LogWebSocketTextInput(string input, IWebSocketInternal webSocket)
        {
            var length = Encoding.UTF8.GetByteCount(input);
            SendToAll($"=> [WS {webSocket.Id}] Received {length} bytes to '{webSocket.TerminalResource.FullName}' from " +
                      $"'{webSocket.TcpConnection.ClientIP}' at {_DateTime}. Content: {input}");
        }

        internal static void LogWebSocketBinaryInput(int inputLength, IWebSocketInternal webSocket)
        {
            SendToAll($"=> [WS {webSocket.Id}] Received {inputLength} bytes to '{webSocket.TerminalResource.FullName}' from " +
                      $"'{webSocket.TcpConnection.ClientIP}' at {_DateTime}.");
        }

        internal static void LogWebSocketTextOutput(string output, IWebSocketInternal webSocket)
        {
            var length = Encoding.UTF8.GetByteCount(output);
            SendToAll($"<= [WS {webSocket.Id}] Sent {length} bytes to '{webSocket.TcpConnection.ClientIP}' from " +
                      $"'{webSocket.TerminalResource.FullName}' at {_DateTime}. Content: {output}");
        }

        internal static void LogWebSocketBinaryOutput(int outputLength, IWebSocketInternal webSocket)
        {
            SendToAll($"<= [WS {webSocket.Id}] Sent {outputLength} bytes to '{webSocket.TcpConnection.ClientIP}' from " +
                      $"'{webSocket.TerminalResource.FullName}' at {_DateTime}.");
        }

        private static void SendToAll(string message) => Consoles.Keys
            .AsParallel()
            .Where(c => c.Status == ACTIVE)
            .ForEach(c => c.WebSocketInternal.SendTextRaw(message));
    }
}