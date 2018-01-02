using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RESTar.Operations;
using static System.StringComparison;

namespace RESTar.Deflection.Dynamic
{
    /// <inheritdoc />
    /// <summary>
    /// A dynamic property represents a dynamic property of a class, that is,
    /// a member that is not known at compile time.
    /// </summary>
    public class DynamicProperty : Property
    {
        /// <inheritdoc />
        public override bool Dynamic => true;

        /// <summary>
        /// Should the evaluation fall back to a static property 
        /// if no dynamic property can be found in the target entity?
        /// </summary>
        public readonly bool StaticFallback;

        /// <summary>
        /// Creates a dynamic property instance from a key string
        /// </summary>
        /// <param name="keyString">The string to parse</param>
        /// <param name="staticFallback">Should the evaluation fall back to a static property 
        /// if no dynamic property can be found in the target entity?</param>
        /// <returns>A dynamic property that represents the runtime property
        /// described by the key string</returns>
        public static DynamicProperty Parse(string keyString, bool staticFallback = false) =>
            new DynamicProperty(keyString, staticFallback);

        internal void SetName(string name) => Name = name;

        private DynamicProperty(string name, bool staticFallback)
        {
            Name = ActualName = name;
            ScQueryable = false;
            StaticFallback = staticFallback;

            Getter = obj =>
            {
                object value;
                string actualKey = null;
                string capitalized;

                dynamic getFromStatic()
                {
                    var type = obj.GetType();
                    value = Do.Try(() =>
                    {
                        var prop = StaticProperty.Find(type, Name);
                        actualKey = prop.Name;
                        return prop.GetValue(obj);
                    }, default(object));
                    Name = actualKey ?? Name;
                    return value;
                }

                switch (obj)
                {
                    case IDictionary<string, object> dict:
                        capitalized = Name.Capitalize();
                        if (dict.TryGetValue(capitalized, out value))
                        {
                            Name = capitalized;
                            return value;
                        }
                        if (dict.TryFindInDictionary(Name, out actualKey, out value))
                        {
                            Name = actualKey;
                            return value;
                        }
                        return StaticFallback ? getFromStatic() : null;
                    case JObject jobj:
                        capitalized = Name.Capitalize();
                        if (!(jobj.GetValue(capitalized, OrdinalIgnoreCase)?.Parent is JProperty property))
                            return StaticFallback ? getFromStatic() : null;
                        Name = property.Name;
                        return property.Value.ToObject<dynamic>();
                    default: return getFromStatic();
                }
            };

            Setter = (obj, value) =>
            {
                switch (obj)
                {
                    case IDictionary<string, dynamic> ddict:
                        ddict[Name] = value;
                        break;
                    case IDictionary idict:
                        idict[Name] = value;
                        break;
                    case IDictionary<string, JToken> jobj:
                        jobj[Name] = value;
                        break;
                    default:
                        var type = obj.GetType();
                        Do.Try(() => StaticProperty.Find(type, Name)?.SetValue(obj, value));
                        break;
                }
            };
        }
    }
}