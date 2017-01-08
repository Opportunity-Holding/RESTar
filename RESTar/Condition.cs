﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Starcounter;
using static RESTar.RESTarMetaConditions;

namespace RESTar
{
    public sealed class Condition
    {
        public string Key;
        public Operator Operator;
        public object Value;

        internal static IList<Condition> ParseConditions(Type resource, string conditionString)
        {
            if (string.IsNullOrEmpty(conditionString))
                return null;

            return conditionString.Split('&').Select(s =>
            {
                if (s == "")
                    throw new SyntaxException("Invalid condition syntax");
                var matched = new string(s.Where(c => OpMatchChars.Contains(c)).ToArray());
                var op = Operators.FirstOrDefault(o => o.Common == matched);
                if (op == null)
                {
                    throw new SyntaxException("Invalid or missing operator for condition. The presence of one " +
                                              "(and only one) operator is required per condition. Accepted operators: " +
                                              string.Join(", ", Operators.Select(o => o.Common)));
                }
                var pair = s.Split(new[] {op.Common}, StringSplitOptions.None);
                Type type;
                var key = GetKey(resource, pair[0], out type);
                return new Condition
                {
                    Key = key,
                    Operator = op,
                    Value = GetValue(pair[1], key, type)
                };
            }).ToList();
        }

        internal static IDictionary<string, object> ParseMetaConditions(string metConditionString)
        {
            if (metConditionString?.Equals("") != false)
                return null;

            return metConditionString.Split('&').Select(s =>
            {
                if (s == "")
                    throw new SyntaxException("Invalid meta-condition syntax");
                var op = Operators.FirstOrDefault(o => s.Contains(o.Common));
                if (op?.Common != "=")
                    throw new SyntaxException("Invalid operator for meta-condition. Only '=' is accepted");
                var pair = s.Split(new[] {op.Common}, StringSplitOptions.None);

                RESTarMetaConditions metaCondition;
                var success = Enum.TryParse(pair[0].Capitalize(), out metaCondition);
                if (!success)
                    throw new SyntaxException($"Invalid meta-condition '{pair[0]}'. Available meta-conditions: " +
                                              $"{string.Join(", ", RESTarConfig.MetaConditions.Keys)}. For more info, see " +
                                              $"{Settings.Instance.HelpResourcePath}/topic=Meta-conditions");
                var typeCheck = RESTarConfig.MetaConditions[metaCondition];
                var value = GetValue(pair[1]);
                if (value.GetType() != typeCheck)
                    throw new SyntaxException($"Invalid data type assigned to meta-condition '{pair[0]}'. Expected " +
                                              $"{(typeCheck == typeof(decimal) ? "number" : typeCheck.FullName)}.");
                return new KeyValuePair<string, object>(pair[0].ToLower(), value);
            }).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private static readonly char[] OpMatchChars = {'<', '>', '=', '!'};

        private static readonly IEnumerable<Operator> Operators = new List<Operator>
        {
            new Operator("=", "="),
            new Operator("!=", "<>"),
            new Operator("<", "<"),
            new Operator(">", ">"),
            new Operator(">=", ">="),
            new Operator("<=", "<=")
        };

        private static string GetKey(Type resource, string keyString, out Type keyType)
        {
            keyType = default(Type);
            keyString = keyString.ToLower();
            var columns = resource.GetColumns();
            if (!keyString.Contains('.'))
            {
                if (keyString == "objectno")
                    return "ObjectNo";
                if (keyString == "objectid")
                    return "ObjectId";
                var column = columns.FindColumn(resource, keyString);
                keyType = column.PropertyType;
                return column.Name;
            }
            var parts = keyString.Split('.');
            if (parts.Length == 1)
                throw new SyntaxException($"Invalid condition '{keyString}'");
            var types = new List<Type>();
            foreach (var str in parts.Take(parts.Length - 1))
            {
                var containingType = types.LastOrDefault() ?? resource;
                var type = containingType
                    .GetProperties()
                    .Where(prop => str == prop.Name.ToLower())
                    .Select(prop => prop.PropertyType)
                    .FirstOrDefault();

                if (type == null)
                    throw new UnknownColumnException(resource, keyString);

                if (type.GetAttribute<RESTarAttribute>()?.AvailableMethods.Contains(RESTarMethods.GET) != true)
                    throw new SyntaxException($"RESTar does not have read access to resource '{type.FullName}' " +
                                              $"referenced in '{keyString}'.");

                if (!type.HasAttribute<DatabaseAttribute>())
                    throw new SyntaxException($"Part '{str}' in condition key '{keyString}' referenced a column of " +
                                              $"type '{type.FullName}', which is of a non-resource type. Non-resource " +
                                              "columns can only appear last in condition keys containing dot notation.");
                types.Add(type);
            }

            if (parts.Last() == "objectno" || parts.Last() == "objectid")
                return string.Join(".", parts);
            var lastType = types.Last();
            var lastColumns = lastType.GetColumns();
            var lastColumn = lastColumns.FindColumn(lastType, parts.Last());
            parts[parts.Length - 1] = lastColumn.Name;
            keyType = lastColumn.PropertyType;
            return string.Join(".", parts);
        }

        private static object GetValue(string valueString, string key = null, Type expectedType = null)
        {
            valueString = HttpUtility.UrlDecode(valueString);
            if (valueString == null)
                return null;
            if (valueString == "null")
                return null;
            if (valueString.First() == '\"')
                return valueString.Replace("\"", "");
            object obj;
            decimal dec;
            bool boo;
            DateTime dat;
            if (bool.TryParse(valueString, out boo))
                obj = boo;
            else if (decimal.TryParse(valueString, out dec))
            {
                var rounded = decimal.Round(dec, 6);
                obj = rounded;
            }
            else if (DateTime.TryParse(valueString, out dat))
                obj = dat;
            else obj = valueString;

            if (expectedType != null)
                if (obj.GetType() != expectedType)
                    throw new SyntaxException($"Invalid type for condition '{key}'. Expected " +
                                              $"{expectedType}, found {obj.GetType()}");
            return obj;
        }

        public override string ToString()
        {
            return Key + Operator + Value;
        }
    }
}