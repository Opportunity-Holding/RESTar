using System.Reflection;
using RESTar.Meta;
using RESTar.Resources;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a property by some search string
    /// </summary>
    internal class UnknownProperty : NotFound
    {
        internal UnknownProperty(MemberInfo type, string searchString) : base(ErrorCodes.UnknownProperty,
            $"Could not find any property in {(type.HasAttribute<RESTarViewAttribute>() ? $"view '{type.Name}' or type '{Resource.Get(type.DeclaringType)?.Name}'" : $"type '{type.Name}'")} by '{searchString}'.") { }
    }
}