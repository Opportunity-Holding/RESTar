using System;
using System.Collections.Generic;
using RESTar.Admin;
using RESTar.Operations;
using RESTar.Resources;

namespace RESTar.Internal
{
    /// <summary>
    /// The common non-generic interface for all resource entities used by RESTar
    /// </summary>
    public interface IResource : IEqualityComparer<IResource>, IComparable<IResource>
    {
        /// <summary>
        /// The name of this resource
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Is this resource editable?
        /// </summary>
        bool Editable { get; }

        /// <summary>
        /// The available methods for this resource
        /// </summary>
        IReadOnlyList<Methods> AvailableMethods { get; }

        /// <summary>
        /// Resource descriptions are visible in the AvailableMethods resource
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The target type for this resource
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// The alias of this resource (if any)
        /// </summary>
        string Alias { get; set; }

        /// <summary>
        /// The resource provider that generated this resource
        /// </summary>
        string Provider { get; }

        /// <summary>
        /// Returns true if and only if this resource was claimed by the given 
        /// ResourceProvider type
        /// </summary>
        bool ClaimedBy<T>() where T : ResourceProvider;

        /// <summary>
        /// Is this a DDictionary resource?
        /// </summary>
        bool IsDDictionary { get; }

        /// <summary>
        /// Does this resource contain dynamic members?
        /// </summary>
        bool IsDynamic { get; }

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
        /// Are runtime-defined conditions allowed in requests to this resource?
        /// </summary>
        bool DynamicConditionsAllowed { get; }

        /// <summary>
        /// Is this a Starcounter resource?
        /// </summary>
        bool IsStarcounterResource { get; }

        /// <summary>
        /// Is this a singleton resource?
        /// </summary>
        bool IsSingleton { get; }

        /// <summary>
        /// A friendly label for this resource
        /// </summary>
        string AliasOrName { get; }

        /// <summary>
        /// Does this resource require validation on insertion and updating?
        /// </summary>
        bool RequiresValidation { get; }

        /// <summary>
        /// Gets a ResourceProfile for this resource
        /// </summary>
        ResourceProfile ResourceProfile { get; }
    }

    /// <inheritdoc />
    /// <summary>
    /// The common generic interface for all resource entities used by RESTar
    /// </summary>
    public interface IResource<T> : IResource where T : class
    {
        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        Selector<T> Select { get; }

        /// <summary>
        /// RESTar inserter (don't use)
        /// </summary>
        Inserter<T> Insert { get; }

        /// <summary>
        /// RESTar updater (don't use)
        /// </summary>
        Updater<T> Update { get; }

        /// <summary>
        /// RESTar deleter (don't use)
        /// </summary>
        Deleter<T> Delete { get; }

        /// <summary>
        /// RESTar counter (don't use)
        /// </summary>
        Counter<T> Count { get; }

        /// <summary>
        /// RESTar profiler (don't use)
        /// </summary>
        Profiler<T> Profile { get; }
    }

    internal interface IResourceInternal
    {
        IReadOnlyList<IResource> InnerResources { get; set; }
        string Description { get; set; }
        IReadOnlyList<Methods> AvailableMethods { get; set; }
    }
}