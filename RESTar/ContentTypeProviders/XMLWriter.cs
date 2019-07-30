using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using RESTar.Requests;

namespace RESTar.ContentTypeProviders
{
    /// <inheritdoc />
    /// <summary>
    /// A simple XML writer implementation, enabling RESTar output to be 
    /// sent with the application/xml content type.
    /// </summary>
    public class XMLWriter : IContentTypeProvider
    {
        private const string XMLMimeType = "application/xml";
        private const string RESTarSpecific = "application/restar-xml";
        private const string Brief = "xml";

        /// <inheritdoc />
        public string Name => "XML";

        /// <inheritdoc />
        public ContentType ContentType { get; } = "application/xml; charset=utf-8";

        /// <inheritdoc />
        public string[] MatchStrings { get; set; } = {XMLMimeType, RESTarSpecific, Brief};

        /// <inheritdoc />
        public bool CanRead => false;

        /// <inheritdoc />
        public bool CanWrite => true;

        /// <inheritdoc />
        public string ContentDispositionFileExtension => ".xml";

        private static readonly JsonProvider JsonProvider;
        private static readonly byte[] XMLHeader;

        static XMLWriter()
        {
            JsonProvider = new JsonProvider();
            XMLHeader = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
        }

        /// <inheritdoc />
        public ulong SerializeCollection<T>(IEnumerable<T> entities, Stream stream, IRequest request = null)
        {
            var count = JsonProvider.SerializeCollection(entities, stream, request);
            stream.Seek(0, SeekOrigin.Begin);
            XmlSerializeJsonStream(stream);
            return count;
        }

        private static void XmlSerializeJsonStream(Stream stream)
        {
            using (var streamReader = new StreamReader(stream, RESTarConfig.DefaultEncoding, false, 1024, true))
            {
                var json = $"{{\"entity\":{streamReader.ReadToEnd()}}}";
                var xml = JsonConvert.DeserializeXmlNode(json, "root", true);
                stream.Seek(0, SeekOrigin.Begin);
                stream.Write(XMLHeader, 0, XMLHeader.Length);
                xml.Save(stream);
            }
        }

        /// <inheritdoc />
        public IEnumerable<T> DeserializeCollection<T>(Stream body) => throw new NotImplementedException();

        /// <inheritdoc />
        public IEnumerable<T> Populate<T>(IEnumerable<T> entities, byte[] body) =>
            throw new NotImplementedException();
    }
}