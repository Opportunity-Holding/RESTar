using System;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar found a non-RESTar response for a remote RESTar request
    /// </summary>
    internal sealed class ExternalServiceNotRESTar : NotFound
    {
        internal ExternalServiceNotRESTar(Uri uri, Exception ie = null) : base(ErrorCodes.ExternalServiceNotRESTar,
            $"A remote request was made to '{uri}', but the response was not recognized as a compatible RESTar service response", ie) { }
    }
}