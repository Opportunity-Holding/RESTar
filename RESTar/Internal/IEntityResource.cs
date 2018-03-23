using System.Collections.Generic;
using RESTar.Admin;
using RESTar.Deflection;
using RESTar.Operations;
using RESTar.Resources;

namespace RESTar.Internal
{
    internal interface ITerminalResourceInternal<T> : ITerminalResourceInternal where T : class
    {
        void InstantiateFor(WebSocket webSocket, IRequest<T> assignments);
    }

    internal interface ITerminalResourceInternal : ITerminalResource
    {
        void InstantiateFor(WebSocket webSocket);
    }

    /// <inheritdoc />
    /// <summary>
    /// A common non-generic interface for terminal resources
    /// </summary>
    public interface ITerminalResource<T> : ITerminalResource where T : class { }

    /// <inheritdoc />
    /// <summary>
    /// A common non-generic interface for terminal resources
    /// </summary>
    public interface ITerminalResource : IResource { }

    /// <inheritdoc cref="ITarget" />
    /// <summary>
    /// The common non-generic interface for all entity resource entities used by RESTar
    /// </summary>
    public interface IEntityResource : IResource
    {
        /// <summary>
        /// Is this resource editable?
        /// </summary>
        bool Editable { get; }

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
        /// Are runtime-defined conditions allowed in requests to this resource?
        /// </summary>
        bool DynamicConditionsAllowed { get; }

        /// <summary>
        /// Are the public instance properties defined in this resource's type 
        /// flagged (preceded by $) in the REST API to avoid capture against 
        /// dynamic properties?
        /// </summary>
        bool DeclaredPropertiesFlagged { get; }

        /// <summary>
        /// The binding rule to use when binding output terms for this resource
        /// </summary>
        TermBindingRules OutputBindingRule { get; }

        /// <summary>
        /// Is this a singleton resource?
        /// </summary>
        bool IsSingleton { get; }

        /// <summary>
        /// Does this resource require validation on insertion and updating?
        /// </summary>
        bool RequiresValidation { get; }

        /// <summary>
        /// Gets a ResourceProfile for this resource
        /// </summary>
        ResourceProfile ResourceProfile { get; }

        /// <summary>
        /// The views for this resource
        /// </summary>
        IEnumerable<IView> Views { get; }

        /// <summary>
        /// Does this resource have a separate authentication
        /// for REST requests?
        /// </summary>
        bool RequiresAuthentication { get; }
    }

    /// <inheritdoc cref="ITarget{T}" />
    /// <summary>
    /// The common generic interface for all entity resources used by RESTar
    /// </summary>
    public interface IEntityResource<T> : IResource<T>, IEntityResource where T : class
    {
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

        /// <summary>
        /// RESTar authenticate (don't use)
        /// </summary>
        Authenticator<T> Authenticate { get; }

        /// <summary>
        /// The Views registered for this resource
        /// </summary>
        IReadOnlyDictionary<string, View<T>> ViewDictionary { get; }
    }
}