using System;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error with an external resource provider
    /// </summary>
    public class InvalidEntityResourceProviderException : RESTarException
    {
        internal InvalidEntityResourceProviderException(Type providerType, string message) : base(ErrorCodes.EntityResourceProviderError,
            $"An error was found in the declaration of entity resource provider '{providerType.RESTarTypeName()}': {message}") { }
    }
}