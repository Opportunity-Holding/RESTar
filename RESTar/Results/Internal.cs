using System;
using System.Net;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Internal errors used in RESTar
    /// </summary>
    public abstract class Internal : Error
    {
        internal Internal(ErrorCodes code, string info, Exception ie = null) : base(code, info, ie)
        {
            StatusCode = HttpStatusCode.InternalServerError;
            StatusDescription = "Internal server error";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(Internal)};{RequestInternal?.Resource};{ErrorCode}";
    }
}