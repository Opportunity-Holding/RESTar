namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot uniquely identify a resource by some search string
    /// </summary>
    internal class AmbiguousResource : NotFound
    {
        internal AmbiguousResource(string searchString) : base(ErrorCodes.AmbiguousResource,
            $"RESTar could not uniquely identify a resource by '{searchString}'. Try qualifying the name further. ") { }
    }
}