using System;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid terminal resource declaration
    /// </summary>
    public class InvalidBinaryDeclarationException : RESTarException
    {
        internal InvalidBinaryDeclarationException(Type binary, string message) : base(ErrorCodes.InvalidBinaryResourceDeclaration,
            $"Invalid binary resource declaration '{binary.RESTarTypeName()}'. Binary resource types {message}") { }
    }
}