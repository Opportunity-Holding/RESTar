using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;

namespace RESTar.Internal
{
    public class PropertyChain : List<Property>
    {
        public string Key => string.Join(".", this.Select(p => p.Name));
        public string DbKey => string.Join(".", this.Select(p => p.DatabaseQueryName));
        public bool ScQueryable => this.All(p => p.ScQueryable);
        private static readonly NoCaseComparer Comparer = new NoCaseComparer();

        internal static PropertyChain Parse(string keyString, IResource resource, List<string> dynamicDomain = null)
        {
            var chain = new PropertyChain();
            Func<string, Property> propertyMaker = str =>
            {
                if (string.IsNullOrWhiteSpace(str))
                    throw new SyntaxException($"Invalid condition '{str}'",
                        ErrorCode.InvalidConditionSyntaxError);
                if (str.ToLower() == "objectno") return StaticProperty.ObjectNo;
                if (str.ToLower() == "objectid") return StaticProperty.ObjectID;
                if (dynamicDomain?.Contains(str, Comparer) == true)
                    return DynamicProperty.Parse(str);
                var previous = chain.LastOrDefault();
                if (previous == null)
                    return Property.Parse(str, resource.TargetType, resource.IsDynamic);
                if (previous.Static)
                {
                    var _previous = (StaticProperty) previous;
                    if (_previous.Type.IsSubclassOf(typeof(DDictionary)))
                        return DynamicProperty.Parse(str);
                    return StaticProperty.Parse(str, ((StaticProperty) previous).Type);
                }
                return DynamicProperty.Parse(str);
            };
            keyString.Split('.').ForEach(s => chain.Add(propertyMaker(s)));
            return chain;
        }

        internal void MakeDynamic()
        {
            var newProperties = this.Select(prop =>
                {
                    var stat = prop as StaticProperty;
                    if (stat != null && !stat.IsObjectID && !stat.IsObjectNo)
                        new DynamicProperty(prop.Name);
                    return prop;
                })
                .ToList();
            Clear();
            AddRange(newProperties);
        }

        internal void Migrate(Type type)
        {
            StaticProperty previousStatic = null;
            foreach (var property in this)
            {
                if (property.Dynamic) return;
                var stat = (StaticProperty) property;
                stat.Migrate(type, previousStatic);
                previousStatic = stat;
            }
        }

        internal dynamic GetValue(dynamic val)
        {
            if (val is IDictionary<string, dynamic>) MakeDynamic();
            foreach (var prop in this)
            {
                if (val == null) return null;
                val = prop.GetValue(val);
            }
            return val;
        }
    }
}