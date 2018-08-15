﻿using System;
using System.Net;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Exceptions that should be treated as bad requests
    /// </summary>
    public abstract class NotFound : Error
    {
        internal NotFound(ErrorCodes code, string info, Exception ie = null) : base(code, info, ie)
        {
            StatusCode = HttpStatusCode.NotFound;
            StatusDescription = "Not found";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(NotFound)};{RequestInternal?.Resource};{ErrorCode}";
    }
}