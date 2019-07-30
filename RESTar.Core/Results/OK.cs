using System.Net;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    public abstract class OK : RequestSuccess
    {
        /// <inheritdoc />
        protected OK(IRequest request) : base(request)
        {
            StatusCode = HttpStatusCode.OK;
            StatusDescription = "OK";
        }
    }
}