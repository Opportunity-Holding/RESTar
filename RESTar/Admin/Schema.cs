using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using static RESTar.Method;
using static RESTar.Requests.Operators;

namespace RESTar.Admin
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="JObject" />
    /// <summary>
    /// The Schema resource provides schemas for non-dynamic RESTar resources
    /// </summary>
    [RESTar(GET, Description = description)]
    internal class Schema : JObject, ISelector<Schema>
    {
        private const string description = "The Schema resource provides schemas for " +
                                           "non-dynamic RESTar resources.";

        /// <summary>
        /// The name of the resource to get the schema for
        /// </summary>
        public string Resource { private get; set; }

        /// <inheritdoc />
        public IEnumerable<Schema> Select(IRequest<Schema> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!(request.Conditions.Get("resource", EQUALS)?.Value is string resourceName))
                throw new Exception("Invalid syntax in request to RESTar.Schema. Format: " +
                                    "/schema/resource=insert_resource_name_here");
            var res = Meta.Resource.Find(resourceName) as IEntityResource;
            if (res?.IsDynamic != false) return null;
            var schema = new Schema();
            res.Members.Values.ForEach(p => schema[p.Name] = p.Type.FullName);
            return new[] {schema};
        }
    }
}