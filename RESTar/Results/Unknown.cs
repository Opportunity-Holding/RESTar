using System;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an unkown error
    /// </summary>
    internal class Unknown : Internal
    {
        /// <inheritdoc />
        public Unknown(Exception e) : base(ErrorCodes.Unknown, e.Message, e) { }
    }
}