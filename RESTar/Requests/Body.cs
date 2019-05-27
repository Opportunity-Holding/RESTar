using System;
using System.Collections.Generic;
using System.IO;
using RESTar.Internal;
using RESTar.Results;

namespace RESTar.Requests
{
    /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// Encodes a request body
    /// </summary>
    public struct Body : IDisposable
    {
        /// <summary>
        /// The content type of the body
        /// </summary>
        public ContentType ContentType { get; }

        private CachedProtocolProvider ProtocolProvider { get; }

        /// <summary>
        /// The body's bytes
        /// </summary>
        internal RESTarStream Stream { get; }

        /// <summary>
        /// Deserializes the body to an IEnumerable of entities of the given type
        /// </summary>
        public IEnumerable<T> Deserialize<T>() where T : class
        {
            if (!HasContent) return null;
            var contentTypeProvider = ProtocolProvider.InputMimeBindings.SafeGet(ContentType.MediaType) ??
                                      throw new UnsupportedContent(ContentType.MediaType);
            try
            {
                return contentTypeProvider.DeserializeCollection<T>(Stream.Rewind());
            }
            finally
            {
                Stream.Rewind();
            }
        }

        /// <summary>
        /// Populates the body onto each entity in a source collection. If the body is empty,
        /// returns null.
        /// </summary>
        public IEnumerable<T> PopulateTo<T>(IEnumerable<T> source) where T : class
        {
            if (source == null || !HasContent) return null;
            var contentTypeProvider = ProtocolProvider.InputMimeBindings.SafeGet(ContentType.MediaType) ??
                                      throw new UnsupportedContent(ContentType.MediaType);
            try
            {
                return contentTypeProvider.Populate(source, Stream.GetBytes());
            }
            finally
            {
                Stream.Rewind();
            }
        }

        /// <summary>
        /// Does this Body have content?
        /// </summary>
        public bool HasContent { get; }

        /// <summary>
        /// The length of the body content
        /// </summary>
        public long? ContentLength { get; }

        internal string LengthLogString => !HasContent ? "" : $" ({ContentLength} bytes)";

        internal Body(RESTarStream stream, CachedProtocolProvider protocolProvider)
        {
            ContentType = (stream?.ContentType).GetValueOrDefault();
            Stream = stream;
            ContentLength = stream?.Length;
            HasContent = ContentLength > 0;
            ProtocolProvider = protocolProvider;
        }

        private const int MaxStringLength = 50_000;

        /// <inheritdoc />
        public override string ToString()
        {
            if (!HasContent) return "";
            Stream.Rewind();
            try
            {
                var reader = new StreamReader(Stream, RESTarConfig.DefaultEncoding, false, 1024, true);
                if (Stream.Length > MaxStringLength)
                {
                    var buffer = new char[MaxStringLength];
                    using (reader) reader.Read(buffer, 0, buffer.Length);
                    return new string(buffer);
                }
                else
                {
                    using (reader) return reader.ReadToEnd();
                }
            }
            finally
            {
                Stream.Rewind();
            }
        }

        internal Body GetCopy(string newProtocol = null)
        {
            if (!HasContent) return default;
            var streamCopy = new RESTarStream(ContentType);
            Stream.CopyTo(streamCopy);
            streamCopy.Rewind();
            Stream.Rewind();
            var protocolProvider = newProtocol != null
                ? ProtocolController.ResolveProtocolProvider(newProtocol)
                : ProtocolProvider;
            return new Body(streamCopy, protocolProvider);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Stream == null) return;
            Stream.CanClose = true;
            Stream.Dispose();
        }
    }
}