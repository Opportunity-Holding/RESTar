namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot get entities from an external source
    /// </summary>
    internal class InvalidExternalSource : BadRequest
    {
        internal InvalidExternalSource(string uri, string message) : base(ErrorCodes.InvalidSourceData,
            $"RESTar could not get entities from source at '{uri}'. {message}") { }
    }
}