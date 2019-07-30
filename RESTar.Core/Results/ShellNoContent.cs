using System;
using System.Net;

namespace RESTar.Results
{
    internal class ShellNoContent : Success
    {
        internal ShellNoContent(ITraceable trace, TimeSpan elapsed) : base(trace)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No content";
            TimeElapsed = elapsed;
        }
    }
}