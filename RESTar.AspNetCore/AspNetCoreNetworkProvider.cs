using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RESTar.Linq;
using RESTar.NetworkProviders;
using RESTar.Requests;
using RESTar.Results;

namespace RESTar.AspNetCore
{
    internal class AspNetCoreNetworkProvider : INetworkProvider
    {
        private IRouteBuilder RouteBuilder { get; }

        public AspNetCoreNetworkProvider(IRouteBuilder routeBuilder)
        {
            RouteBuilder = routeBuilder;
        }

        public void AddRoutes(Method[] methods, string rootUri, ushort _)
        {
            var template = rootUri + "/{resource?}/{conditions?}/{metaconditions?}";
            foreach (var method in methods)
            {
                RouteBuilder.MapVerb(method.ToString(), template, async aspNetCoreContext =>
                {
                    var (_, uri) = aspNetCoreContext.Request.Path.Value.TSplit(rootUri);
                    var headers = new Headers(aspNetCoreContext.Request.Headers);
                    var client = GetClient(aspNetCoreContext);
                    if (!client.TryAuthenticate(ref uri, headers, out var error))
                    {
                        await WriteResponse(aspNetCoreContext, error);
                        return;
                    }
                    var context = new RESTarAspNetCoreContext(client, aspNetCoreContext);
                    var body = aspNetCoreContext.Request.Body.ReadFully();
                    using var request = context.CreateRequest(uri, method, body, headers);
                    using var result = request.Evaluate().Serialize();
                    if (result is WebSocketUpgradeSuccessful ws)
                        await ws.WebSocketLifeTime;
                    else await WriteResponse(aspNetCoreContext, result);
                });
            }
        }

        public void RemoveRoutes(Method[] methods, string uri, ushort _) { }

        private static async Task WriteResponse(HttpContext context, ISerializedResult result)
        {
            context.Response.StatusCode = (ushort) result.StatusCode;
            result.Headers.ForEach(header => context.Response.Headers[header.Key] = header.Value);
            result.Cookies.ForEach(cookie => context.Response.Headers["Set-Cookie"] = cookie.ToString());
            if (result.Body != null)
            {
                if (result.Headers.ContentType.HasValue)
                    context.Response.ContentType = result.Headers.ContentType.ToString();
                await using var local = result.Body;
                await using var remote = context.Response.Body;
                await local.CopyToAsync(remote);
            }
        }

        private static Client GetClient(HttpContext context)
        {
            var clientIP = context.Connection.RemoteIpAddress;
            var proxyIP = default(IPAddress);
            var host = context.Request.Host.Host;
            var userAgent = context.Request.Headers["User-Agent"];
            var https = context.Request.IsHttps;
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var ip))
            {
                clientIP = IPAddress.Parse(ip.First().Split(':')[0]);
                proxyIP = clientIP;
            }
            return Client.External
            (
                clientIP: clientIP,
                proxyIP: proxyIP,
                userAgent: userAgent,
                host: host,
                https: https,
                cookies: new Cookies(context.Request.Cookies)
            );
        }
    }
}