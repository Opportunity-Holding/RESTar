using RESTar.ProtocolProviders;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters a request that was not compliant with the specified protocol 
    /// </summary>
    internal class NotCompliantWithProtocol : BadRequest
    {
        internal NotCompliantWithProtocol(IProtocolProvider provider, string message) : base(ErrorCodes.NotCompliantWithProtocol,
            $"The request was not compliant with the {provider.ProtocolName} protocol. {message}") { }
    }
}