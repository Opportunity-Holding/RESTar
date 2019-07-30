using System;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid resource view declaration
    /// </summary>
    public class InvalidResourceViewDeclarationException : RESTarException
    {
        internal InvalidResourceViewDeclarationException(Type view, string message) : base(ErrorCodes.InvalidResourceViewDeclaration,
            $"Invalid resource view declaration for view '{view.Name}' in resource '{view.DeclaringType?.RESTarTypeName()}'. {message}") { }
    }
}