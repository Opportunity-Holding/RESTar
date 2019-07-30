using RESTar.Meta;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an invalid value type was used in a condition or there was a mismatch 
    /// with the type of the referenced property.
    /// </summary>
    internal class InvalidConditionValueType : BadRequest
    {
        internal InvalidConditionValueType(string valueLiteral, Member property)
            : base(ErrorCodes.InvalidConditionValueType,
                $"Invalid type for condition value '{valueLiteral}'. Expected {property.Type.GetFriendlyTypeName()}") { }
    }
}