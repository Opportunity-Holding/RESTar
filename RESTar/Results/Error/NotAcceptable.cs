﻿using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Fail
{
    internal class NotAcceptable : RESTarError
    {
        internal NotAcceptable(MimeType unsupported) : base(ErrorCodes.NotAcceptable,
            $"Unsupported accept format: '{unsupported.TypeCodeString}'")
        {
            StatusCode = HttpStatusCode.NotAcceptable;
            StatusDescription = "Not acceptable";
        }
    }
}