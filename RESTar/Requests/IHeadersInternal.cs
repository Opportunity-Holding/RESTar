using System.Collections.Generic;

namespace RESTar.Requests
{
    internal interface IHeadersInternal : IHeaders
    {
        bool TryGetCustomHeader(string key, out string value);
        void SetCustomHeader(string key, string value);
        bool ContainsCustomHeader(KeyValuePair<string, string> item);
        bool ContainsCustomHeaderName(string name);
        bool RemoveCustomHeader(string name);
        bool RemoveCustomHeader(KeyValuePair<string, string> header);
        IEnumerable<KeyValuePair<string, string>> GetCustomHeaders();
    }
}