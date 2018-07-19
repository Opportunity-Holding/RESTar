﻿using System.Collections.Generic;
using System.Linq;
using Dynamit;
using Newtonsoft.Json;
using RESTar.ContentTypeProviders.NativeJsonProtocol;
using Starcounter;

namespace RESTar.Requests
{
    /// <inheritdoc cref="DDictionary" />
    /// <inheritdoc cref="IDDictionary{TTable,TKvp}" />
    /// <inheritdoc cref="IHeaders" />
    /// <summary>
    /// A persistent storage for request headers
    /// </summary>
    [Database, JsonConverter(typeof(HeadersConverter<DbHeaders>))]
    public class DbHeaders : DDictionary, IDDictionary<DbHeaders, DbHeadersKvp>, IHeaders, IHeadersInternal
    {
        /// <inheritdoc />
        public ContentTypes Accept { get; set; }

        /// <inheritdoc />
        public ContentType? ContentType { get; set; }

        /// <inheritdoc />
        public string Source { get; set; }

        /// <inheritdoc />
        public string Destination { get; set; }

        /// <inheritdoc />
        public string Authorization { get; set; }

        /// <inheritdoc />
        public string Origin { get; set; }

        internal Headers ToTransient() => new Headers(this);

        #region IHeadersInternal

        bool IHeadersInternal.TryGetCustomHeader(string key, out string value)
        {
            if (base.TryGetValue(key, out var _value))
            {
                value = _value.ToString();
                return true;
            }
            value = null;
            return false;
        }

        void IHeadersInternal.SetCustomHeader(string key, string value) => base[key] = value;

        bool IHeadersInternal.ContainsCustomHeader(KeyValuePair<string, string> item) =>
            Contains(new KeyValuePair<string, object>(item.Key, item.Value));

        bool IHeadersInternal.ContainsCustomHeaderName(string name) => ContainsKey(name);
        bool IHeadersInternal.RemoveCustomHeader(string name) => Remove(name);

        bool IHeadersInternal.RemoveCustomHeader(KeyValuePair<string, string> header) =>
            Remove(new KeyValuePair<string, object>(header.Key, header.Value));

        IEnumerable<KeyValuePair<string, string>> IHeadersInternal.GetCustomHeaders() => _GetCustomHeaders();

        private IEnumerable<KeyValuePair<string, string>> _GetCustomHeaders() => KeyValuePairs
            .Select(p => new KeyValuePair<string, string>(p.Key, p.Value?.ToString()));

        #endregion

        #region Overriding operations

        /// <inheritdoc />
        public void Add(KeyValuePair<string, string> item) => Add(item.Key, item.Value);

        /// <inheritdoc />
        public void Add(string key, string value) => this._Set(key, value);

        /// <inheritdoc />
        public new ICollection<string> Keys => this._Keys();

        /// <inheritdoc />
        public new ICollection<string> Values => this._Values();

        /// <inheritdoc />
        public new IEnumerator<KeyValuePair<string, string>> GetEnumerator() => new HeadersEnumerator(this, _GetCustomHeaders().GetEnumerator());

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, string> item) => this._Contains(item);

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, string> item) => this._Remove(item);

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => this._CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public bool TryGetValue(string key, out string value) => this._TryGetValue(key, out value);

        /// <inheritdoc />
        public new string this[string key]
        {
            get => this._Get(key);
            set => this._Set(key, value);
        }

        #endregion

        #region Explicit implementations

        DbHeadersKvp IDDictionary<DbHeaders, DbHeadersKvp>.NewKeyPair(DbHeaders dict, string key, object value) =>
            new DbHeadersKvp(dict, key, value?.ToString());

        #endregion
    }

    /// <inheritdoc />
    /// <summary>
    /// Key-value pair class for DbHeaders
    /// </summary>
    [Database]
    public class DbHeadersKvp : DKeyValuePair
    {
        /// <inheritdoc />
        public DbHeadersKvp(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }
}