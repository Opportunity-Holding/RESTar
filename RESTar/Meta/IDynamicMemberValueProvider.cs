namespace RESTar.Meta
{
    /// <summary>
    /// Defines custom accessors to a dynamic member
    /// </summary>
    public interface IDynamicMemberValueProvider
    {
        /// <summary>
        /// Gets the value of the given member by name (case insensitive)
        /// </summary>
        bool TryGetValue(string memberName, out object value, out string actualMemberName);

        /// <summary>
        /// Sets the value of the given member to the given value
        /// </summary>
        bool TrySetValue(string memberName, object value);
    }
}