using System;
using System.Collections.Generic;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Registers a new RESTar resource and provides permissions. If no methods are 
    /// provided in the constructor, all methods are made available for this resource.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RESTarAttribute : Attribute
    {
        /// <summary>
        /// The methods declared as available for this RESTar resource. Not applicable for 
        /// terminal resources.
        /// </summary>
        public IReadOnlyList<Method> AvailableMethods { get; }

        /// <summary>
        /// If true, unknown conditions encountered when handling incoming requests
        /// will be passed through as dynamic. This allows for a dynamic handling of
        /// members, both for condition matching and for entities returned from the 
        /// resource selector. Not applicable for terminal resources.
        /// </summary>
        public bool AllowDynamicConditions { get; set; }

        /// <summary>
        /// This will place a dollar sign ($) before all statically defined properties 
        /// for this type in the REST API, to avoid capture with dynamic members. Always 
        /// true for DDictionary resources. Not applicable for terminal resources.
        /// </summary>
        public bool FlagStaticMembers { get; set; }

        /// <summary>
        /// Singleton resources get special treatment in the view. They have no list 
        /// view, but only entity view. Good for settings, reports etc. Not applicable for 
        /// terminal resources.
        /// </summary>
        public bool Singleton { get; set; }

        /// <summary>
        /// Resource descriptions are visible in the AvailableResource resource
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Should this resource, with methods GET and REPORT, be included in all access scopes, 
        /// regardless of API keys in the configuration file? This is useful for global resources 
        /// in foreign assemblies. The API will still require API key if requireApiKey is set to 
        /// true in the call to RESTarConfig.Init(), but this resource will be included in each 
        /// key's scope.
        /// </summary>
        public bool GETAvailableToAll { get; set; }

        /// <summary>
        /// Does this attribute describe a declared resource type?
        /// </summary>
        internal bool IsDeclared => !(this is RESTarProceduralAttribute);

        /// <inheritdoc />
        /// <summary>
        /// Registers a new RESTar resource and provides permissions. If no methods are 
        /// provided in the constructor, all methods are made available for this resource.
        /// </summary>
        public RESTarAttribute(params Method[] methodRestrictions) => AvailableMethods = methodRestrictions.ResolveMethodRestrictions();
    }
}