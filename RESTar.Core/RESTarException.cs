using System;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// A common base class for all RESTar exceptions
    /// </summary>
    public abstract class RESTarException : Exception
    {
        /// <summary>
        /// The error code for this error
        /// </summary>
        public ErrorCodes ErrorCode { get; }

        /// <inheritdoc />
        protected RESTarException(ErrorCodes errorCode, string message, Exception ie = null) : base(message, ie)
        {
            ErrorCode = errorCode;
        }
    }
}