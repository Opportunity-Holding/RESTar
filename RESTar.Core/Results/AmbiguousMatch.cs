namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a uniquely matched entity was expected for a request, but multiple was found
    /// </summary>
    internal class SafePostAmbiguousMatch : BadRequest
    {
        internal SafePostAmbiguousMatch(int count, string uri) : base(ErrorCodes.AmbiguousMatch,
            $"As part of the SafePost operation, RESTar tried to select entities using '{uri}', expecting either no " +
            $"or a single entity as result. Found {count} entities matching this URI. SafePost, like PUT, can only be " +
            "used on entity resources where zero or exacly one entity is selected by the input conditions.") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a uniquely matched entity was expected for a request, but multiple was found
    /// </summary>
    internal class AmbiguousMatch : BadRequest
    {
        internal AmbiguousMatch() : base(ErrorCodes.AmbiguousMatch,
            "Expected a uniquely matched entity for this operation, but found multiple. " +
            "Manipulating multiple entities is either unsupported or unsafe. Specify additional " +
            "conditions or use the 'unsafe' meta-condition") { }
    }
}