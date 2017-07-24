using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Linq;

namespace RESTar.Operations
{
    internal class Select : List<Term>, ICollection<Term>, IProcessor
    {
        internal Select(IResource resource, string key, IEnumerable<string> dynDomain) => key
            .ToLower()
            .Split(',')
            .Distinct()
            .If(dynDomain == null,
                then: s => s.Select(_s => resource.MakeTerm(_s, resource.IsDynamic)),
                @else: s => s.Select(_s => Term.ParseInternal(resource.Type, _s, resource.IsDynamic, dynDomain)))
            .ForEach(Add);

        public IEnumerable<JObject> Apply<T>(IEnumerable<T> entities)
        {
            return entities.Select(entity =>
            {
                var jobj = new JObject();
                ForEach(term =>
                {
                    if (jobj[term.Key] != null) return;
                    object val = term.Evaluate(entity, out string actualKey);
                    jobj[actualKey] = val?.ToJToken();
                });
                return jobj;
            });
        }
    }
}