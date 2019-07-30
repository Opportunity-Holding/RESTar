namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid member inside a resource declaration
    /// </summary>
    public class InvalidResourceMemberException : RESTarException
    {
        internal InvalidResourceMemberException(string info) : base(ErrorCodes.InvalidResourceMember, info) { }
    }
}