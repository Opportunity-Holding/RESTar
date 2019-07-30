using System.Net;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a regular HTTP request was made to a resource that requires a WebSocket connection
    /// </summary>
    public class UpgradeRequired : Error
    {
        internal UpgradeRequired(string terminalName) : base(ErrorCodes.UpgradeRequired,
            $"Connections to terminal resource '{terminalName}' must include a WebSocket upgrade handshake")
        {
            StatusCode = HttpStatusCode.UpgradeRequired;
            StatusDescription = "Upgrade required";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(UpgradeRequired)};{RequestInternal?.Resource};{ErrorCode}";
    }
}