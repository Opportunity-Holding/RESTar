using System;
using Newtonsoft.Json;
using RESTar.Requests;

namespace RESTar.Internal.Logging
{
    internal struct InputOutput
    {
        public string Type;
        public ClientInfo? ClientInfo;
        public LogItem In;
        public LogItem Out;
        public double ElapsedMilliseconds;
    }

    internal struct ClientInfo
    {
        public string ClientIP;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string ProxyIP;

        public string Protocol;
        public string UserAgent;

        internal ClientInfo(Client client)
        {
            ClientIP = client.ClientIP;
            ProxyIP = client.ProxyIP;
            Protocol = client.HTTPS ? "HTTPS" : "HTTP";
            UserAgent = client.UserAgent;
        }
    }

    internal struct LogItem
    {
        public string Type;
        public string Id;
        public string Message;
        public ClientInfo? Client;
        public string Content;
        public Headers CustomHeaders;
        public DateTime? Time;
    }
}