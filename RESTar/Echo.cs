using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using static RESTar.Method;

namespace RESTar
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="JObject" />
    /// <summary>
    /// The Echo resource is a test and utility resource that returns the 
    /// request conditions as an object.
    /// </summary>
    [RESTar(GET, AllowDynamicConditions = true, Description = description)]
    public class Echo : JObject, ISelector<Echo>
    {
        private const string description = "The Echo resource is a test and utility entity resource that " +
                                           "returns the request conditions as an entity.";

        private Echo() { }

        private Echo(object thing) : base(thing) { }

        /// <inheritdoc />
        public IEnumerable<Echo> Select(IRequest<Echo> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var members = request.Conditions.Select(c => new JProperty(c.Key, c.Value));
            var body = request.GetBody().Deserialize<JObject>();
            if (body != null) members = members.Union<JProperty>(body.SelectMany(item => item.Properties()), EqualityComparer);
            var echo = new Echo(members);
            TypeCache.ClearTermsFor<Echo>();
            yield return echo;
        }
    }
}