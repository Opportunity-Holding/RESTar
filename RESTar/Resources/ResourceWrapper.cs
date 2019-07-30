// ReSharper disable UnusedTypeParameter

using RESTar.Meta.Internal;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// ResourceWrapper instances enable resource declarations for resources in read-only 
    /// assemblies and/or in assemblies where a reference to RESTar is not practical.
    /// </summary>
    public abstract class ResourceWrapper<T> : IResourceWrapper where T : class { }
}