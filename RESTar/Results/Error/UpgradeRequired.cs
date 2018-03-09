﻿using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a regular HTTP request was made to a resource that requires a WebSocket connection
    /// </summary>
    public class UpgradeRequired : RESTarError
    {
        internal UpgradeRequired(string terminalName) : base(ErrorCodes.UpgradeRequired,
            $"Connections to terminal resource '{terminalName}' must include a WebSocket upgrade handshake")
        {
            StatusCode = HttpStatusCode.UpgradeRequired;
            StatusDescription = "Upgrade required";
        }
    }
}