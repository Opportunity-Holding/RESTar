using System;
using System.Collections.Generic;
using System.IO;
using RESTar.Requests;

namespace RESTar.Palindrom
{
    /// <summary>
    /// The content type provider that bootstraps a Palindrom session onto requests for
    /// text/html.
    /// </summary>
    public class PalindromContentTypeProvider : ContentTypeProviders.IContentTypeProvider
    {
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
        public PalindromContentTypeProvider()
        {
            Name = "Palindrom";
            ContentType = "text/html";
            MatchStrings = new[] {"text/html", "application/palindrom"};
            CanRead = false;
            CanWrite = true;
            ContentDispositionFileExtension = null;
        }

        /// <inheritdoc />
        public ulong SerializeCollection<T>(IEnumerable<T> entities, Stream stream, IRequest request = null) where T : class
        {
            // we write the app shell to the stream, including information about how to communicate with the palindrom session
            // we will need handlers for 
            return 0;
        }

        #region Unused

        /// <inheritdoc />
        /// <summary>
        /// Palindrom only writes bootstrapping data, so this will not be used
        /// </summary>
        public IEnumerable<T> DeserializeCollection<T>(Stream stream) where T : class => throw new NotImplementedException();

        /// <inheritdoc />
        /// <summary>
        /// Palindrom only writes bootstrapping data, so this will not be used
        /// </summary>
        public IEnumerable<T> Populate<T>(IEnumerable<T> entities, byte[] body) where T : class => throw new NotImplementedException();

        #endregion
    }
}