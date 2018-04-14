﻿using System.Net;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client when no content was selected in a request
    /// </summary>
    public class NoContent : RequestSuccess
    {
        internal NoContent(IRequest request) : base(request)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No content";
            Headers.Info = "No entities found matching request.";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(NoContent)};{Request.Resource};";
    }
}