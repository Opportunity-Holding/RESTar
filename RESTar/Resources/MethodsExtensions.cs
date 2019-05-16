using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;

namespace RESTar.Resources
{
    internal static class MethodsExtensions
    {
        internal static IReadOnlyList<Method> ResolveMethodRestrictions(this IEnumerable<Method> methods)
        {
            var restrictions = new HashSet<Method>(methods ?? RESTarConfig.Methods);
            if (restrictions.Contains(Method.GET))
            {
                restrictions.Add(Method.REPORT);
                restrictions.Add(Method.HEAD);
            }
            return restrictions.OrderBy(i => i, MethodComparer.Instance).ToList().AsReadOnly();
        }
    }
}