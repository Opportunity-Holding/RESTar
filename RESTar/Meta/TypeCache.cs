using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Newtonsoft.Json.Serialization;
using RESTar.Linq;
using RESTar.Meta.IL;
using RESTar.Meta.Internal;
using RESTar.Resources;
using Starcounter;

namespace RESTar.Meta
{
    /// <summary>
    /// The type cache keeps track of discovered types and provides
    /// fast access to their declared properties.
    /// </summary>limit
    public static class TypeCache
    {
        static TypeCache()
        {
            DeclaredPropertyCache = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, DeclaredProperty>>();
            DeclaredPropertyCacheByActualName = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, DeclaredProperty>>();
            TermCache = new ConcurrentDictionary<(string, string, TermBindingRule), Term>();
            PropertyMonitoringTreeCache = new ConcurrentDictionary<Type, PropertyMonitoringTree>();
        }

        #region Terms

        internal static readonly ConcurrentDictionary<(string Type, string Key, TermBindingRule BindingRule), Term> TermCache;

        /// <summary>
        /// Condition terms are terms that refer to properties in resources, or for
        /// use in conditions.
        /// </summary>
        internal static Term MakeConditionTerm(this ITarget target, string key) => target.Type.MakeOrGetCachedTerm
        (
            key: key,
            componentSeparator: ".",
            bindingRule: target.ConditionBindingRule
        );

        /// <summary>
        /// Output terms are terms that refer to properties in RESTar output. If they refer to
        /// a property in the dynamic domain, they are not cached. 
        /// </summary>
        internal static Term MakeOutputTerm(this IEntityResource target, string key, ICollection<string> dynamicDomain) =>
            dynamicDomain == null
                ? MakeOrGetCachedTerm(target.Type, key, ".", target.OutputBindingRule)
                : Term.Parse(target.Type, key, ".", target.OutputBindingRule, dynamicDomain);

        /// <summary>
        /// Creates a new term for the given type, with the given key, component separator and binding rule. If a term with
        /// the given key already existed, simply returns that one.
        /// </summary>
        public static Term MakeOrGetCachedTerm(this Type resource, string key, string componentSeparator, TermBindingRule bindingRule)
        {
            var tuple = (resource.RESTarTypeName(), key.ToLower(), bindingRule);
            if (!TermCache.TryGetValue(tuple, out var term))
                term = TermCache[tuple] = Term.Parse(resource, key, componentSeparator, bindingRule, null);
            return term;
        }

        internal static void ClearTermsFor<T>() => TermCache
            .Where(pair => pair.Key.Type == typeof(T).RESTarTypeName())
            .Select(pair => pair.Key)
            .ToList()
            .ForEach(key => TermCache.TryRemove(key, out _));

        #endregion

        #region Declared properties

        internal static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, DeclaredProperty>> DeclaredPropertyCache;
        internal static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, DeclaredProperty>> DeclaredPropertyCacheByActualName;

        internal static IEnumerable<DeclaredProperty> FindAndParseDeclaredProperties(this Type type, bool flag = false) => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ParseDeclaredProperties(flag);

        internal static IEnumerable<DeclaredProperty> ParseDeclaredProperties(this IEnumerable<PropertyInfo> props, bool flag) => props
            .Where(p => !p.RESTarIgnored())
            .Where(p => !p.GetIndexParameters().Any())
            .Select(p => new DeclaredProperty(p, flag))
            .OrderBy(p => p.Order);

        /// <summary>
        /// Gets the declared properties for a given type
        /// </summary>
        public static IReadOnlyDictionary<string, DeclaredProperty> GetDeclaredProperties(this Type type, bool groupByActualName = false)
        {
            IEnumerable<DeclaredProperty> make(Type _type)
            {
                switch (_type)
                {
                    case null: return new DeclaredProperty[0];
                    case var _ when _type.IsInterface:
                        return new[] {_type}
                            .Concat(_type.GetInterfaces())
                            .SelectMany(i => i.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                            .ParseDeclaredProperties(false);
                    case var _ when _type.GetRESTarInterfaceType() is Type t:
                        var interfaceName = t.RESTarTypeName();
                        var targetsByProp = _type
                            .GetInterfaceMap(t)
                            .TargetMethods
                            .GroupBy(m =>
                            {
                                if (m.IsPrivate && m.Name.StartsWith($"{interfaceName}.get_"))
                                    return m.Name.Split(interfaceName + ".get_")[1];
                                if (m.IsPrivate && m.Name.StartsWith($"{interfaceName}.set_"))
                                    return m.Name.Split(interfaceName + ".set_")[1];
                                if (m.Name.StartsWith("get_"))
                                    return m.Name.Split("get_")[1];
                                if (m.Name.StartsWith("set_"))
                                    return m.Name.Split("set_")[1];
                                return null;
                            })
                            .Where(group => group.Key != null)
                            .ToDictionary(m => m.Key, m => (
                                getter: m.FirstOrDefault(p => p.GetParameters().Length == 0),
                                setter: m.FirstOrDefault(p => p.GetParameters().Length == 1)
                            ));
                        return make(t).Select(p =>
                        {
                            p.IsScQueryable = _type.HasAttribute<DatabaseAttribute>() && p.Type.IsStarcounterCompatible();
                            var (getter, setter) = targetsByProp.SafeGet(p.ActualName);
                            if (p.IsReadable)
                            {
                                p.ActualName = getter.GetInstructions()
                                    .Select(i => i.OpCode == OpCodes.Call && i.Operand is MethodInfo calledMethod && getter.IsSpecialName
                                        ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .FirstOrDefault(prop => prop.GetGetMethod() == calledMethod)
                                        : null)
                                    .LastOrDefault(prop => prop != null)?
                                    .Name;
                            }
                            else if (p.IsWritable)
                            {
                                p.ActualName = setter.GetInstructions()
                                    .Select(i => i.OpCode == OpCodes.Call && i.Operand is MethodInfo calledMethod && setter.IsSpecialName
                                        ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .FirstOrDefault(prop => prop.GetSetMethod() == calledMethod)
                                        : null)
                                    .LastOrDefault(prop => prop != null)?
                                    .Name;
                            }
                            return p;
                        }).If(_type.HasAttribute<DatabaseAttribute>(), ps => ps.Union(SpecialProperty.GetObjectNoAndObjectID(flag: false, _type)));
                    case var _ when typeof(ITerminal).IsAssignableFrom(_type):
                        return _type.FindAndParseDeclaredProperties().Except(make(typeof(ITerminal)), DeclaredProperty.NameComparer);
                    case var _ when _type.IsNullable(out var underlying):
                        return underlying.GetDeclaredProperties().Values;
                    case var _ when _type.HasAttribute<RESTarViewAttribute>():
                        return _type.FindAndParseDeclaredProperties().Union(make(_type.DeclaringType));
                    case var _ when _type.IsDDictionary():
                        return _type.FindAndParseDeclaredProperties(true).Union(SpecialProperty.GetObjectNoAndObjectID(flag: true, _type));
                    case var _ when Resource.SafeGet(_type) is IEntityResource e && e.DeclaredPropertiesFlagged:
                        return _type.FindAndParseDeclaredProperties(true);
                    default:
                        return _type
                            .FindAndParseDeclaredProperties()
                            .If(_type.HasAttribute<DatabaseAttribute>(), ps => ps.Union(SpecialProperty.GetObjectNoAndObjectID(false, _type)));
                }
            }

            if (type?.RESTarTypeName() == null) return null;

            if (!groupByActualName)
            {
                if (!DeclaredPropertyCache.TryGetValue(type, out var propsByName))
                {
                    propsByName = DeclaredPropertyCache[type] = make(type).SafeToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
                    propsByName.Values.ForEach(property => property.EstablishPropertyDependancies());
                }
                return propsByName;
            }

            if (!DeclaredPropertyCacheByActualName.TryGetValue(type, out var propsByActualName))
            {
                propsByActualName = DeclaredPropertyCacheByActualName[type] = GetDeclaredProperties(type)
                    .Values
                    .SafeToDictionary(p => p.ActualName, StringComparer.OrdinalIgnoreCase);
            }
            return propsByActualName;
        }

        /// <summary>
        /// Gets the DeclaredProperty for a given PropertyInfo
        /// </summary>
        public static DeclaredProperty GetDeclaredProperty(this PropertyInfo member)
        {
            var declaringType = member.DeclaringType;
            if (declaringType.RESTarTypeName() == null)
                throw new Exception($"Cannot get declared property for member '{member}' of unknown type");
            declaringType.GetDeclaredProperties(true).TryGetValue(member.Name, out var property);
            return property;
        }

        /// <summary>
        /// Gets the DeclaredProperty for a given JsonProperty
        /// </summary>
        public static DeclaredProperty GetDeclaredProperty(this JsonProperty member)
        {
            var declaringType = member.DeclaringType;
            if (declaringType.RESTarTypeName() == null)
                throw new Exception($"Cannot get declared property for member '{member}' of unknown type");
            declaringType.GetDeclaredProperties().TryGetValue(member.PropertyName, out var property);
            return property;
        }

        #endregion

        #region Property monitoring trees

        internal static readonly IDictionary<Type, PropertyMonitoringTree> PropertyMonitoringTreeCache;

        /// <summary>
        /// Gets a property monitoring tree for a given type
        /// </summary>
        public static PropertyMonitoringTree GetPropertyMonitoringTree(this Type rootType, Term stub, string outputTermComponentSeparator,
            ObservedChangeHandler handleObservedChange)
        {
            return new PropertyMonitoringTree(rootType, outputTermComponentSeparator, stub, handleObservedChange);
        }

        #endregion
    }
}