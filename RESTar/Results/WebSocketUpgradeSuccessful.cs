using System.Net;
using System.Threading.Tasks;
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
        public Task WebSocketLifeTime { get; }
        internal WebSocketUpgradeSuccessful(IRequest request, Task lifetimeTask) : base(request)
        {
            WebSocketLifeTime = lifetimeTask;
            StatusCode = HttpStatusCode.SwitchingProtocols;
            StatusDescription = "Switching protocols";
            TimeElapsed = request.TimeElapsed;
        }
    }
}   