using System.Net;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned when a WebSocket upgrade was performed successfully, and RESTar has taken over the 
    /// context from the network provider.
    /// </summary>
    public class WebSocketUpgradeSuccessful : Success
    {
        internal WebSocketUpgradeSuccessful(IRequest request) : base(request)
        {
            StatusCode = HttpStatusCode.SwitchingProtocols;
            StatusDescription = "Switching protocols";
            TimeElapsed = request.TimeElapsed;
        }
    }
}   