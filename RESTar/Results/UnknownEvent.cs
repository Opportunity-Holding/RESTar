namespace RESTar.Results
{
    internal class UnknownEvent : NotFound
    {
        internal UnknownEvent(string info) : base(ErrorCodes.UnknownEventType, info) { }
    }

    internal class AmbiguousEventMatch : NotFound
    {
        internal AmbiguousEventMatch(string info) : base(ErrorCodes.AmbiguousMatch, info) { }
    }
}