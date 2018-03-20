﻿using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Logging;

namespace RESTar.Operations
{
    /// <inheritdoc />
    /// <summary>
    /// A RESTar result
    /// </summary>
    public interface IResult : ILogable
    {
        /// <summary>
        /// The status code of the result
        /// </summary>
        HttpStatusCode StatusCode { get; }

        /// <summary>
        /// The description of the status
        /// </summary>
        string StatusDescription { get; }

        /// <summary>
        /// The cookies to set in the response
        /// </summary>
        ICollection<string> Cookies { get; }

        /// <summary>
        /// Finalizes the result and prepares output streams and content types
        /// </summary>
        IFinalizedResult FinalizeResult();
    }

    /// <inheritdoc />
    /// <summary>
    /// Describes a result that is ready to be sent back to the client, for example 
    /// using an HTTP response.
    /// </summary>
    public interface IFinalizedResult : IResult
    {
        /// <summary>
        /// The body contained in the result
        /// </summary>
        Stream Body { get; }

        /// <summary>
        /// The content type of the result body
        /// </summary>
        ContentType ContentType { get; }
    }
}