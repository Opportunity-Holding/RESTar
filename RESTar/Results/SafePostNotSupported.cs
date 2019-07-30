namespace RESTar.Results
{
    internal class SafePostNotSupported : FeatureNotImplemented
    {
        internal SafePostNotSupported(string info) : base("SafePost is not supported by this resource " + info) { }
    }
}