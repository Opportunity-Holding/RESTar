using System.Collections.Generic;
using System.IO;
using System.Text;
using RESTar.Internal;
using RESTar.Requests;

namespace RESTar.ContentTypeProviders
{
    /// <inheritdoc />
    /// <summary>
    /// The <see cref="JsonAdapter" /> simplifies the process of building a RESTar 
    /// content provider. Deserialization is usually the hardest part, and the JsonAdapterProvider
    /// uses JSON for all deserialization. All we need to do when subclassing the JsonAdapterProvider
    /// class is to provide a method for producing JSON from a byte array of some content type, and 
    /// a Serialize implementation.
    /// </summary>
    public abstract class JsonAdapter : IContentTypeProvider
    {
        private readonly JsonProvider JsonProvider = new JsonProvider();

        /// <summary>
        /// The RESTar default UTF8 encoding. An UTF8 encoding without BOM.
        /// </summary>
        protected static readonly Encoding UTF8 = RESTarConfig.DefaultEncoding;

        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public abstract ContentType ContentType { get; }

        /// <inheritdoc />
        public abstract string[] MatchStrings { get; set; }

        /// <inheritdoc />
        public abstract bool CanRead { get; }

        /// <inheritdoc />
        public abstract bool CanWrite { get; }

        /// <inheritdoc />
        public abstract string ContentDispositionFileExtension { get; }

        /// <summary>
        /// Produces JSON that is then used to deserialize to entities of the resource type. The
        /// input stream contains data from the client, in the format of the content type provider.
        /// Read this data and write corresponding JSON to the given output stream.
        /// Include true in the isSingularEntity property when the produced JSON encodes a single 
        /// entity (as opposed to an array of entities).
        /// </summary>
        protected abstract void ProduceJsonArray(Stream inputStream, Stream outputStream);

        /// <inheritdoc />
        public abstract ulong SerializeCollection<T>(IEnumerable<T> entities, Stream stream, IRequest request = null);

        /// <inheritdoc />
        public IEnumerable<T> DeserializeCollection<T>(Stream stream)
        {
            var jsonStream = new RESTarStream(ContentType.JSON);
            try
            {
                ProduceJsonArray(stream, jsonStream);
                foreach (var item in JsonProvider.DeserializeCollection<T>(jsonStream.Rewind()))
                    yield return item;
            }
            finally
            {
                jsonStream.CanClose = true;
                jsonStream.Dispose();
            }
        }

        /// <inheritdoc />
        public IEnumerable<T> Populate<T>(IEnumerable<T> entities, byte[] body)
        {
            var jsonStream = new RESTarStream(ContentType.JSON);
            try
            {
                ProduceJsonArray(new MemoryStream(body), jsonStream);
                return JsonProvider.Populate(entities, jsonStream.GetBytes());
            }
            finally
            {
                jsonStream.CanClose = true;
                jsonStream.Dispose();
            }
        }
    }
}