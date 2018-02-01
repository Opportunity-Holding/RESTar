﻿using RESTar.Http;
using RESTar.Internal;

namespace RESTar.Results.Fail.BadRequest
{
    internal class InvalidExternalDestination : BadRequest
    {
        internal InvalidExternalDestination(HttpRequest request, string message) : base(ErrorCodes.InvalidDestination,
            $"RESTar could not upload entities to destination at '{request.URI}': {message}") { }
    }
}