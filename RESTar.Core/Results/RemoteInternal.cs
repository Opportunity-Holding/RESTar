namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// An Internal error received from a remote RESTar service
    /// </summary>
    internal class RemoteInternal : Internal
    {
        /// <inheritdoc />
        internal RemoteInternal(ErrorCodes code) : base(code, null) { }
    }
}   