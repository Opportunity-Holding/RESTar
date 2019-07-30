namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an invalid enum declaration was referenced from a RESTar resource
    /// </summary>
    public class InvalidReferencedEnumDeclarationException : RESTarException
    {
        internal InvalidReferencedEnumDeclarationException(string info) : base(ErrorCodes.InvalidReferencedEnumDeclaration, info) { }
    }
}