namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// A Bad Request error received from a remote RESTar service 
    /// </summary>
    internal class RemoteBadRequest : BadRequest
    {
        /// <inheritdoc />
        internal RemoteBadRequest(ErrorCodes code) : base(code, null) { }
    }
}