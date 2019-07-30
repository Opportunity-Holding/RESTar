using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Linq;
using RESTar.Requests;

namespace RESTar.ContentTypeProviders.NativeJsonProtocol
{
    internal class HeadersConverter<T> : JsonConverter<T> where T : class, IHeaders, new()
    {
        private HashSet<string> WhitelistedNonCustomHeaders { get; }

        public HeadersConverter() : this(false) { }

        public HeadersConverter(bool allowAuth)
        {
            WhitelistedNonCustomHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (allowAuth)
                WhitelistedNonCustomHeaders.Add(nameof(Headers.Authorization));
        }

        /// <inheritdoc />
        /// <summary>
        /// Writes only custom headers to JSON
        /// </summary>s
        public override void WriteJson(JsonWriter writer, T headers, JsonSerializer s)
        {
            var jobj = new JObject();
            var _headers = (IHeadersInternal) headers;
            _headers?.GetCustom(WhitelistedNonCustomHeaders).ForEach(pair => jobj[pair.Key] = pair.Value);
            jobj.WriteTo(writer);
        }

        /// <inheritdoc />
        /// <summary>
        /// Reads only custom headers from JSON
        /// </summary>
        public override T ReadJson(JsonReader reader, Type o, T headers, bool h, JsonSerializer s)
        {
            IEnumerable<KeyValuePair<string, JToken>> values = JObject.Load(reader);
            headers = headers ?? new T();
            values.Where(pair => WhitelistedNonCustomHeaders.Contains(pair.Key) || pair.Key.IsCustomHeaderName())
                .ForEach(pair => headers[pair.Key] = pair.Value.ToObject<string>());
            return headers;
        }
    }
}