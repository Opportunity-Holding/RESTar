using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders;
using RESTar.Linq;
using RESTar.Meta;

namespace RESTar.Requests.Processors
{
    /// <inheritdoc cref="List{T}" />
    /// <inheritdoc cref="IProcessor" /> 
    /// <summary>
    /// Selects a set of properties from an IEnumerable of entities
    /// </summary>
    public class Select : List<Term>, IProcessor
    {
        internal Select(IEntityResource resource, string keys, ICollection<string> dynDomain) => keys
            .Split(',')
            .Distinct()
            .Select(key => resource.MakeOutputTerm(key, dynDomain))
            .ForEach(Add);

        private Select(Select other) : base(other) { }
        internal Select GetCopy() => new Select(this);

        internal JObject Apply<T>(T entity)
        {
            var jobj = new JObject();
            ForEach(term =>
            {
                if (jobj[term.Key] != null) return;
                object val = term.Evaluate(entity, out var actualKey);
                jobj[actualKey] = val == null ? null : JToken.FromObject(val, JsonProvider.Serializer);
            });
            return jobj;
        }

        /// <summary>
        /// Selects a set of properties from an IEnumerable of entities
        /// </summary>
        public IEnumerable<JObject> Apply<T>(IEnumerable<T> entities) => entities?.Select(Apply);
    }
}