﻿using RESTar.Http;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot upload entities to an external destination
    /// </summary>
    public class InvalidExternalDestination : BadRequest
    {
        internal InvalidExternalDestination(HttpRequest request, string message) : base(ErrorCodes.InvalidDestination,
            $"RESTar could not upload entities to destination at '{request.URI}': {message}") { }
    }
}