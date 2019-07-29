using System;
using System.Collections.Generic;
using RESTar.Linq;
using RESTar.Meta.Internal;

namespace RESTar.Meta
{
    /// <summary>
    /// Defines the operation of handling a change observed by a property monitoring tree
    /// </summary>
    /// <param name="termRelativeRoot">A term representing the changing object, relative to the root</param>
    /// <param name="oldValue">The old value of the changing object</param>
    /// <param name="newValue">The new and current value of the changing object</param>
    public delegate void ObservedChangeHandler(Term termRelativeRoot, object oldValue, object newValue);

    /// <inheritdoc />
    /// <summary>
    /// Property monitoring trees is a data structure for properties (and properties of properties
    /// etc.) of a given type, along with these properties dependencies on other properties. When 
    /// a change is monitored in some property, a term relative to the root type is generated and
    /// sent to the given callback along with the previous and new values for the changed property.
    /// </summary>
    public class PropertyMonitoringTree : IDisposable
    {
        internal ObservedChangeHandler HandleObservedChange { get; }

        /// <summary>
        /// The component separator to use in output terms from this monitoring tree
        /// </summary>
        public string OutputTermComponentSeparator { get; }

        /// <summary>
        /// The term stub to append all output terms to
        /// </summary>
        public Term Stub { get; }

        /// <summary>
        /// The property links of this monitoring tree
        /// </summary>
        public HashSet<PropertyLink> AllLinks { get; }

        /// <summary>
        /// Creates a new property tree for the given root type
        /// </summary>
        /// <param name="rootType">The root type to monitor</param>
        /// <param name="outputTermComponentSeparator">The component separator to use in output terms</param>
        /// <param name="stub">The term stub to append all output terms to</param>
        /// <param name="handleObservedChange">The handler of output terms and new and old values</param>
        internal PropertyMonitoringTree
        (
            Type rootType,
            string outputTermComponentSeparator,
            Term stub,
            ObservedChangeHandler handleObservedChange
        )
        {
            OutputTermComponentSeparator = outputTermComponentSeparator;
            HandleObservedChange = handleObservedChange;
            Stub = stub;
            AllLinks = new HashSet<PropertyLink>();

            var discoveredTypes = new HashSet<Type>();

            void recurseTree(Type owner, PropertyLink rootWard)
            {
                if (!discoveredTypes.Add(owner)) return;
                if (owner.ImplementsGenericInterface(typeof(IEnumerable<>), out var p))
                {
                    var elementType = p[0];
                    var link = new PropertyLink(this, rootWard, new AnyIndexProperty(elementType, owner));
                    recurseTree(elementType, link);
                }
                foreach (var property in owner.GetDeclaredProperties().Values)
                {
                    var link = new PropertyLink(this, rootWard, property);
                    recurseTree(property.Type, link);
                }
            }

            recurseTree(rootType, null);

            Activate();
        }

        /// <summary>
        /// Makes all links active and registers all event listeners
        /// </summary>
        public void Activate() => AllLinks.ForEach(link => link.Activate());

        /// <inheritdoc />
        public void Dispose() => AllLinks.ForEach(link => link.Dispose());
    }
}