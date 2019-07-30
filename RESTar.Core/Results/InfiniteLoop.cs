using System.Net;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an infinite loop of recursive internal requests
    /// </summary>
    public class InfiniteLoop : Error
    {
        internal InfiniteLoop() : base(ErrorCodes.InfiniteLoopDetected,
            "RESTar encountered a potentially infinite loop of recursive internal calls.")
        {
            StatusCode = (HttpStatusCode) 508;
            StatusDescription = "Infinite loop detected";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(InfiniteLoop)};{RequestInternal?.Resource};{ErrorCode}";
    }
}