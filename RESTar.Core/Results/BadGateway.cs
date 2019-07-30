namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters a bad gateway when connecting to a remote RESTar service
    /// </summary>
    public class BadGateway : Internal
    {
        internal BadGateway(string uri) : base(ErrorCodes.ExternalServiceNotRESTar, "Encountered a bad gateway when connecting to " + uri) { }
    }
}