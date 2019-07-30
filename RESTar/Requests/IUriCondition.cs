using System;

namespace RESTar.Requests
{
    /// <summary>
    /// A common interface for URI conditions in RESTar
    /// </summary>
    public interface IUriCondition
    {
        /// <summary>
        /// The key of the condition
        /// </summary>
        string Key { get; }

        /// <summary>
        /// The operator of the condition
        /// </summary>
        Operators Operator { get; }

        /// <summary>
        /// A string describing the value encoded in the condition
        /// </summary>
        string ValueLiteral { get; }

        /// <summary>
        /// A TypeCode describing the type of the condition value (if known)
        /// </summary>
        TypeCode ValueTypeCode { get; }
    }
}