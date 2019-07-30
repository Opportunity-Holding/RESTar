using System.Net;

namespace RESTar.Results
{
    /// <inheritdoc />
    public class FeatureNotImplemented : Error
    {
        /// <inheritdoc />
        public FeatureNotImplemented(string info) : base(ErrorCodes.NotImplemented, info)
        {
            StatusCode = HttpStatusCode.NotImplemented;
            StatusDescription = "Not implemented";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(FeatureNotImplemented)};{RequestInternal?.Resource};{ErrorCode}";
    }
}