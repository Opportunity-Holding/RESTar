using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RESTar.ContentTypeProviders;
using RESTar.Linq;

namespace RESTar.WebSockets
{
    public static class WebSocketController
    {
        internal static readonly IDictionary<string, WebSocket> AllSockets;
        static WebSocketController() => AllSockets = new ConcurrentDictionary<string, WebSocket>();
        internal static void Add(WebSocket webSocket) => AllSockets[webSocket.TraceId] = webSocket;

        internal static void RevokeAllWithKey(string key) => AllSockets.Values
            .Where(webSocket => webSocket.Client.AccessRights.ApiKey == key)
            .ForEach(webSocket => webSocket.Disconnect($"The access rights of this WebSocket ({webSocket.TraceId}) " +
                                                       "have been revoked. Disconnecting..."));

        public static async Task HandleTextInput(string wsId, string textInput)
        {
            if (!AllSockets.TryGetValue(wsId, out var webSocket))
                throw new UnknownWebSocketIdException($"This WebSocket ({wsId}) is not recognized by the current " +
                                                      "application. Disconnecting...");

            if (webSocket.IsStreaming)
            {
                await webSocket.HandleStreamingTextInput(textInput);
                return;
            }

            if (textInput.ElementAtOrDefault(0) == '#')
            {
                var (command, tail) = textInput.Trim().TSplit(' ');
                switch (command.ToUpperInvariant())
                {
                    case "#TERMINAL" when tail is string json:
                        try
                        {
                            Providers.Json.Populate(json, webSocket.Terminal);
                            webSocket.SendText("Terminal updated");
                            webSocket.SendJson(webSocket.Terminal);
                        }
                        catch (Exception e)
                        {
                            webSocket.SendException(e);
                        }
                        break;
                    case "#TERMINAL":
                        webSocket.SendJson(webSocket.Terminal);
                        break;
                    case "#INFO" when tail is string json:
                        try
                        {
                            var profile = webSocket.GetAppProfile();
                            Providers.Json.Populate(json, profile);
                            webSocket.SendText("Profile updated");
                            webSocket.SendJson(webSocket.GetAppProfile());
                        }
                        catch (Exception e)
                        {
                            webSocket.SendException(e);
                        }
                        break;
                    case "#INFO":
                        webSocket.SendJson(webSocket.GetAppProfile());
                        break;
                    case "#SHELL":
                    case "#HOME":
                        webSocket.DirectToShell();
                        break;
                    case "#DISCONNECT":
                        webSocket.Disconnect();
                        break;
                    default:
                        webSocket.SendText($"Unknown global command '{command}'");
                        break;
                }
            }
            else webSocket.HandleTextInput(textInput);
        }

        public static void HandleBinaryInput(string wsId, byte[] binaryInput)
        {
            if (!AllSockets.TryGetValue(wsId, out var webSocket))
                throw new UnknownWebSocketIdException($"Unknown WebSocket ID: {wsId}");
            webSocket.HandleBinaryInput(binaryInput);
        }

        public static void HandleDisconnect(string wsId)
        {
            if (!AllSockets.TryGetValue(wsId, out var webSocket))
                throw new UnknownWebSocketIdException($"Unknown WebSocket ID: {wsId}");
            webSocket.Dispose();
            AllSockets.Remove(wsId);
        }
    }
}