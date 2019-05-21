using System;
using System.Collections.Generic;
using System.IO;
using RESTar.Requests;

namespace RESTar.ContentTypeProviders
{
    /// <inheritdoc />
    /// <summary>
    /// A content type provider for the Json Patch protocol (RFC 6902)
    /// </summary>
    public class JsonPatchProvider : IContentTypeProvider
    {
        private const string JsonPatchMimeType = "application/json-patch+json";

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public ContentType ContentType { get; }

        /// <inheritdoc />
        public string[] MatchStrings { get; set; }

        /// <inheritdoc />
        public bool CanRead { get; }

        /// <inheritdoc />
        public bool CanWrite { get; }

        /// <inheritdoc />
        public string ContentDispositionFileExtension { get; }

        /// <inheritdoc />
        public JsonPatchProvider()
        {
            Name = "JSON Patch";
            ContentType = JsonPatchMimeType;
            MatchStrings = new[] {JsonPatchMimeType};
            CanRead = true;
            CanWrite = true;
            ContentDispositionFileExtension = ".json";
        }

        /// <inheritdoc />
        public ulong SerializeCollection<T>(IEnumerable<T> entities, Stream stream, IRequest request = null) where T : class
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IEnumerable<T> DeserializeCollection<T>(Stream stream) where T : class
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IEnumerable<T> Populate<T>(IEnumerable<T> entities, byte[] body) where T : class
        {
            throw new NotImplementedException();
        }
    }
}