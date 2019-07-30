using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RESTar.Requests.Filters
{
    /// <summary>
    /// Applies a distinct filtering to the inputted entities
    /// </summary>
    public class Distinct : IFilter
    {
        /// <summary>
        /// Applies the distinct filtering
        /// </summary>
        public IEnumerable<T> Apply<T>(IEnumerable<T> entities) where T : class
        {
            if (entities == null) return null;
            return DistinctIterator(entities);
        }

        private static IEnumerable<TSource> DistinctIterator<TSource>(IEnumerable<TSource> source)
        {
            var set = new HashSet<JObject>(JToken.EqualityComparer);
            foreach (var element in source)
                if (set.Add(element.ToJObject()))
                    yield return element;
        }
    }
}