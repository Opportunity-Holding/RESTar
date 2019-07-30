using System.IO;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Holds a binary content, used as result from requests to binary resources
    /// </summary>
    public class Binary : Content
    {
        /// <inheritdoc />
        public override Stream Body { get; set; }

        /// <inheritdoc />
        public Binary(IRequest request, Stream stream, ContentType contentType) : base(request)
        {
            Headers.ContentType = contentType;
            Body = stream;
            if (Body.CanSeek)
                Body.Seek(0, SeekOrigin.Begin);
            IsSerialized = true;
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(Binary)};{Request.Resource};";
    }
}