using System.Collections.Generic;
using System.Linq;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// A RESTar attribute type used for procedurally created resources
    /// </summary>
    internal class RESTarProceduralAttribute : RESTarAttribute
    {
        /// <inheritdoc />
        internal RESTarProceduralAttribute(IEnumerable<Method> methods) : base(methods.ToArray()) { }
    }
}