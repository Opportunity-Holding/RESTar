using System;
using RESTar.Requests;

namespace RESTar.Meta.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// Used to indicate a property that could refer to any index in an enumeration
    /// </summary>
    public class AnyIndexProperty : DeclaredProperty
    {
        /// <inheritdoc />
        public AnyIndexProperty
        (
            Type type,
            Type owner
        ) : base
        (
            metadataToken: "*".GetHashCode(),
            name: "<any index>",
            actualName: "<any index>",
            type: type,
            order: null,
            isScQueryable: false,
            attributes: new Attribute[0],
            skipConditions: false,
            hidden: true,
            hiddenIfNull: false,
            isEnum: type.IsEnum,
            customDateTimeFormat: null,
            allowedConditionOperators: Operators.All,
            owner: owner,
            getter: null,
            setter: null
        ) { }
    }
}