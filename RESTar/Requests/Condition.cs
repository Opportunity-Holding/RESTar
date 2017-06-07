﻿using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection;
using RESTar.Internal;
using RESTar.Operations;

namespace RESTar.Requests
{
    public class Condition
    {
        public string Key => PropertyChain.Key;
        public Operator Operator { get; set; }
        public dynamic Value { get; set; }
        public PropertyChain PropertyChain { get; set; }
        internal bool ScQueryable => PropertyChain.ScQueryable;
        internal Type Type => PropertyChain.IsStatic ? ((StaticProperty) PropertyChain.Last())?.Type : null;
        internal bool IsOfType<T>() => Type == typeof(T);

        internal void Migrate(Type newType)
        {
            PropertyChain = PropertyChain.MakeFromPrototype(PropertyChain, newType);
        }

        internal Condition(PropertyChain propertyChain, Operator op, dynamic value)
        {
            PropertyChain = propertyChain;
            Operator = op;
            Value = value;
        }

        internal bool HoldsFor(dynamic subject)
        {
            var subjectValue = PropertyChain.Get(subject);
            switch (Operator.OpCode)
            {
                case Operators.EQUALS: return Do.Try<bool>(() => subjectValue == Value, false);
                case Operators.NOT_EQUALS: return Do.Try<bool>(() => subjectValue != Value, true);
                case Operators.LESS_THAN:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string && Value is string)
                            return string.Compare((string) subjectValue, (string) Value, StringComparison.Ordinal) < 0;
                        return subjectValue < Value;
                    }, false);
                case Operators.GREATER_THAN:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string && Value is string)
                            return string.Compare((string) subjectValue, (string) Value, StringComparison.Ordinal) > 0;
                        return subjectValue > Value;
                    }, false);
                case Operators.LESS_THAN_OR_EQUALS:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string && Value is string)
                            return string.Compare((string) subjectValue, (string) Value, StringComparison.Ordinal) <= 0;
                        return subjectValue <= Value;
                    }, false);
                case Operators.GREATER_THAN_OR_EQUALS:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string && Value is string)
                            return string.Compare((string) subjectValue, (string) Value, StringComparison.Ordinal) >= 0;
                        return subjectValue >= Value;
                    }, false);
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}