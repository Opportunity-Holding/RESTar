using System.Net;
using RESTar.Requests;

namespace RESTar.Results
{
    internal class SwitchedTerminal : Success
    {
        internal SwitchedTerminal(IRequest request) : base(request)
        {
            StatusCode = HttpStatusCode.OK;
            StatusDescription = "Switched terminal";
            TimeElapsed = request.TimeElapsed;
        }
    }
}