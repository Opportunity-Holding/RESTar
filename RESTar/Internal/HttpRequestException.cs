using System;

namespace RESTar.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an HTTP request failed due to some error
    /// </summary>
    internal class HttpRequestException : Exception
    {
        /// <inheritdoc />
        public HttpRequestException(string message) : base(message) { }
    }
}