using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;

namespace RESTar.Deflection.Dynamic
{
    /// <summary>
    /// A term denotes a node in a static or dynamic member tree. Contains a chain of properties, 
    /// used in queries to refer to properties and properties of properties.
    /// </summary>
    public class Term
    {
        private List<Property> Store;

        /// <summary>
        /// A string representation of the path to the property, using dot notation
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// The property path for use in SQL queries
        /// </summary>
        public string DbKey { get; private set; }

        /// <summary>
        /// Can this term be used to reference a property in an SQL statement?
        /// </summary>
        public bool ScQueryable { get; private set; }

        /// <summary>
        /// Is this term static? (Are all of its containing property references denoting static members?)
        /// </summary>
        public bool IsStatic { get; private set; }

        /// <summary>
        /// Is this term dynamic? (Are not all of its containing property references denoting static members?)
        /// </summary>
        public bool IsDynamic => !IsStatic;

        /// <summary>
        /// Gets the first property reference of the term, and safe casts it to T
        /// </summary>
        public T FirstAs<T>() where T : Property => First as T;

        /// <summary>
        /// Gets the first property reference of the term, or null of the term is empty
        /// </summary>
        public Property First => Store.Any() ? Store[0] : null;

        /// <summary>
        /// Gets the last property reference of the term, and safe casts it to T
        /// </summary>
        public T LastAs<T>() where T : Property => Store.LastOrDefault() as T;

        /// <summary>
        /// Gets the last property reference of the term, or null of the term is empty
        /// </summary>
        public Property Last => Store.LastOrDefault();

        private static readonly NoCaseComparer Comparer = new NoCaseComparer();
        private Term() => Store = new List<Property>();

        /// <summary>
        /// Create a new term for a given type, with a key describing the target property
        /// </summary>
        public static Term Create<T>(string key) where T : class =>
            typeof(T).MakeTerm(key, Resource<T>.SafeGet?.DynamicConditionsAllowed == true);

        /// <summary>
        /// Create a new term for a given type, with a key describing the target property
        /// </summary>
        public static Term Create(Type type, string key) => type.MakeTerm(key,
            Resource.SafeGet(type)?.DynamicConditionsAllowed == true);

        /// <summary>
        /// Create a new term from a given PropertyInfo
        /// </summary>
        public static Term Create(PropertyInfo propertyInfo) => propertyInfo.ToTerm();

        /// <summary>
        /// Parses a term key string and returns a term describing it.
        /// </summary>
        internal static Term Parse(Type resource, string key, bool dynamicUnknowns,
            IEnumerable<string> dynamicDomain = null)
        {
            var term = new Term();

            Property propertyMaker(string str)
            {
                if (string.IsNullOrWhiteSpace(str))
                    throw new SyntaxException(ErrorCodes.InvalidConditionSyntax, $"Invalid condition '{str}'");
                if (dynamicDomain?.Contains(str, Comparer) == true)
                    return DynamicProperty.Parse(str);

                Property make(Type type)
                {
                    if (type.IsDDictionary())
                        return DynamicProperty.Parse(str);
                    if (dynamicUnknowns)
                        return Do.Try<Property>(
                            () => StaticProperty.Find(type, str),
                            () => DynamicProperty.Parse(str));
                    return StaticProperty.Find(type, str);
                }

                switch (term.Store.LastOrDefault())
                {
                    case null: return make(resource);
                    case StaticProperty stat: return make(stat.Type);
                    default: return DynamicProperty.Parse(str);
                }
            }

            key.Split('.').ForEach(s => term.Store.Add(propertyMaker(s)));
            term.ScQueryable = term.Store.All(p => p.ScQueryable);
            term.IsStatic = term.Store.All(p => p is StaticProperty);
            term.Key = string.Join(".", term.Store.Select(p => p.Name));
            term.DbKey = string.Join(".", term.Store.Select(p => p.DatabaseQueryName));
            return term;
        }

        /// <summary>
        /// Converts all properties in this term to dynamic properties
        /// </summary>
        private void MakeDynamic()
        {
            if (IsDynamic) return;
            Store = Store.Select(prop =>
            {
                switch (prop)
                {
                    case SpecialProperty _:
                    case DynamicProperty _: return prop;
                    case StaticProperty _: return new DynamicProperty(prop.Name);
                    default: throw new ArgumentOutOfRangeException();
                }
            }).ToList();
            ScQueryable = false;
            IsStatic = false;
            Key = string.Join(".", Store.Select(p => p.Name));
        }

        /// <summary>
        /// Returns the value that this term denotes for a given target object
        /// </summary>
        public dynamic Evaluate(object target) => Evaluate(target, out var _);

        /// <summary>
        /// Returns the value that this term denotes for a given target object as well as
        /// the actual key for this property (matching is case insensitive).
        /// </summary>
        public dynamic Evaluate(object target, out string actualKey)
        {
            // If the target is the result of processing using some IProcessor, the type
            // will be JObject. In that case, the object may contain the entire term key
            // as member, even if the term has multiple properties (common result of add 
            // and select). This code handles those cases.
            if (target is JObject jobj)
            {
                if (jobj.TryGetNoCase(Key, out var actual, out var jvalue))
                {
                    actualKey = actual;
                    return jvalue.ToObject<dynamic>();
                }
                MakeDynamic();
            }

            // Walk over the properties in the term, and if null is encountered, simply
            // keep the null. Else keep evaluating the next property as a property of the
            // previous property value.
            Store.ForEach(prop =>
            {
                if (target != null)
                    target = prop.GetValue(target);
            });

            // If the term is dynamic, we do not know the actual key beforehand. We instead
            // set names for dynamic properties when getting their values, and concatenate the
            // property names here.
            if (IsDynamic)
                Key = string.Join(".", Store.Select(p => p.Name));

            actualKey = Key;
            return target;
        }
    }
}