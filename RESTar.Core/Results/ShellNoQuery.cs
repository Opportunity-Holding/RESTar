using System.Net;

namespace RESTar.Results
{
    internal class ShellNoQuery : Success
    {
        internal ShellNoQuery(ITraceable trace) : base(trace)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No query";
            TimeElapsed = default;
        }
    }
}