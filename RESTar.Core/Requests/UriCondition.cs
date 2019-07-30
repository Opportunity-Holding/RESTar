using System;
using System.Collections.Generic;
using RESTar.Internal;
using static System.StringComparison;

namespace RESTar.Requests
{
    /// <inheritdoc cref="IUriCondition" />
    /// <summary>
    /// Describes the syntactic components of a RESTar uri condition
    /// </summary>
    public struct UriCondition : IUriCondition
    {
        /// <inheritdoc />
        public string Key { get; }

        /// <summary>
        /// The operator used for determining the comparison operation
        /// </summary>
        public Operators OperatorCode => Operator.OpCode;

        Operators IUriCondition.Operator => OperatorCode;

        internal Operator Operator { get; }

        /// <inheritdoc />
        public string ValueLiteral { get; }

        /// <inheritdoc />
        public TypeCode ValueTypeCode { get; }

        /// <summary>
        /// Creates a new custom UriCondition
        /// </summary>
        public UriCondition(string key, Operators op, string valueLiteral, TypeCode valueTypeCode)
        {
            Key = key;
            Operator = op;
            ValueLiteral = valueLiteral;
            ValueTypeCode = valueTypeCode;
        }

        /// <summary>
        /// Creates a new custom UriCondition encoding a meta-condition
        /// </summary>
        public UriCondition(RESTarMetaCondition metaCondition, string valueLiteral)
        {
            if (metaCondition < RESTarMetaCondition.Unsafe || metaCondition > RESTarMetaCondition.Safepost)
                throw new ArgumentOutOfRangeException(nameof(metaCondition));
            Key = metaCondition.ToString().ToLower();
            Operator = Operator.EQUALS;
            ValueLiteral = valueLiteral;
            ValueTypeCode = Type.GetTypeCode(metaCondition.GetExpectedType());
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is UriCondition u &&
                                                   string.Equals(u.Key, Key, OrdinalIgnoreCase) &&
                                                   u.Operator == Operator &&
                                                   u.ValueLiteral == ValueLiteral;

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + StringComparer.OrdinalIgnoreCase.GetHashCode(Key);
                hash = hash * 23 + Operator.OpCode.GetHashCode();
                hash = hash * 23 + ValueLiteral.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// EqualityComparer for UriCondition objects
        /// </summary>
        public static readonly IEqualityComparer<IUriCondition> EqualityComparer = new _EqualityComparer();

        private class _EqualityComparer : IEqualityComparer<IUriCondition>
        {
            public bool Equals(IUriCondition x, IUriCondition y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;
                return string.Equals(x.Key, y.Key, OrdinalIgnoreCase)
                       && x.Operator == y.Operator
                       && x.ValueLiteral == y.ValueLiteral
                       && x.ValueTypeCode == y.ValueTypeCode;
            }

            public int GetHashCode(IUriCondition obj)
            {
                unchecked
                {
                    var hash = 17;
                    hash = hash * 23 + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Key);
                    hash = hash * 23 + obj.Operator.GetHashCode();
                    hash = hash * 23 + obj.ValueLiteral.GetHashCode();
                    hash = hash * 23 + obj.ValueTypeCode.GetHashCode();
                    return hash;
                }
            }
        }
    }
}