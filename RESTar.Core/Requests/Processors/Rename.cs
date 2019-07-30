using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders;
using RESTar.Linq;
using RESTar.Meta;

namespace RESTar.Requests.Processors
{
    /// <inheritdoc cref="Dictionary{TKey,TValue}" />
    /// /// <inheritdoc cref="IProcessor" />
    /// <summary>
    /// Renames properties in an IEnumerable
    /// </summary>
    public class Rename : Dictionary<Term, string>, IProcessor
    {
        internal Rename(IEntityResource resource, string keys, out ICollection<string> dynamicDomain)
        {
            keys.Split(',').ForEach(keyString =>
            {
                var (termKey, newName) = keyString.TSplit(keys.Contains("->") ? "->" : "-%3E");
                Add(resource.MakeOutputTerm(termKey.ToLower(), null), newName);
            });
            dynamicDomain = Values;
        }

        private Rename(Rename other) : base(other) { }
        internal Rename GetCopy() => new Rename(this);

        private JObject Renamed(JObject entity)
        {
            this.ForEach(pair =>
            {
                var value = entity.GetValue(pair.Key.Key, StringComparison.OrdinalIgnoreCase);
                if (value == null)
                {
                    value = pair.Key.Evaluate(entity, out _);
                    entity[pair.Value] = value == null ? null : JToken.FromObject(value, JsonProvider.Serializer);
                    return;
                }
                var property = (JProperty) value.Parent;
                var actualKey = property.Name;
                if (actualKey != null)
                    entity.Remove(actualKey);
                entity[pair.Value] = value;
            });
            return entity;
        }

        /// <summary>
        /// Renames properties in an IEnumerable
        /// </summary>
        public IEnumerable<JObject> Apply<T>(IEnumerable<T> entities) => entities?.Select(entity => Renamed(entity.ToJObject()));
    }
}