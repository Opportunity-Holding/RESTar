﻿using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class NotAcceptable : RESTarException
    {
        internal NotAcceptable(MimeType unsupported) : base(ErrorCodes.NotAcceptable,
            $"Unsupported accept format: '{unsupported.TypeCodeString}'")
        {
            StatusCode = HttpStatusCode.NotAcceptable;
            StatusDescription = "Not acceptable";
        }
    }
}