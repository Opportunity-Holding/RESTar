using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RESTar.ContentTypeProviders;
using RESTar.Requests;

namespace RESTar.Palindrom
{
    /// <inheritdoc />
    /// <summary>
    /// The content type provider that bootstraps a Palindrom session onto requests for
    /// text/html.
    /// </summary>
    public class PalindromBootstrapper : IContentTypeProvider
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
        public PalindromBootstrapper()
        {
            Name = nameof(PalindromBootstrapper);
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
            // then we set up the palindrom session from here, assign cookies etc.

            if (request == null)
                throw new Exception("No request!");

            // for now, let's just take the first result and use that
            var entity = entities.FirstOrDefault() ?? throw new InvalidOperationException("No root object was selected by request!");

            var patchRequest = request.Context.CreateRequest<T>(Method.PATCH);
            patchRequest.Selector = () => new[] {entity};

            // this creates a session ID, sets it as a cookie the the request, and remembers the root until later.
            var session = Session.Create(request, entity, patchRequest);

            request.Cookies.Add
            (
                name: Session.SessionCookieName,
                value: session.ID
                // probably add some attributes
            );

            // then we write the appropriate html data to the stream, to set up Palindrom on the client-side, and load the root.
            // for now, let's just return the root as plain text, along with text instructions on how to proceed.

            using (var writer = new StreamWriter(stream: stream, leaveOpen: true, encoding: Encoding.UTF8, bufferSize: 1024))
            {
                writer.WriteLine("<!DOCTYPE html>");
                writer.WriteLine("<html lang=\"en-US\">");
                writer.WriteLine("<body>");
                writer.WriteLine("Bootstrapping code will be run here.");
                writer.Write("<br>");
                writer.WriteLine($"A new session was created with id: {session.ID}");
                writer.Write("<br>");
                writer.WriteLine("Current state:");
                writer.Write("<br>");
                writer.WriteLine(Providers.Json.Serialize(entity));
                writer.Write("<br>");
                writer.WriteLine("</body>");
                writer.WriteLine("</html>");
            }

            return 1;
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