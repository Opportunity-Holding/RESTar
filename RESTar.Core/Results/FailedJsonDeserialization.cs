using Newtonsoft.Json;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error when reading JSON
    /// </summary>
    internal class FailedJsonDeserialization : BadRequest
    {
        internal FailedJsonDeserialization(JsonReaderException ie) : base(ErrorCodes.FailedJsonDeserialization,
            $"JSON syntax error: {ie.Message}", ie) { }
    }
}