using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using RESTar.Meta;
using RESTar.Requests;

// ReSharper disable All
#pragma warning disable 1591

namespace RESTar.ContentTypeProviders
{
    public enum JsonPatchOperation
    {
        add,
        remove,
        replace,
        copy,
        move,
        test
    }

    public struct JsonPatch
    {
        public JsonPatchOperation op { get; }
        public string path { get; }
        public string from { get; }
        public dynamic value { get; }

        /// <summary>
        /// Creates a new json patch
        /// </summary>
        [JsonConstructor]
        public JsonPatch(JsonPatchOperation op, string path, string from, dynamic value)
        {
            this.op = op;
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            this.from = from;
            this.value = value;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// A content type provider for the Json Patch protocol (RFC 6902)
    /// </summary>
    public class JsonPatchProvider : IContentTypeProvider
    {
        public const string JsonPatchMimeType = "application/json-patch+json";

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

        public static JsonPatch[] ParsePatches(string patchString)
        {
            return Providers.Json.Deserialize<JsonPatch[]>(patchString);
        }

        private static Exception Unsupported(string message) => new Exception("Unsupported patch: " + message);

        internal void ApplyPatches<T>(T target, IEnumerable<JsonPatch> patches)
        {
            foreach (var patch in patches)
            {
                var termString = patch.path.Substring(1).Replace("/", ".");
                switch (termString)
                {
                    case "_ver#c$":
                    case "_ver#s":
                        // we don't deal with this right now
                        continue;
                }
                var path = Term.Create(typeof(T), termString);

                switch (patch.op)
                {
                    case JsonPatchOperation.add:
                    {
                        if (!(patch.value is string valueString))
                            throw Unsupported("Can only use 'add' to add strings to other strings");
                        var existingString = path.Evaluate(target, out _, out var parent, out var property);
                        if (existingString is string || (existingString == null && (property.Type == typeof(string) || property.IsDynamic)))
                        {
                            var newValue = $"{existingString}{valueString}";
                            property.SetValue(parent, newValue);
                        }
                        break;
                    }
                    case JsonPatchOperation.remove: break;
                    case JsonPatchOperation.replace:
                    {
                        path.Evaluate(target, out _, out var parent, out var property);
                        property.SetValue(parent, patch.value);

                        break;
                    }
                    case JsonPatchOperation.copy: break;
                    case JsonPatchOperation.move: break;
                    case JsonPatchOperation.test: break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
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
            var patches = Providers.Json.Deserialize<JsonPatch[]>(body);
            foreach (var entity in entities)
            {
                ApplyPatches(entity, patches);
                yield return entity;
            }
        }
    }
}