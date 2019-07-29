namespace RESTar.Results
{
    /// <inheritdoc />
    public class InvalidSyntax : BadRequest
    {
        /// <inheritdoc />
        public InvalidSyntax(ErrorCodes errorCode, string message) : base(errorCode, "Syntax error: " + message) { }
    }
}