using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using RESTar.Internal.Auth;
using RESTar.Meta;
using RESTar.Results;

namespace RESTar.Requests
{
    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// Describes the origin and basic client parameters of a request
    /// </summary>
    public class Client
    {
        /// <summary>
        /// The origin type
        /// </summary>
        public OriginType Origin { get; }

        /// <summary>
        /// The client IP address that made the request (null for internal requests)
        /// </summary>
        public string ClientIP { get; }

        /// <summary>
        /// If the client was forwarded by a proxy, this property contains the proxy's IP address. Otherwise null.
        /// </summary>
        public string ProxyIP { get; }

        /// <summary>
        /// The host, as defined in the incoming request
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// The user agent, as defined in the incoming request
        /// </summary>
        public string UserAgent { get; }

        /// <summary>
        /// Was the request sent over HTTPS?
        /// </summary>
        public bool HTTPS { get; }

        internal AccessRights AccessRights { get; set; }
        internal IDictionary<IResource, byte> ResourceAuthMappings { get; }
        internal IDictionary<IResource, IDictionary<string, object>> ResourceClientDataMappings { get; }
        internal bool IsInWebSocket { get; set; }
        internal string ShellConfig { get; set; }

        /// <summary>
        /// Creates a new client with the given origin type
        /// </summary>
        private Client(OriginType origin, string host, IPAddress clientIP, IPAddress proxyIP, string userAgent, bool https)
        {
            Origin = origin;
            Host = host;
            ClientIP = clientIP?.ToString();
            ProxyIP = proxyIP?.ToString();
            UserAgent = userAgent;
            HTTPS = https;
            ResourceAuthMappings = new ConcurrentDictionary<IResource, byte>();
            ResourceClientDataMappings = new ConcurrentDictionary<IResource, IDictionary<string, object>>();
        }

        /// <summary>
        /// Creates a new Client representing an external web client.
        /// </summary>
        /// <param name="clientIP">The IP address of the client</param>
        /// <param name="proxyIP">The IP address of the proxy, or null if no proxy was used to route the request</param>
        /// <param name="userAgent">THe user agent of the client</param>
        /// <param name="host">The content of the host header in the HTTP request</param>
        /// <param name="https">Is the client connected with HTTPS?</param>
        /// <returns></returns>
        public static Client External(IPAddress clientIP, IPAddress proxyIP, string userAgent, string host, bool https) => new Client
        (
            origin: OriginType.External,
            host: host,
            clientIP: clientIP,
            proxyIP: proxyIP,
            userAgent: userAgent,
            https: https
        );

        /// <summary>
        /// The internal location, has root access to all resources
        /// </summary>
        public static Client Internal => new Client
        (
            origin: OriginType.Internal,
            host: $"localhost:{Admin.Settings._Port}",
            clientIP: new IPAddress(new byte[] {127, 0, 0, 1}),
            proxyIP: null,
            userAgent: null,
            https: false
        ) {AccessRights = AccessRights.Root};

        internal static Client Remote => new Client
        (
            origin: (OriginType) (-1),
            host: $"localhost:{Admin.Settings._Port}",
            clientIP: new IPAddress(new byte[] {127, 0, 0, 1}),
            proxyIP: null,
            userAgent: null,
            https: false
        );

        internal static Client Webhook => new Client
        (
            origin: OriginType.Internal,
            host: $"localhost:{Admin.Settings._Port}",
            clientIP: new IPAddress(new byte[] {127, 0, 0, 1}),
            proxyIP: null,
            userAgent: null,
            https: false
        );

        /// <summary>
        /// Returns true if and only if this client is considered authenticated. This is a necessary precondition for 
        /// being included in a context. If false, a NotAuthorized result object is returned in the out parameter, that 
        /// can be returned to the client.
        /// </summary>
        /// <param name="uri">The URI of the request</param>
        /// <param name="headers">The headers of the request</param>
        /// <param name="error">The error result, if not authenticated</param>
        /// <returns></returns>
        public bool TryAuthenticate(ref string uri, Headers headers, out Forbidden error)
        {
            error = null;
            AccessRights = Authenticator.GetAccessRights(ref uri, headers);
            if (!RESTarConfig.RequireApiKey)
                AccessRights = AccessRights.Root;
            if (AccessRights == null)
            {
                error = new NotAuthorized();
                if (headers.Metadata == "full")
                    error.Headers.Metadata = error.Metadata;
                return false;
            }
            return true;
        }

        internal bool TryAuthenticate(string apiKeyHash, out Forbidden error)
        {
            error = null;
            AccessRights = Authenticator.GetAccessRights(apiKeyHash);
            if (!RESTarConfig.RequireApiKey) AccessRights = AccessRights.Root;
            if (AccessRights == null)
            {
                error = new NotAuthorized();
                return false;
            }
            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Origin: {Origin}, IP: {ClientIP}";
        }
    }
}