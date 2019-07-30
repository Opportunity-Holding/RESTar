namespace RESTar.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar requires a configuration file for setting up API keys and/or CORS origins, but
    /// no configuration file was found.
    /// </summary>
    internal class MissingConfigurationFile : RESTarException
    {
        internal MissingConfigurationFile(string info) : base(ErrorCodes.MissingConfigurationFile, info) { }
    }
}