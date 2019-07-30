namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid resource declaration
    /// </summary>
    public class InvalidResourceDeclarationException : RESTarException
    {
        internal InvalidResourceDeclarationException(string info) : base(ErrorCodes.InvalidResourceDeclaration, info) { }
    }
}