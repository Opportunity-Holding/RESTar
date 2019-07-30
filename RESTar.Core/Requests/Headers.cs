using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using RESTar.ContentTypeProviders.NativeJsonProtocol;
using RESTar.Linq;

namespace RESTar.Requests
{
    /// <inheritdoc cref="IDictionary{TKey,TValue}" />
    /// <summary>
    /// A collection of request headers. Key comparison is case insensitive.
    /// </summary>
    [JsonConverter(typeof(HeadersConverter<Headers>))]
    public class Headers : IHeaders, IHeadersInternal
    {
        #region Response headers

        #region Header names

        private const string _Info = "RESTar-info";
        private const string _Error = "RESTar-error";
        private const string _Elapsed = "RESTar-elapsed-ms";
        private const string _EntityCount = "RESTar-count";
        private const string _Pager = "RESTar-pager";
        private const string _Metadata = "RESTar-metadata";
        private const string _Version = "RESTar-version";
        private const string _Vary = "Vary";
        private const string _AccessControlAllowOrigin = "Access-Control-Allow-Origin";
        private const string _AccessControlAllowMethods = "Access-Control-Allow-Methods";
        private const string _AccessControlMaxAge = "Access-Control-Max-Age";
        private const string _AccessControlAllowCredentials = "Access-Control-Allow-Credentials";
        private const string _AccessControlAllowHeaders = "Access-Control-Allow-Headers";

        #endregion

        internal string Info
        {
            get => this[_Info];
            set => this[_Info] = value;
        }

        internal string Error
        {
            get => this[_Error];
            set => this[_Error] = value;
        }

        internal string Elapsed
        {
            get => this[_Elapsed];
            set => this[_Elapsed] = value;
        }

        internal string EntityCount
        {
            get => this[_EntityCount];
            set => this[_EntityCount] = value;
        }

        internal string Pager
        {
            get => this[_Pager];
            set => this[_Pager] = value;
        }

        internal string Metadata
        {
            get => this[_Metadata];
            set => this[_Metadata] = value;
        }

        internal string Version
        {
            get => this[_Version];
            set => this[_Version] = value;
        }

        internal string Vary
        {
            get => this[_Vary];
            set => this[_Vary] = value;
        }

        internal string AccessControlAllowOrigin
        {
            get => this[_AccessControlAllowOrigin];
            set => this[_AccessControlAllowOrigin] = value;
        }

        internal string AccessControlAllowMethods
        {
            get => this[_AccessControlAllowMethods];
            set => this[_AccessControlAllowMethods] = value;
        }

        internal string AccessControlMaxAge
        {
            get => this[_AccessControlMaxAge];
            set => this[_AccessControlMaxAge] = value;
        }

        internal string AccessControlAllowCredentials
        {
            get => this[_AccessControlAllowCredentials];
            set => this[_AccessControlAllowCredentials] = value;
        }

        internal string AccessControlAllowHeaders
        {
            get => this[_AccessControlAllowHeaders];
            set => this[_AccessControlAllowHeaders] = value;
        }

        #endregion

        /// <inheritdoc />
        public ContentTypes Accept { get; set; }

        /// <inheritdoc />
        public ContentType? ContentType { get; set; }

        /// <inheritdoc />
        public string Source { get; set; }

        /// <inheritdoc />
        public string Destination { get; set; }

        /// <summary>
        /// The Authorization header
        /// </summary>
        public string Authorization { internal get; set; }

        string IHeaders.Authorization
        {
            get => Authorization;
            set => Authorization = value;
        }

        /// <inheritdoc />
        public string Origin { get; set; }

        internal bool UnsafeOverride { get; set; }

        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        /// <summary>
        /// Gets the header with the given name, or null if there is 
        /// no such header.
        /// </summary>
        public string this[string key]
        {
            get => this._Get(key);
            set
            {
                if (value == null) Remove(key);
                this._Set(key, value);
            }
        }

        private IDictionary<string, string> _dict { get; }

        /// <inheritdoc />
        public Headers() => _dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public Headers(Dictionary<string, string> dictToUse) : this() => dictToUse?.ForEach(pair => this[pair.Key] = pair.Value);
        public Headers(IDictionary<string, StringValues> dictToUse) : this() => dictToUse?.ForEach(pair => this[pair.Key] = pair.Value);

        /// <inheritdoc />
        internal Headers(IHeadersInternal other) : this()
        {
            Accept = other.Accept;
            ContentType = other.ContentType;
            Source = other.Source;
            Destination = other.Destination;
            Authorization = other.Authorization;
            Origin = other.Origin;
            other.GetCustomHeaders().ForEach(pair => SetCustomHeader(pair.Key, pair.Value));
        }

        #region IHeadersInternal

        private void SetCustomHeader(string key, string value) => _dict[key] = value;
        bool IHeadersInternal.TryGetCustomHeader(string key, out string value) => _dict.TryGetValue(key, out value);
        void IHeadersInternal.SetCustomHeader(string key, string value) => SetCustomHeader(key, value);
        bool IHeadersInternal.ContainsCustomHeader(KeyValuePair<string, string> item) => _dict.Contains(item);
        bool IHeadersInternal.ContainsCustomHeaderName(string name) => _dict.ContainsKey(name);
        bool IHeadersInternal.RemoveCustomHeader(string name) => _dict.Remove(name);
        bool IHeadersInternal.RemoveCustomHeader(KeyValuePair<string, string> header) => _dict.Remove(header);
        IEnumerable<KeyValuePair<string, string>> IHeadersInternal.GetCustomHeaders() => _dict;

        #endregion

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => new HeadersEnumerator(this, _dict.GetEnumerator());

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> pair) => this._Set(pair.Key, pair.Value);

        /// <inheritdoc />
        public void Clear()
        {
            Accept = null;
            ContentType = null;
            Source = null;
            Destination = null;
            Authorization = null;
            Origin = null;
            _dict.Clear();
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, string> item) => this._Contains(item);

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => this._CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, string> item) => this._Remove(item);

        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        // ReSharper disable once UseCollectionCountProperty
        // We must use the HeadersEnumerator for this to work properly
        public int Count => this.Count();

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        public bool ContainsKey(string key) => this._ContainsKey(key);

        /// <inheritdoc />
        public void Add(string key, string value) => this._Set(key, value);

        /// <inheritdoc />
        public bool Remove(string key) => this._Remove(key);

        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        public bool TryGetValue(string key, out string value) => this._TryGetValue(key, out value);

        /// <inheritdoc />
        public ICollection<string> Keys => this._Keys();

        /// <inheritdoc />
        public ICollection<string> Values => this._Keys();

        internal Headers GetCopy() => new Headers(this);
    }
}