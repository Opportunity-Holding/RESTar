using System;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid event type declration
    /// </summary>
    public class InvalidEventDeclarationException : RESTarException
    {
        internal InvalidEventDeclarationException(Type eventType, string message) : base(ErrorCodes.InvalidEventDeclaration,
            $"Invalid event resource type declaration for '{eventType.RESTarTypeName()}'. Event resource types {message}") { }
    }
}