using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Admin;
using RESTar.ContentTypeProviders.NativeJsonProtocol;
using RESTar.Requests;
using static Newtonsoft.Json.Formatting;
using static RESTar.Admin.Settings;

namespace RESTar.ContentTypeProviders
{
    /// <inheritdoc />
    public class JsonProvider : IContentTypeProvider
    {
        /// <summary>
        /// The settings that are used when serializing and deserializing JSON
        /// </summary>
        private static JsonSerializerSettings Settings { get; }

        private static JsonSerializerSettings SettingsIgnoreNulls { get; }

        /// <summary>
        /// UTF 8 encoding without byte order mark (BOM)
        /// </summary>
        private static Encoding UTF8 { get; }

        /// <summary>
        /// The JSON serializer
        /// </summary>
        internal static JsonSerializer Serializer { get; }

        /// <summary>
        /// Creates a new JSON.net JsonSerializer with the current RESTar serialization settings
        /// </summary>
        /// <returns></returns>
        public static JsonSerializer GetSerializer() => JsonSerializer.Create(Settings);

        private static JsonSerializer SerializerIgnoreNulls { get; }

        private const string JsonMimeType = "application/json";
        private const string RESTarSpecific = "application/restar-json";
        private const string Brief = "json";
        private const string TextPlain = "text/plain";

        static JsonProvider()
        {
            UTF8 = RESTarConfig.DefaultEncoding;
            Settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                DateParseHandling = DateParseHandling.DateTime,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ContractResolver = new DefaultResolver(),
                FloatParseHandling = FloatParseHandling.Decimal
            };
            Serializer = JsonSerializer.Create(Settings);
            SettingsIgnoreNulls = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateParseHandling = Settings.DateParseHandling,
                DateFormatHandling = Settings.DateFormatHandling,
                DateTimeZoneHandling = Settings.DateTimeZoneHandling,
                ContractResolver = Settings.ContractResolver,
                FloatParseHandling = Settings.FloatParseHandling
            };
            SerializerIgnoreNulls = JsonSerializer.Create(SettingsIgnoreNulls);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="JsonProvider"/> type
        /// </summary>
        public JsonProvider()
        {
            MatchStrings = new[] {JsonMimeType, RESTarSpecific, Brief, TextPlain};
            ContentDispositionFileExtension = ".json";
            CanWrite = true;
            CanRead = true;
            ContentType = "application/json; charset=utf-8";
            Name = "JSON";
        }

        internal string SerializeFormatter(JToken formatterToken, out int indents)
        {
            using (var sw = new StringWriter())
            using (var jwr = new FormatWriter(sw))
            {
                jwr.Formatting = Indented;
                Serializer.Serialize(jwr, formatterToken);
                indents = jwr.Depth;
                return sw.ToString();
            }
        }

        /// <summary>
        /// A general-purpose serializer. Serializes the given value using the formatting, and optionally - a type. If no 
        /// formatting is given, the formatting defined in Settings is used.
        /// </summary>
        public string Serialize(object value, Formatting? formatting = null, bool ignoreNulls = false)
        {
            var _formatting = formatting ?? (_PrettyPrint ? Indented : None);
            var settings = ignoreNulls ? SettingsIgnoreNulls : Settings;
            return JsonConvert.SerializeObject(value, _formatting, settings);
        }

        /// <summary>
        /// A general-purpose deserializer. Deserializes the given JSON string.
        /// </summary>
        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }

        /// <summary>
        /// A general-purpose deserializer. Deserializes the given byte array.
        /// </summary>
        public T Deserialize<T>(byte[] bytes)
        {
            var json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }

        /// <summary>
        /// Serializes the value to the given JsonTextWriter, using the default serializer
        /// </summary>
        public void Serialize(JsonTextWriter jsonWriter, object value)
        {
            Serializer.Serialize(jsonWriter, value);
        }

        internal void Populate(string json, object target)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            JsonConvert.PopulateObject(json, target, Settings);
        }

        internal MemoryStream SerializeStream(object entity, Formatting? formatting = null, bool ignoreNulls = false)
        {
            var _formatting = formatting ?? (_PrettyPrint ? Indented : None);
            var serializer = ignoreNulls ? SerializerIgnoreNulls : Serializer;
            var stream = new MemoryStream();
            using (var swr = new StreamWriter(stream, UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr, 0))
            {
                jwr.Formatting = _formatting;
                serializer.Serialize(jwr, entity);
            }
            return stream;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public ContentType ContentType { get; }

        /// <inheritdoc />
        public bool CanRead { get; }

        /// <inheritdoc />
        public bool CanWrite { get; }

        /// <inheritdoc />
        public string ContentDispositionFileExtension { get; }

        /// <inheritdoc />
        public IEnumerable<T> Populate<T>(IEnumerable<T> entities, byte[] body) where T : class
        {
            var json = Encoding.UTF8.GetString(body);
            foreach (var entity in entities)
            {
                JsonConvert.PopulateObject(json, entity, Settings);
                yield return entity;
            }
        }

        /// <inheritdoc />
        public string[] MatchStrings { get; set; }

        /// <inheritdoc />
        public ulong SerializeCollection<T>(IEnumerable<T> entities, Stream stream, IRequest request = null) where T : class
        {
            if (entities == null) return 0;
            var formatter = request?.MetaConditions.Formatter ?? DbOutputFormat.Default;
            using (var swr = new StreamWriter(stream, UTF8, 2048, true))
            using (var jwr = new RESTarJsonWriter(swr, formatter.StartIndent))
            {
                jwr.Formatting = _PrettyPrint ? Indented : None;
                swr.Write(formatter.Pre);
                Serializer.Serialize(jwr, entities);
                swr.Write(formatter.Post);
                return jwr.ObjectsWritten;
            }
        }

        /// <inheritdoc />
        public IEnumerable<T> DeserializeCollection<T>(Stream body) where T : class
        {
            using (var jsonReader = new JsonTextReader(new StreamReader(body, UTF8, false, 1024, true)))
            {
                jsonReader.Read();
                switch (jsonReader.TokenType)
                {
                    case JsonToken.StartObject:
                        yield return Serializer.Deserialize<T>(jsonReader);
                        break;
                    case JsonToken.StartArray:
                        jsonReader.Read();
                        while (jsonReader.TokenType != JsonToken.EndArray)
                        {
                            yield return Serializer.Deserialize<T>(jsonReader);
                            jsonReader.Read();
                        }
                        break;
                    case var other: throw new JsonReaderException($"Invalid JSON data. Expected array or object. Found {other}");
                }
            }
        }
    }
}