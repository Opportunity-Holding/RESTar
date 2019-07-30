namespace RESTar.NetworkProviders
{
    /// <summary>
    /// Specifies the operations of a RESTar network provider
    /// </summary>
    public interface INetworkProvider
    {
        /// <summary>
        /// Adds HTTP request bindings for the given methods on the given port and root URI
        /// </summary>
        void AddRoutes(Method[] methods, string rootUri, ushort port);

        /// <summary>
        /// Removes HTTP request bindings for the given methods on the given port and root URI
        /// </summary>
        void RemoveRoutes(Method[] methods, string uri, ushort port);
    }
}