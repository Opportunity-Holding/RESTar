namespace RESTar.Meta
{
    /// <summary>
    /// The rule used for binding terms to a resource
    /// </summary>
    public enum TermBindingRule
    {
        /// <summary>
        /// First finds declared properties, then creates dynamic properties
        /// for any unknown property
        /// </summary>
        DeclaredWithDynamicFallback,

        /// <summary>
        /// First finds dynamic properties, then (at runtime) searches for 
        /// declared properties if no dynamic property was found
        /// </summary>
        DynamicWithDeclaredFallback,

        /// <summary>
        /// Binds only declared properties, and throws an exception if no 
        /// declared property was found
        /// </summary>
        OnlyDeclared
    }
}