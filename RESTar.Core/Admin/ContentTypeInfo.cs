using System.Collections.Generic;

namespace RESTar.Admin
{
    /// <summary>
    /// Describes a content type
    /// </summary>
    public class ContentTypeInfo
    {
        /// <summary>
        /// The name of the content type
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The MIME type of this content type
        /// </summary>
        public string MimeType { get; internal set; }

        /// <summary>
        /// Can this content type be used to read data?
        /// </summary>
        public bool CanRead { get; internal set; }

        /// <summary>
        /// Can this content type be used to write data?
        /// </summary>
        public bool CanWrite { get; internal set; }

        /// <summary>
        /// The MIME type string bindings used for the protocol provider
        /// </summary>
        public IEnumerable<string> Bindings { get; internal set; }
    }
}