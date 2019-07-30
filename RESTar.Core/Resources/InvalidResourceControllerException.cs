namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an invalid resource controller has been declared
    /// </summary>
    public class InvalidResourceControllerException : RESTarException
    {
        internal InvalidResourceControllerException(string info) : base(ErrorCodes.InvalidResourceControllerDeclaration, info) { }
    }
}