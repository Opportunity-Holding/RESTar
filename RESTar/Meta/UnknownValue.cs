namespace RESTar.Meta
{
    /// <summary>
    /// Represents an unknown value, in the context of handling changes to properties.
    /// Unknown values can occur in oldValue when handling changes that were pushed from
    /// other defining properties. In that case, the state change has taken place before
    /// the handler can be called.
    /// </summary>
    public struct UnknownValue { }
}