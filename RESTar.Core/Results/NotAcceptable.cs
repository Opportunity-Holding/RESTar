using System.Net;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid or unsupported content type in the Accept header of a request.
    /// </summary>
    public class NotAcceptable : Error
    {
        /// <inheritdoc />
        public NotAcceptable(string headerValue) : base(ErrorCodes.NotAcceptable,
            $"No supported media types were found in the Accept header. Found '{headerValue}'")
        {
            StatusCode = HttpStatusCode.NotAcceptable;
            StatusDescription = "Not acceptable";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(NotAcceptable)};{RequestInternal?.Resource};{ErrorCode}";
    }
}