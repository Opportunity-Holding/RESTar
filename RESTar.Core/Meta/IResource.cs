using System;
using System.Collections.Generic;

namespace RESTar.Meta
{
    /// <inheritdoc cref="ITarget" />
    /// <inheritdoc cref="IEqualityComparer{T}" />
    /// <inheritdoc cref="IComparable{T}" />
    /// <summary>
    /// The common non-generic interface for all resources used by RESTar
    /// </summary>
    public interface IResource : ITarget, IEqualityComparer<IResource>, IComparable<IResource>
    {
        /// <summary>
        /// The available methods for this resource
        /// </summary>
        IReadOnlyCollection<Method> AvailableMethods { get; }

        /// <summary>
        /// Is this resource only available for internal requests?
        /// </summary>
        bool IsInternal { get; }

        /// <summary>
        /// Is this resource available for all requests?
        /// </summary>
        bool IsGlobal { get; }

        /// <summary>
        /// Is this resource an inner resource of some other resource?
        /// </summary>
        bool IsInnerResource { get; }

        /// <summary>
        /// The name of the parent resource, if this is an inner resource
        /// </summary>
        string ParentResourceName { get; }

        /// <summary>
        /// Is this resource declared as available to all, regardless of API keys?
        /// </summary>
        bool GETAvailableToAll { get; }

        /// <summary>
        /// An interface type to use instead of this type when determing the public instance 
        /// members of this resource type. The resource type must implement this interface.
        /// </summary>
        Type InterfaceType { get; }

        /// <summary>
        /// The kind of resource, for example entity resource
        /// </summary>
        ResourceKind ResourceKind { get; }
    }

    /// <inheritdoc cref="IResource" />
    /// <inheritdoc cref="ITarget{T}" />
    /// <summary>
    /// The common generic interface for all resources used by RESTar
    /// </summary>
    public interface IResource<T> : IResource, ITarget<T> where T : class { }
}