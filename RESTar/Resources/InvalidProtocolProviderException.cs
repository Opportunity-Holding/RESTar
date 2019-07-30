namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error with an external protocol provider
    /// </summary>
    public class InvalidProtocolProviderException : RESTarException
    {
        internal InvalidProtocolProviderException(string message) : base(ErrorCodes.InvalidProtocolProvider,
            "An error was found in an external IProtocolProvider: " + message) { }
    }
}