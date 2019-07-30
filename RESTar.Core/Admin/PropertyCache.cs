using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;

namespace RESTar.Admin
{
    /// <inheritdoc />
    /// <summary>
    /// Gets the properties discovered by this RESTar instance
    /// </summary>
    [RESTar(Method.GET, Description = description)]
    public class PropertyCache : ISelector<PropertyCache>
    {
        private const string description = "Contains the types and properties discovered by RESTar when " +
                                           "working with the resources of the current RESTar application";

        /// <summary>
        /// The type containing the properties
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// The discovered properties
        /// </summary>
        public IEnumerable<DeclaredProperty> Properties { get; private set; }

        /// <inheritdoc />
        public IEnumerable<PropertyCache> Select(IRequest<PropertyCache> request) => TypeCache
            .DeclaredPropertyCache
            .Select(item => new PropertyCache
            {
                Type = item.Key,
                Properties = item.Value.Values
            })
            .Where(request.Conditions);
    }
}