namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an invalid number of entities was contained in a request body
    /// </summary>
    internal class InvalidInputCount : BadRequest
    {
        internal InvalidInputCount() : base(ErrorCodes.DataSourceFormat,
            "Invalid input entity count. Expected a single entity as input for this operation, " +
            "but found additional.") { }
    }
}