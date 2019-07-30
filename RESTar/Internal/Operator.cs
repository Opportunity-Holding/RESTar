using System;
using RESTar.Requests;

#pragma warning disable 1591

namespace RESTar.Internal
{
    /// <summary>
    /// Encodes an operator, used in conditions
    /// </summary>
    internal struct Operator
    {
        /// <summary>
        /// The code for this operator
        /// </summary>
        public readonly Operators OpCode;

        /// <summary>
        /// The common string representation of this operator
        /// </summary>
        internal string Common
        {
            get
            {
                switch (OpCode)
                {
                    case Operators.EQUALS: return "=";
                    case Operators.NOT_EQUALS: return "!=";
                    case Operators.LESS_THAN: return "<";
                    case Operators.GREATER_THAN: return ">";
                    case Operators.LESS_THAN_OR_EQUALS: return "<=";
                    case Operators.GREATER_THAN_OR_EQUALS: return ">=";
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// The SQL string representation of this operator
        /// </summary>
        public string SQL => OpCode == Operators.NOT_EQUALS ? "<>" : Common;

        internal bool Equality => OpCode == Operators.EQUALS || OpCode == Operators.NOT_EQUALS;
        internal bool Compare => !Equality;
        public override bool Equals(object obj) => obj is Operator op && op.OpCode == OpCode;
        public bool Equals(Operator other) => OpCode == other.OpCode;
        public override int GetHashCode() => (int) OpCode;
        public override string ToString() => Common;
        public static bool operator ==(Operator o1, Operator o2) => o1.OpCode == o2.OpCode;
        public static bool operator !=(Operator o1, Operator o2) => !(o1 == o2);

        public static Operator EQUALS => Operators.EQUALS;
        public static Operator NOT_EQUALS => Operators.NOT_EQUALS;
        public static Operator LESS_THAN => Operators.LESS_THAN;
        public static Operator GREATER_THAN => Operators.GREATER_THAN;
        public static Operator LESS_THAN_OR_EQUALS => Operators.LESS_THAN_OR_EQUALS;
        public static Operator GREATER_THAN_OR_EQUALS => Operators.GREATER_THAN_OR_EQUALS;

        private Operator(Operators op)
        {
            switch (op)
            {
                case Operators.EQUALS:
                case Operators.NOT_EQUALS:
                case Operators.LESS_THAN:
                case Operators.GREATER_THAN:
                case Operators.LESS_THAN_OR_EQUALS:
                case Operators.GREATER_THAN_OR_EQUALS:
                    OpCode = op;
                    break;
                default: throw new ArgumentException($"Invalid operator '{op}'");
            }
        }

        private static Operator Parse(string common)
        {
            switch (common)
            {
                case "=": return Operators.EQUALS;
                case "!=": return Operators.NOT_EQUALS;
                case "<": return Operators.LESS_THAN;
                case ">": return Operators.GREATER_THAN;
                case "<=": return Operators.LESS_THAN_OR_EQUALS;
                case ">=": return Operators.GREATER_THAN_OR_EQUALS;
                default: throw new ArgumentException(nameof(common));
            }
        }

        internal static bool TryParse(string common, out Operator op)
        {
            try
            {
                op = Parse(common);
                return true;
            }
            catch
            {
                op = default;
                return false;
            }
        }

        public static implicit operator Operator(Operators op) => new Operator(op);
    }
}