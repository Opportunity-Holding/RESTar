using System;
using System.IO;
using System.Net;
using RESTar.Linq;
using RESTar.NetworkProviders;
using RESTar.Requests;
using RESTar.Results;
using RESTar.WebSockets;
using Starcounter;

namespace RESTar.Internal.Sc
{
    internal class ScNetworkProvider : INetworkProvider
    {
        internal const string WsGroupName = "restar_ws";

        public void AddBindings(Method[] methods, string rootUri, ushort port)
        {
            methods.ForEach(method => Handle.CUSTOM
            (
                port: port,
                methodSpaceUri: $"{method} {rootUri}{{?}}",
                handler: (Request scRequest, string uri) =>
                {
                    var headers = new Headers(scRequest.HeadersDictionary);
                    var client = GetClient(scRequest);
                    if (!client.TryAuthenticate(ref uri, headers, out var error))
                        return ToResponse(error);
                    var context = new ScContext(client, scRequest);
                    using (var request = context.CreateRequest(uri, method, scRequest.BodyBytes, headers))
                    {
                        switch (request.Evaluate().Serialize())
                        {
                            case WebSocketUpgradeSuccessful _: return HandlerStatus.Handled;
                            case var result:
                                Admin.Console.Log(request, result);
                                return ToResponse(result);
                        }
                    }
                }
            ));

            Handle.OPTIONS
            (
                port: port,
                uriTemplate: $"{rootUri}{{?}}",
                handler: (Request scRequest, string query) =>
                {
                    var context = new ScContext(GetClient(scRequest), scRequest);
                    var headers = new Headers(scRequest.HeadersDictionary);
                    return ToResponse(context.GetOptions(query, headers));
                }
            );

            Handle.WebSocket
            (
                port: port,
                groupName: WsGroupName,
                handler: async (textInput, webSocket) =>
                {
                    try
                    {
                        await WebSocketController.HandleTextInput(ScWebSocket.GetRESTarWsId(webSocket), textInput);
                    }
                    catch (Exception e)
                    {
                        webSocket.Send(e.Message);
                        webSocket.Disconnect();
                    }
                }
            );

            Handle.WebSocket
            (
                port: port,
                groupName: WsGroupName,
                handler: (binaryInput, webSocket) =>
                {
                    try
                    {
                        WebSocketController.HandleBinaryInput(ScWebSocket.GetRESTarWsId(webSocket), binaryInput);
                    }
                    catch (Exception e)
                    {
                        webSocket.Send(e.Message);
                        webSocket.Disconnect();
                    }
                }
            );

            Handle.WebSocketDisconnect
            (
                port: port,
                groupName: WsGroupName,
                handler: webSocket =>
                {
                    try
                    {
                        WebSocketController.HandleDisconnect(ScWebSocket.GetRESTarWsId(webSocket));
                    }
                    catch { }
                }
            );
        }

        public void RemoveBindings(Method[] methods, string uri, ushort port)
        {
            methods.SafeForEach(method => Handle.UnregisterHttpHandler
            (
                port: port,
                method: method.ToString(),
                uri: $"{uri}{{?}}"
            ));
        }

        private static Response ToResponse(ISerializedResult result)
        {
            var response = new Response
            {
                StatusCode = (ushort) result.StatusCode,
                StatusDescription = result.StatusDescription
            };
            if (result.Body != null)
            {
                if (result.Headers.ContentType.HasValue)
                    response.ContentType = result.Headers.ContentType.ToString();
                if (result.Body.CanSeek && result.Body.Length > 0)
                    response.StreamedBody = result.Body;
                else
                {
                    var stream = new MemoryStream();
                    using (result.Body)
                        result.Body.CopyTo(stream);
                    if (stream.Position > 0)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        response.StreamedBody = stream;
                    }
                    else stream.Dispose();
                }
            }
            result.Headers.ForEach(header => response.Headers[header.Key] = header.Value);
            result.Cookies.ForEach(cookie => response.Headers["Set-Cookie"] = cookie.ToString());
            return response;
        }

        private static Client GetClient(Request request)
        {
            var clientIP = request.ClientIpAddress;
            var proxyIP = default(IPAddress);
            var host = request.Host;
            var userAgent = request.Headers["User-Agent"];
            var https = false;
            if (request.HeadersDictionary != null)
            {
                https = request.HeadersDictionary.ContainsKey("X-ARR-SSL") || request.HeadersDictionary.ContainsKey("X-HTTPS");
                if (request.HeadersDictionary.TryGetValue("X-Forwarded-For", out var ip))
                {
                    clientIP = IPAddress.Parse(ip.Split(':')[0]);
                    proxyIP = request.ClientIpAddress;
                }
            }
            return Client.External
            (
                clientIP: clientIP,
                proxyIP: proxyIP,
                userAgent: userAgent,
                host: host,
                https: https,
                cookies: new Cookies(request.Cookies)
            );
        }
    }
}