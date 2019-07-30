using System.Net;
using RESTar.Meta;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters a request with an unallowed method (according to access rights or resource declaration)
    /// search string.
    /// </summary>
    public class MethodNotAllowed : Error
    {
        /// <summary>
        /// Was the error generated because the client was not authorized to use the method? As opposed to
        /// the method not being enabled for the resource.
        /// </summary>
        public bool NotAuthorized { get; }

        /// <inheritdoc />
        public MethodNotAllowed(Method method, ITarget target, bool failedAuth) : base(ErrorCodes.MethodNotAllowed,
            $"Method '{method}' is not available for resource '{target.Name}'{(failedAuth ? " for the current API key" : "")}")
        {
            NotAuthorized = failedAuth;
            StatusCode = HttpStatusCode.MethodNotAllowed;
            StatusDescription = "Method not allowed";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(MethodNotAllowed)};{RequestInternal?.Resource};{ErrorCode}";
    }
}