using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Meta.Internal;
using RESTar.Results;

namespace RESTar.Meta
{
    /// <inheritdoc />
    /// <summary>
    /// A term denotes a node in a static or dynamic member tree. Contains a chain of properties, 
    /// used in queries to refer to properties and properties of properties.
    /// </summary>
    [JsonConverter(typeof(ToStringConverter))]
    public class Term : IEnumerable<Property>
    {
        private List<Property> Store;
        private readonly string ComponentSeparator;

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
        /// Is this term static? (Are all of its containing property references denoting declared members?)
        /// </summary>
        public bool IsDeclared { get; private set; }

        /// <summary>
        /// Is this term dynamic? (Are not all of its containing property references denoting declared members?)
        /// </summary>
        public bool IsDynamic => !IsDeclared;

        /// <summary>
        /// Automatically sets the Skip property of conditions matched against this term to true
        /// </summary>
        public bool ConditionSkip { get; private set; }

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

        /// <summary>
        /// Counts the properties of the Term
        /// </summary>
        public int Count => Store.Count;

        private static readonly NoCaseComparer Comparer = new NoCaseComparer();

        private Term(string componentSeparator)
        {
            Store = new List<Property>();
            ComponentSeparator = componentSeparator;
        }

        #region Public create methods, not used internally

        /// <summary>
        /// Create a new term for a given type, with a key describing the target property
        /// </summary>
        public static Term Create<T>(string key) where T : class => Create(typeof(T), key);

        /// <summary>
        /// Create a new term for a given type, with a key describing the target property
        /// </summary>
        public static Term Create(Type type, string key, string componentSeparator = ".") => type.MakeOrGetCachedTerm
        (
            key: key,
            componentSeparator: componentSeparator,
            bindingRule: TermBindingRule.DeclaredWithDynamicFallback
        );

        /// <summary>
        /// Create a new term from a given PropertyInfo
        /// </summary>
        public static Term Create(PropertyInfo propertyInfo) => propertyInfo.DeclaringType.MakeOrGetCachedTerm
        (
            key: propertyInfo.Name,
            componentSeparator: ".",
            bindingRule: TermBindingRule.DeclaredWithDynamicFallback
        );

        #endregion

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<Property> GetEnumerator() => Store.GetEnumerator();

        /// <summary>
        /// Parses a term key string and returns a term describing it. All terms are created here.
        /// The main caller is TypeCache.MakeTerm, but it's also called from places that use a 
        /// dynamic domain (processors).
        /// </summary>
        internal static Term Parse(Type resource, string key, string componentSeparator, TermBindingRule bindingRule, ICollection<string> dynDomain)
        {
            var term = new Term(componentSeparator);

            Property propertyMaker(string str)
            {
                if (string.IsNullOrWhiteSpace(str))
                    throw new InvalidSyntax(ErrorCodes.InvalidConditionSyntax, $"Invalid condition '{str}'");
                if (dynDomain?.Contains(str, Comparer) == true)
                    return DynamicProperty.Parse(str);

                Property make(Type type)
                {
                    switch (bindingRule)
                    {
                        case var _ when type.IsDDictionary():
                        case TermBindingRule.DeclaredWithDynamicFallback:
                            try
                            {
                                return DeclaredProperty.Find(type, str);
                            }
                            catch (UnknownProperty)
                            {
                                return DynamicProperty.Parse(str);
                            }
                        case TermBindingRule.DynamicWithDeclaredFallback: return DynamicProperty.Parse(str, true);
                        case TermBindingRule.OnlyDeclared:
                            try
                            {
                                return DeclaredProperty.Find(type, str);
                            }
                            catch (UnknownProperty)
                            {
                                if (type.GetSubclasses().Any(subClass => DeclaredProperty.TryFind(subClass, str, out _)))
                                    return DynamicProperty.Parse(str);
                                throw;
                            }
                        default: throw new Exception();
                    }
                }

                switch (term.Store.LastOrDefault())
                {
                    case null: return make(resource);
                    case DeclaredProperty stat: return make(stat.Type);
                    default: return DynamicProperty.Parse(str);
                }
            }

            key.Split(componentSeparator).ForEach(s => term.Store.Add(propertyMaker(s)));
            term.ScQueryable = term.Store.All(p => p.IsScQueryable);
            term.IsDeclared = term.Store.All(p => p is DeclaredProperty);
            term.ConditionSkip = term.Store.Any(p => p is DeclaredProperty s && s.SkipConditions);
            term.Key = string.Join(componentSeparator, term.Store.Select(p => p.Name));
            term.DbKey = string.Join(componentSeparator, term.Store.Select(p => p.ActualName));
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
                    case DeclaredProperty _: return DynamicProperty.Parse(prop.Name);
                    default: throw new ArgumentOutOfRangeException();
                }
            }).ToList();
            ScQueryable = false;
            IsDeclared = false;
            Key = string.Join(ComponentSeparator, Store.Select(p => p.Name));
        }

        /// <summary>
        /// Returns the value that this term denotes for a given target object
        /// </summary>
        public dynamic Evaluate(object target) => Evaluate(target, out _);

        /// <summary>
        /// Returns the value that this term denotes for a given target object as well as
        /// the actual key for this property (matching is case insensitive).
        /// </summary>
        public dynamic Evaluate(object target, out string actualKey) => Evaluate(target, out actualKey, out _, out _);

        /// <summary>
        /// Returns the value that this term denotes for a given target object as well as
        /// the actual key for this property (matching is case insensitive), the parent
        /// of the denoted value, and the property representing the denoted value.
        /// </summary>
        public dynamic Evaluate(object target, out string actualKey, out object parent, out Property property)
        {
            parent = null;
            property = null;

            // If the target is the result of processing using some IProcessor, the type
            // will be JObject. In that case, the object may contain the entire term key
            // as member, even if the term has multiple properties (common result of add 
            // and select). This code handles those cases.
            if (target is JObject jobj)
            {
                if (jobj.GetValue(Key, StringComparison.OrdinalIgnoreCase)?.Parent is JProperty jproperty)
                {
                    actualKey = jproperty.Name;
                    parent = jobj;
                    property = DynamicProperty.Parse(Key);
                    return jproperty.Value.ToObject<dynamic>();
                }
                MakeDynamic();
            }

            // Walk over the properties in the term, and if null is encountered, simply
            // keep the null. Else continue evaluating the next property as a property of the
            // previous property value.
            for (var i = 0; target != null && i < Store.Count; i++)
            {
                parent = target;
                property = Store[i];
                target = property.GetValue(target);
            }

            // If the term is dynamic, we do not know the actual key beforehand. We instead
            // set names for dynamic properties when getting their values, and concatenate the
            // property names here.
            if (IsDynamic)
                Key = string.Join(ComponentSeparator, Store.Select(p => p.Name));

            actualKey = Key;
            return target;
        }

        /// <summary>
        /// Gets a string representation of the given term
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Key;
    }
}