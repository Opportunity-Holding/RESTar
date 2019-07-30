namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error with an external protocol provider
    /// </summary>
    public class InvalidContentTypeProviderException : RESTarException
    {
        internal InvalidContentTypeProviderException(string message) : base(ErrorCodes.InvalidContentTypeProvider,
            "An error was found in an external IContentTypeProvider: " + message) { }
    }
}