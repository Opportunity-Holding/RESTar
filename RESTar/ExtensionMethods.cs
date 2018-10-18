using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Dynamit;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Admin;
using RESTar.ContentTypeProviders;
using RESTar.Internal;
using RESTar.Internal.Sc;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Meta.Internal;
using RESTar.Requests;
using RESTar.Requests.Filters;
using RESTar.Requests.Processors;
using RESTar.Resources;
using RESTar.Results;
using RESTar.WebSockets;
using Starcounter;
using static System.Globalization.DateTimeStyles;
using static System.Reflection.BindingFlags;
using static System.StringComparison;
using static RESTar.ErrorCodes;
using static RESTar.Internal.ContentTypeController;
using static RESTar.Requests.Operators;
using Operator = RESTar.Internal.Operator;
using UriComponents = RESTar.Requests.UriComponents;

namespace RESTar
{
    /// <summary>
    /// Extension methods used by RESTar
    /// </summary>
    public static class ExtensionMethods
    {
        #region Member reflection

        internal static string RESTarMemberName(this MemberInfo m, bool flagged = false)
        {
            var name = m.GetCustomAttributes().Select(a =>
            {
                switch (a)
                {
                    case RESTarMemberAttribute rma: return rma.Name;
                    case DataMemberAttribute dma: return dma.Name;
                    case JsonPropertyAttribute jpa: return jpa.PropertyName;
                    default: return null;
                }
            }).FirstOrDefault(a => a != null) ?? m.Name;
            return flagged ? $"${name}" : name;
        }

        internal static bool RESTarIgnored(this MemberInfo m) => m.GetCustomAttribute<RESTarMemberAttribute>()?.Ignored == true ||
                                                                 m.HasAttribute<IgnoreDataMemberAttribute>();

        #endregion

        #region Type reflection

        internal static string RESTarTypeName(this Type type) => type?.FullName?.Replace('+', '.');

        /// <summary>
        /// Can this type hold dynamic members? Defined as implementing the IDictionary`2 interface
        /// </summary>
        public static bool IsDynamic(this Type type) => type.ImplementsGenericInterface(typeof(IDictionary<,>))
                                                        || typeof(IDynamicMemberValueProvider).IsAssignableFrom(type);

        internal static bool IsDDictionary(this Type type) => type == typeof(DDictionary) ||
                                                              type.IsSubclassOf(typeof(DDictionary));

        internal static IList<Type> GetConcreteSubclasses(this Type baseType) => baseType.GetSubclasses()
            .Where(type => !type.IsAbstract)
            .ToList();

        internal static IEnumerable<Type> GetSubclasses(this Type baseType) =>
            from assembly in AppDomain.CurrentDomain.GetAssemblies()
            from type in assembly.GetTypes()
            where type.IsSubclassOf(baseType)
            select type;

        internal static T AsImplemented<T>(this T @delegate) where T : Delegate
        {
            return @delegate?.Method.HasAttribute<MethodNotImplementedAttribute>() == false ? @delegate : null;
        }

        internal static bool HasAttribute(this MemberInfo type, Type attributeType)
        {
            return (type?.GetCustomAttributes(attributeType).Any()).GetValueOrDefault();
        }

        internal static bool HasResourceProviderAttribute(this Type resource)
        {
            return resource.GetCustomAttributes().OfType<EntityResourceProviderAttribute>().Any();
        }

        internal static bool HasAttribute<TAttribute>(this MemberInfo type) where TAttribute : Attribute
        {
            return type?.GetCustomAttribute<TAttribute>() != null;
        }

        internal static bool HasAttribute<TAttribute>(this MemberInfo type, out TAttribute attribute) where TAttribute : Attribute
        {
            attribute = type?.GetCustomAttribute<TAttribute>();
            return attribute != null;
        }

        internal static IEnumerable<Type> GetBaseTypes(this Type type)
        {
            var currentBaseType = type.BaseType;
            while (currentBaseType != null)
            {
                yield return currentBaseType;
                currentBaseType = currentBaseType.BaseType;
            }
        }

        internal static Type[] GetGenericTypeParameters(this Type type, Type typeDef = null)
        {
            var toMatch = typeDef == null ? type : type?.GetBaseTypes().FirstOrDefault(t => t.GetGenericTypeDefinition() == typeDef);
            return toMatch?.IsGenericType != true ? null : toMatch.GenericTypeArguments;
        }

        /// <summary>
        /// Returns true if and only if the type implements the generic interface type
        /// </summary>
        public static bool ImplementsGenericInterface(this Type type, Type interfaceType)
        {
            if (type.Name == interfaceType.Name &&
                type.Namespace == interfaceType.Namespace &&
                type.Assembly == interfaceType.Assembly)
                return true;
            return type
                .GetInterfaces()
                .Any(i => i.Name == interfaceType.Name &&
                          i.Namespace == interfaceType.Namespace &&
                          i.Assembly == interfaceType.Assembly);
        }

        /// <summary>
        /// Returns true if and only if the type implements the generic interface type. Returns the 
        /// generic type parameters (if any) in an out parameter.
        /// </summary>
        public static bool ImplementsGenericInterface(this Type type, Type interfaceType, out Type[] genericParameters)
        {
            var match = type.GetInterfaces()
                .FirstOrDefault(i => i.Name == interfaceType.Name &&
                                     i.Namespace == interfaceType.Namespace &&
                                     i.Assembly == interfaceType.Assembly);
            if (match == null &&
                type.Name == interfaceType.Name &&
                type.Namespace == interfaceType.Namespace &&
                type.Assembly == interfaceType.Assembly)
                match = type;
            genericParameters = match?.GetGenericArguments();
            return match != null;
        }

        internal static long ByteCount(this PropertyInfo property, object target)
        {
            if (target == null) throw new NullReferenceException(nameof(target));
            switch (property.GetValue(target))
            {
                case null: return 0;
                case string str: return Encoding.UTF8.GetByteCount(str);
                case Starcounter.Binary binary: return binary.ToArray().Length;
                default: return CountBytes(property.PropertyType);
            }
        }

        internal static long CountBytes(this Type type)
        {
            if (type.IsEnum) return 8;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    if (type.IsNullable(out var baseType)) return CountBytes(baseType);
                    if (type.HasAttribute<DatabaseAttribute>()) return 16;
                    throw new Exception($"Unknown type encountered: '{type.RESTarTypeName()}'");
                case TypeCode.Boolean: return 4;
                case TypeCode.Char: return 2;
                case TypeCode.SByte: return 1;
                case TypeCode.Byte: return 1;
                case TypeCode.Int16: return 2;
                case TypeCode.UInt16: return 2;
                case TypeCode.Int32: return 4;
                case TypeCode.UInt32: return 4;
                case TypeCode.Int64: return 8;
                case TypeCode.UInt64: return 8;
                case TypeCode.Single: return 4;
                case TypeCode.Double: return 8;
                case TypeCode.Decimal: return 16;
                case TypeCode.DateTime: return 8;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region Other

        internal static string UriEncode(this string str) => Uri.EscapeDataString(str);
        internal static string UriDecode(this string str) => Uri.UnescapeDataString(str);

        /// <summary>
        /// Gets the object for a Starcounter object number
        /// </summary>
        /// <param name="objectNo">The Starcounter ObjectNo to get the extension for</param>
        /// <returns>The object with the specified ObjectNo</returns>
        public static T GetReference<T>(this ulong? objectNo) where T : class => Db.FromId(objectNo ?? 0) as T;

        /// <summary>
        /// Gets the object for a Starcounter object number
        /// </summary>
        /// <param name="objectNo">The Starcounter ObjectNo to get the extension for</param>
        /// <returns>The object with the specified ObjectNo</returns>
        public static T GetReference<T>(this ulong objectNo) where T : class => Db.FromId(objectNo) as T;

        internal static bool EqualsNoCase(this string s1, string s2) => string.Equals(s1, s2, OrdinalIgnoreCase);
        internal static string ToMethodsString(this IEnumerable<Method> ie) => string.Join(", ", ie);

        internal static JToken Exemplify<T>() where T : class => Exemplify(typeof(T));

        internal static JToken Exemplify(this Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.DBNull: return null;
                case TypeCode.Boolean: return false;
                case TypeCode.Char: return 'a';
                case TypeCode.SByte: return 8;
                case TypeCode.Byte: return -8;
                case TypeCode.Int16: return -16;
                case TypeCode.UInt16: return 16;
                case TypeCode.Int32: return -32;
                case TypeCode.UInt32: return 32;
                case TypeCode.Int64: return -64;
                case TypeCode.UInt64: return 64;
                case TypeCode.Decimal: return 10.0M;
                case TypeCode.Single: return 10.1;
                case TypeCode.Double: return 10.01;
                case TypeCode.DateTime: return DateTime.UtcNow;
                case TypeCode.String: return "a string";
                case TypeCode.Object:
                    switch (type)
                    {
                        case var _ when type.IsNullable(out var _base): return _base.Exemplify();
                        case var _ when type.ImplementsGenericInterface(typeof(IDictionary<,>), out _): return new JObject();
                        case var _ when type.ImplementsGenericInterface(typeof(IEnumerable<>), out var param): return new JArray {param[0].Exemplify()};
                        default:
                            var jobject = new JObject();
                            foreach (var property in type.GetDeclaredProperties().Values)
                                jobject[property.Name] = property.Type.Exemplify();
                            return jobject;
                    }
                default: return null;
            }
        }

        internal static object GetDefault(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return DEFAULT_METHOD.MakeGenericMethod(type).Invoke(null, null);
        }

        private static readonly MethodInfo DEFAULT_METHOD = typeof(ExtensionMethods)
            .GetMethod(nameof(DEFAULT), NonPublic | Static);

        private static object DEFAULT<T>() => default(T);

        internal static string Fnuttify(this string sqlKey) => $"\"{sqlKey.Replace(".", "\".\"")}\"";

        /// <summary>
        /// Checks if the type is a Nullable struct, and - if so -  returns the underlying type in the out parameter.
        /// </summary>
        public static bool IsNullable(this Type type, out Type baseType)
        {
            baseType = Nullable.GetUnderlyingType(type);
            return baseType != null;
        }

        internal static (T, T) Deconstruct<T>(this ICollection<T> collection)
        {
            if (collection.Count > 2) throw new InvalidOperationException("Collection contained more than two elements");
            return (collection.ElementAtOrDefault(0), collection.ElementAtOrDefault(1));
        }

        /// <summary>
        /// Splits a string by a separator string
        /// </summary>
        public static string[] Split(this string str, string separator, StringSplitOptions options = StringSplitOptions.None)
        {
            return str.Split(new[] {separator}, options);
        }

        /// <summary>
        /// Splits a string into two parts by a separator char, and returns a 2-tuple 
        /// holding the parts.
        /// </summary>
        public static (string, string) TSplit(this string str, char separator, bool trim = false)
        {
            var split = str.Split(new[] {separator}, 2);
            if (!trim)
                switch (split.Length)
                {
                    case 1: return (split[0], null);
                    case 2: return (split[0], split[1]);
                    default: return (null, null);
                }
            switch (split.Length)
            {
                case 1: return (split[0].Trim(), null);
                case 2: return (split[0].Trim(), split[1].Trim());
                default: return (null, null);
            }
        }

        /// <summary>
        /// Splits a string into two parts by a separator string, and returns a 2-tuple 
        /// holding the parts.
        /// </summary>
        public static (string, string) TSplit(this string str, string separator, bool trim = false)
        {
            var split = str.Split(new[] {separator}, 2, StringSplitOptions.None);
            if (!trim)
                switch (split.Length)
                {
                    default: return (null, null);
                    case 1: return (split[0], null);
                    case 2: return (split[0], split[1]);
                }
            switch (split.Length)
            {
                default: return (null, null);
                case 1: return (split[0].Trim(), null);
                case 2: return (split[0].Trim(), split[1].Trim());
            }
        }

        #endregion

        #region Resource helpers

        internal static ResourceKind GetResourceKind(this Type metatype)
        {
            switch (metatype)
            {
                case var _ when typeof(IEntityResource).IsAssignableFrom(metatype): return ResourceKind.EntityResource;
                case var _ when typeof(ITerminalResource).IsAssignableFrom(metatype): return ResourceKind.TerminalResource;
                case var _ when typeof(IBinaryResource).IsAssignableFrom(metatype): return ResourceKind.BinaryResource;
                case var _ when typeof(IEventResource).IsAssignableFrom(metatype): return ResourceKind.EventResource;
                default: return ResourceKind.All;
            }
        }

        internal static (bool allowDynamic, TermBindingRule bindingRule) GetDynamicConditionHandling(this Type type, RESTarAttribute attribute)
        {
            var dynamicConditionsAllowed = type.IsDDictionary() || typeof(IDynamicMemberValueProvider).IsAssignableFrom(type) ||
                                           attribute?.AllowDynamicConditions == true;
            var conditionBindingRule = dynamicConditionsAllowed ? TermBindingRule.DeclaredWithDynamicFallback : TermBindingRule.OnlyDeclared;
            return (dynamicConditionsAllowed, conditionBindingRule);
        }

        internal static Type GetRESTarInterfaceType(this Type resourceType) => resourceType
            .GetInterfaces()
            .FirstOrDefault(i => typeof(IEntityResourceInterface).IsAssignableFrom(i));

        internal static IEnumerable<Operator> ToOperators(this Operators operators)
        {
            var opList = new List<Operator>();
            if (operators.HasFlag(EQUALS)) opList.Add(EQUALS);
            if (operators.HasFlag(NOT_EQUALS)) opList.Add(NOT_EQUALS);
            if (operators.HasFlag(LESS_THAN)) opList.Add(LESS_THAN);
            if (operators.HasFlag(GREATER_THAN)) opList.Add(GREATER_THAN);
            if (operators.HasFlag(LESS_THAN_OR_EQUALS)) opList.Add(LESS_THAN_OR_EQUALS);
            if (operators.HasFlag(GREATER_THAN_OR_EQUALS)) opList.Add(GREATER_THAN_OR_EQUALS);
            return opList;
        }

        /// <summary>
        /// Converts a resource entitiy to a JSON.net JObject.
        /// </summary>
        internal static JObject ToJObject(this object entity)
        {
            switch (entity)
            {
                case JObject j: return j;
                case DDictionary ddict: return ddict.ToJObject();
                case Dictionary<string, dynamic> _idict: return _idict.ToJObject();
                case IDictionary idict:
                    var _jobj = new JObject();
                    foreach (DictionaryEntry pair in idict)
                        _jobj[pair.Key.ToString()] = pair.Value == null
                            ? null
                            : JToken.FromObject(pair.Value, JsonProvider.Serializer);
                    return _jobj;
            }

            var jobj = new JObject();
            entity.GetType()
                .GetDeclaredProperties()
                .Values
                .Where(p => !p.Hidden)
                .ForEach(prop =>
                {
                    object val = prop.GetValue(entity);
                    jobj[prop.Name] = val == null ? null : JToken.FromObject(val, JsonProvider.Serializer);
                });
            return jobj;
        }

        internal static string GetEntityResourceProviderId(this Type providerType)
        {
            var typeName = providerType.Name;
            if (typeName == null) throw new ArgumentNullException();
            if (typeName.EndsWith("resourceprovider", InvariantCultureIgnoreCase))
                typeName = typeName.Substring(0, typeName.Length - 16);
            else if (typeName.EndsWith("provider", InvariantCultureIgnoreCase))
                typeName = typeName.Substring(0, typeName.Length - 8);
            return typeName;
        }

        internal static Type GetWrappedType(this Type wrapperType) => wrapperType.BaseType?.GetGenericArguments()[0];

        internal static bool IsWrapper(this Type type) => typeof(IResourceWrapper).IsAssignableFrom(type);

        #endregion

        #region Filter and Process

        internal static IEnumerable<T> Filter<T>(this IEnumerable<T> entities, IFilter filter) where T : class
        {
            return filter?.Apply(entities) ?? entities;
        }

        internal static IEnumerable<JObject> Process<T>(this IEnumerable<T> entities, params IProcessor[] processors)
            where T : class
        {
            return processors.Aggregate(default(IEnumerable<JObject>), (e, p) => e != null ? p.Apply(e) : p.Apply(entities));
        }

        internal static (string WhereString, object[] Values) MakeWhereClause<T>(this IEnumerable<Condition<T>> conds, string orderByIndexName,
            out Dictionary<int, int> valuesAssignments, out bool useOrderBy) where T : class
        {
            var _valuesAssignments = new Dictionary<int, int>();
            var literals = new List<object>();
            var hasOtherIndex = true;
            var clause = string.Join(" AND ", conds.Where(c => !c.Skip).Select((c, index) =>
            {
                var (key, op, value) = (c.Term.DbKey.Fnuttify(), c.InternalOperator.SQL, (object) c.Value);
                if (value == null)
                {
                    switch (c.Operator)
                    {
                        case EQUALS:
                            op = "IS NULL";
                            break;
                        case NOT_EQUALS:
                            op = "IS NOT NULL";
                            break;
                        default: throw new Exception($"Operator '{op}' is not valid for comparison with NULL");
                    }
                    return $"t.{key} {op}";
                }
                literals.Add(c.Value);
                hasOtherIndex = hasOtherIndex
                                || c.Term.Count == 1
                                && c.Term.First is DeclaredProperty p
                                && p.ScIndexWhereFirstNames?.FirstOrDefault() is string indexName
                                && indexName != orderByIndexName;
                _valuesAssignments[index] = literals.Count - 1;
                return $"t.{key} {c.InternalOperator.SQL} ? ";
            }));
            useOrderBy = !hasOtherIndex;
            if (clause.Length == 0)
            {
                valuesAssignments = null;
                return (null, null);
            }
            valuesAssignments = _valuesAssignments;
            return ($"WHERE {clause}", literals.ToArray());
        }

        internal static (string WhereString, object[] Values) MakeWhereClause<T>(this IEnumerable<Condition<T>> conds, string orderByIndexName,
            out bool useOrderBy) where T : class
        {
            var literals = new List<object>();
            var hasOtherIndex = false;
            var clause = string.Join(" AND ", conds.Where(c => !c.Skip).Select(c =>
            {
                var (key, op, value) = (c.Term.DbKey.Fnuttify(), c.InternalOperator.SQL, (object) c.Value);
                if (value == null)
                {
                    switch (c.Operator)
                    {
                        case EQUALS:
                            op = "IS NULL";
                            break;
                        case NOT_EQUALS:
                            op = "IS NOT NULL";
                            break;
                        default: throw new Exception($"Operator '{op}' is not valid for comparison with NULL");
                    }
                    return $"t.{key} {op}";
                }
                literals.Add(c.Value);
                hasOtherIndex = hasOtherIndex
                                || c.Term.Count == 1
                                && c.Term.First is DeclaredProperty p
                                && p.ScIndexWhereFirstNames?.FirstOrDefault() is string index
                                && index != orderByIndexName;
                return $"t.{key} {c.InternalOperator.SQL} ? ";
            }));
            useOrderBy = !hasOtherIndex;
            return clause.Length > 0 ? ($"WHERE {clause} ", literals.ToArray()) : (null, null);
        }

        #endregion

        #region Dictionary helpers

        /// <summary>
        /// Gets the value of a key from an IDictionary, or null if the dictionary does not contain the key.
        /// </summary>
        public static TValue SafeGet<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            dict.TryGetValue(key, out var value);
            return value;
        }

        /// <summary>
        /// Adds the tuple to the IDictionary
        /// </summary>
        public static void TAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, (TKey key, TValue value) pair)
        {
            dict.Add(pair.key, pair.value);
        }

        /// <summary>
        /// Puts the tuple into the IDictionary
        /// </summary>
        public static void TPut<TKey, TValue>(this IDictionary<TKey, TValue> dict, (TKey key, TValue value) pair)
        {
            dict[pair.key] = pair.value;
        }

        /// <summary>
        /// Puts the KeyValuePair into the IDictionary
        /// </summary>
        public static void Put<TKey, TValue>(this IDictionary<TKey, TValue> dict, KeyValuePair<TKey, TValue> pair)
        {
            dict[pair.Key] = pair.Value;
        }

        /// <summary>
        /// Gets the value of a key from an IDictionary, or null if the dictionary does not contain the key.
        /// </summary>
        private static dynamic SafeGet(this IDictionary dict, string key)
        {
            return dict.Contains(key) ? dict[key] : null;
        }

        internal static Dictionary<TKey, T> SafeToDictionary<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector,
            IEqualityComparer<TKey> equalityComparer)
        {
            var dictionary = new Dictionary<TKey, T>(equalityComparer);
            foreach (var item in source)
                dictionary[keySelector(item)] = item;
            return dictionary;
        }

        /// <summary>
        /// Gets the value of a key from an IDictionary, without case sensitivity, or null if the dictionary does 
        /// not contain the key. The actual key is returned in the actualKey out parameter.
        /// </summary>
        internal static bool TryFindInDictionary<T>(this IDictionary<string, T> dict, string key, out string actualKey,
            out T result)
        {
            result = default;
            actualKey = null;
            var matches = dict.Where(pair => pair.Key.EqualsNoCase(key)).ToList();
            switch (matches.Count)
            {
                case 0: return false;
                case 1:
                    actualKey = matches[0].Key;
                    result = matches[0].Value;
                    return true;
                default:
                    if (!dict.TryGetValue(key, out result)) return false;
                    actualKey = key;
                    return true;
            }
        }

        internal static string Capitalize(this string input)
        {
            var array = input.ToCharArray();
            array[0] = char.ToUpper(array[0]);
            return new string(array);
        }

        /// <summary>
        /// Converts a DDictionary object to a JSON.net JObject
        /// </summary>
        public static JObject ToJObject(this DDictionary d)
        {
            var jobj = new JObject();
            d.KeyValuePairs.ForEach(pair => jobj[pair.Key] = MakeJToken(pair.Value));
            jobj["$ObjectNo"] = d.GetObjectNo();
            return jobj;
        }

        /// <summary>
        /// Converts a Dictionary object to a JSON.net JObject
        /// </summary>
        public static JObject ToJObject(this Dictionary<string, dynamic> d)
        {
            var jobj = new JObject();
            d.ForEach(pair => jobj[pair.Key] = MakeJToken(pair.Value));
            return jobj;
        }

        private static JToken MakeJToken(dynamic value)
        {
            try
            {
                return (JToken) value;
            }
            catch
            {
                try
                {
                    return new JArray(value);
                }
                catch
                {
                    return JToken.FromObject(value);
                }
            }
        }

        private static IEnumerable<DictionaryEntry> Cast(this IDictionary dict) => dict.Cast<DictionaryEntry>();

        #endregion

        #region Requests

        private static readonly CultureInfo en_US = new CultureInfo("en-US");

        internal static string GetFriendlyTypeName(this Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case var _ when type.IsNullable(out var @base): return @base.GetFriendlyTypeName();
                case TypeCode.Boolean: return "a boolean (true or false)";
                case TypeCode.Char: return "a single character";
                case TypeCode.SByte: return $"an integer ({sbyte.MinValue} to {sbyte.MaxValue})";
                case TypeCode.Byte: return $"a positive integer ({byte.MinValue} to {byte.MaxValue})";
                case TypeCode.Int16: return $"an integer ({short.MinValue} to {short.MaxValue})";
                case TypeCode.UInt16: return $"a positive integer ({ushort.MinValue} to {ushort.MaxValue})";
                case TypeCode.Int32: return "an integer (32-bit)";
                case TypeCode.Int64: return "an integer (64-bit)";
                case TypeCode.UInt32: return "a positive integer (32-bit)";
                case TypeCode.UInt64: return "a positive integer (64-bit)";
                case TypeCode.Single: return "a floating point number (single)";
                case TypeCode.Double: return "a floating point number (double)";
                case TypeCode.Decimal: return "a floating point number";
                case TypeCode.String: return "a string";
                case TypeCode.DateTime: return "a date time";
                default: return type.FullName;
            }
        }

        internal static Results.Error AsError(this Exception exception)
        {
            switch (exception)
            {
                case Results.Error re: return re;
                case FormatException _: return new UnsupportedContent(exception);
                case JsonReaderException jre: return new FailedJsonDeserialization(jre);
                case DbException _: return new ScDatabaseError(exception);
                case RuntimeBinderException _: return new BinderPermissions(exception);
                case NotImplementedException _: return new FeatureNotImplemented("RESTar encountered a call to a non-implemented method");
                default: return new Unknown(exception);
            }
        }

        internal static IResult AsResultOf(this Exception exception, IRequestInternal request)
        {
            var error = exception.AsError();
            error.SetTrace(request);
            error.RequestInternal = request;
            string errorId = null;
            if (request is IEntityRequest entityRequest && entityRequest.Macro is Macro macro)
                Db.TransactAsync(() => macro.CheckIfValid());
            if (!(error is Forbidden) && !(request is RemoteRequest) && request.Method >= 0)
            {
                Db.TransactAsync(() =>
                {
                    Admin.Error.ClearOld();
                    errorId = Admin.Error.Create(error, request).Id;
                });
            }
            if (request.IsWebSocketUpgrade)
            {
                if (error is Forbidden)
                {
                    request.Context.WebSocket.Disconnect();
                    return new WebSocketUpgradeFailed(error);
                }
                if (request.Context.WebSocket?.Status == WebSocketStatus.Waiting)
                    request.Context.WebSocket?.Open();
                request.Context.WebSocket?.SendResult(error);
                request.Context.WebSocket?.Disconnect();
                return new WebSocketUpgradeSuccessful(request);
            }
            if (request.Headers.Metadata.EqualsNoCase("full"))
                error.Headers.Metadata = error.Metadata;
            error.Headers.Version = RESTarConfig.Version;
            if (errorId != null)
                error.Headers.Error = $"/restar.admin.error/id={errorId.UriEncode()}";
            error.TimeElapsed = request.TimeElapsed;
            return error;
        }

        internal static ISerializedResult Finalize(this ISerializedResult result, IContentTypeProvider acceptProvider)
        {
            switch (result.Body = result.Body.Finalize())
            {
                case null: return result;
                case RESTarStream rs when rs.ContentType.IsDefault:
                    rs.ContentType = acceptProvider.ContentType;
                    break;
            }
            if (!result.Headers.ContentType.HasValue)
                result.Headers.ContentType = acceptProvider.ContentType;
            return result;
        }

        [Pure]
        internal static Stream Finalize(this Stream stream)
        {
            switch (stream?.CanRead)
            {
                case true when !stream.CanSeek: return stream;
                case null:
                case false:
                case true when stream.Length == 0:
                    stream?.Dispose();
                    return null;
                default:
                    stream.Seek(0, SeekOrigin.Begin);
                    return stream;
            }
        }

        /// <summary>
        /// Creates a new writeable UriComponents instance from a possibly read-only IUriComponents instance
        /// </summary>
        public static UriComponents ToWritable(this IUriComponents components)
        {
            return components as UriComponents ?? new UriComponents(components);
        }

        /// <summary>
        /// Generates new UriComponents that encode a request for the next page of entities, calculated from an IEntities entity collection.
        /// The count parameter controls the size of the next page. If omitted, the size is the same as the current page.
        /// </summary>
        public static IUriComponents GetNextPageLink(this IEntities entities, int count = -1)
        {
            var components = entities.Request.UriComponents.ToWritable();
            if (count > -1)
            {
                components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase(nameof(Limit)));
                components.MetaConditions.Add(new UriCondition(RESTarMetaCondition.Limit, count.ToString()));
            }
            components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase(nameof(Offset)));
            var previousOffset = entities.Request.MetaConditions.Offset;
            var offsetNr = (long) entities.EntityCount + previousOffset.Number;
            UriCondition offset;
            if (previousOffset == -1)
                offset = new UriCondition(RESTarMetaCondition.Offset, "∞");
            else if (previousOffset < -1 && offsetNr >= -1)
                offset = new UriCondition(RESTarMetaCondition.Offset, (-1).ToString());
            else offset = new UriCondition(RESTarMetaCondition.Offset, offsetNr.ToString());
            components.MetaConditions.Add(offset);
            return components;
        }

        /// <summary>
        /// Generates new UriComponents that encode a request for the previous page of entities, calculated from an IEntities entity collection.
        /// The count parameter controls the size of the next page. If omitted, the size is the same as the current page.
        /// </summary>
        public static IUriComponents GetPreviousPageLink(this IEntities entities, int count = -1)
        {
            var components = entities.Request.UriComponents.ToWritable();
            var previousOffset = entities.Request.MetaConditions.Offset;
            var pageSize = (long) entities.EntityCount;
            if (count > -1)
            {
                components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase(nameof(Limit)));
                components.MetaConditions.Add(new UriCondition(RESTarMetaCondition.Limit, count.ToString()));
                pageSize = count;
            }
            var offsetNr = previousOffset.Number - pageSize;
            components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase(nameof(Offset)));
            UriCondition offset;
            if (previousOffset == 0)
                offset = new UriCondition(RESTarMetaCondition.Offset, "-∞");
            else if (previousOffset > 0 && offsetNr <= 0)
            {
                if (long.TryParse(components.MetaConditions.FirstOrDefault(c => c.Key.EqualsNoCase(nameof(Limit)))?.ValueLiteral, out var limit))
                {
                    components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase(nameof(Limit)));
                    components.MetaConditions.Add(new UriCondition(RESTarMetaCondition.Limit, (limit + offsetNr).ToString()));
                }
                offset = new UriCondition(RESTarMetaCondition.Offset, 0.ToString());
            }
            else offset = new UriCondition(RESTarMetaCondition.Offset, offsetNr.ToString());
            components.MetaConditions.Add(offset);
            return components;
        }

        /// <summary>
        /// Generates new UriComponents that encode a request for the first number of entities, calculated from an IEntities entity collection.
        /// The count parameter controls how many entities are selected. If omitted, one entity is selected.
        /// </summary>
        public static IUriComponents GetFirstLink(this IEntities entities, int count = 1)
        {
            var components = entities.Request.UriComponents.ToWritable();
            components.MetaConditions.RemoveAll(m => m.Key.EqualsNoCase(nameof(Offset)) || m.Key.EqualsNoCase(nameof(Limit)));
            components.MetaConditions.Add(new UriCondition(RESTarMetaCondition.Limit, count.ToString()));
            return components;
        }

        /// <summary>
        /// Generates new UriComponents that encode a request for the last number of entities, calculated from an IEntities entity collection.
        /// The count parameter controls how many entities are selected. If omitted, one entity is selected.
        /// </summary>
        public static IUriComponents GetLastLink(this IEntities entities, int count = 1)
        {
            var components = entities.Request.UriComponents.ToWritable();
            components.MetaConditions.RemoveAll(m => m.Key.EqualsNoCase(nameof(Offset)) || m.Key.EqualsNoCase(nameof(Limit)));
            components.MetaConditions.Add(new UriCondition(RESTarMetaCondition.Offset, (-count).ToString()));
            components.MetaConditions.Add(new UriCondition(RESTarMetaCondition.Limit, count.ToString()));
            return components;
        }

        /// <summary>
        /// Generates new UriComponents that encode a request for all entities in a resource, calculated from an IEntities entity collection.
        /// </summary>
        public static IUriComponents GetAllLink(this IEntities entities)
        {
            var components = entities.Request.UriComponents.ToWritable();
            components.MetaConditions.RemoveAll(m => m.Key.EqualsNoCase(nameof(Offset)) || m.Key.EqualsNoCase(nameof(Limit)));
            return components;
        }

        /// <summary>
        /// Parses a condition value from a value literal, and performs an optional type check (non-optional for enums)
        /// </summary>
        internal static object ParseConditionValue(this string valueLiteral, DeclaredProperty property = null)
        {
            switch (valueLiteral)
            {
                case null:
                case "null": return null;
                case "": return "";
            }

            if (property is DeclaredProperty prop && prop.IsEnum)
            {
                try
                {
                    if (prop.IsNullable)
                    {
                        var type = Nullable.GetUnderlyingType(prop.Type) ?? prop.Type;
                        return Enum.Parse(type, valueLiteral, true);
                    }
                    return Enum.Parse(property.Type, valueLiteral, true);
                }
                catch
                {
                    throw new InvalidSyntax(InvalidEnumValue, $"'{valueLiteral}' is not a valid enum value for property '{property.Name}'");
                }
            }
            var (first, length) = (valueLiteral[0], valueLiteral.Length);
            var escapedString = false;
            switch (first)
            {
                case '\'':
                case '\"':
                    if (length > 1 && valueLiteral[length - 1] == first)
                    {
                        valueLiteral = valueLiteral.Substring(1, length - 2);
                        escapedString = true;
                    }
                    break;
            }
            if (property != null)
            {
                try
                {
                    if (property.IsDateTime)
                    {
                        if (DateTime.TryParse(valueLiteral, null, AssumeUniversal, out var dateTime))
                            return dateTime.ToUniversalTime();
                        throw new Exception();
                    }
                    return Convert.ChangeType(valueLiteral, property.Type.IsNullable(out var t) ? t : property.Type);
                }
                catch
                {
                    throw new InvalidConditionValueType(valueLiteral, property);
                }
            }
            if (escapedString) return valueLiteral;
            switch (valueLiteral)
            {
                case "false":
                case "False":
                case "FALSE": return false;
                case "true":
                case "True":
                case "TRUE": return true;
            }
            if (char.IsDigit(first))
            {
                if (int.TryParse(valueLiteral, out var i))
                    return i;
                if (decimal.TryParse(valueLiteral, out var d))
                    return d;
                if (DateTime.TryParse(valueLiteral, null, AssumeUniversal, out var dateTime))
                    return dateTime.ToUniversalTime();
            }
            return valueLiteral;
        }

        /// <summary>
        /// Returns true if and only if the request contains a condition with the given key and 
        /// operator (case insensitive). If true, the out Condition parameter will contain a reference to the found
        /// condition.
        /// </summary>
        public static bool TryGetCondition<T>(this IRequest<T> request, string key, Operators op,
            out Condition<T> condition) where T : class
        {
            condition = request.Conditions?.Get(key, op);
            return condition != null;
        }

        /// <summary>
        /// Returns true if and only if the request contains at least one condition with the given key (case insensitive). 
        /// If true, the out Conditions parameter will contain all the matching conditions
        /// </summary>
        /// <returns></returns>
        public static bool TryGetConditions<T>(this IRequest<T> request, string key,
            out ICollection<Condition<T>> conditions) where T : class
        {
            conditions = request.Conditions.Get(key).ToList();
            return !conditions.Any() != true;
        }

        #endregion

        #region Conversion

        /// <summary>
        /// Generates a content stream from a CLR object. If no content type is provided, uses the content type of the
        /// request. If neither content type nor request are present, JSON is used if serialization is needed. If no
        /// content type is provided with binary data (byte array or stream) an exception is thrown.
        /// </summary>
        internal static RESTarStream ToStream<T>(this T content, ContentType? contentType = null, IRequestInternal request = null)
            where T : class
        {
            ContentType _contentType;
            switch (content)
            {
                case Stream _stream:
                    _contentType = contentType ?? ResolveInputContentType(request);
                    if (_contentType.IsDefault) throw new ArgumentException("Missing content type for binary data", nameof(contentType));
                    return new RESTarStream(_contentType, _stream);
                case byte[] bytes:
                    _contentType = contentType ?? ResolveInputContentType(request);
                    if (_contentType.IsDefault) throw new ArgumentException("Missing content type for binary data", nameof(contentType));
                    return new RESTarStream(_contentType, bytes);
                case string str:
                    _contentType = contentType ?? ResolveInputContentType(request);
                    if (_contentType.IsDefault) _contentType = "text/plain";
                    return new RESTarStream(_contentType, str.ToBytes());
                case null: throw new ArgumentNullException(nameof(content));
            }

            IContentTypeProvider provider;
            switch (request)
            {
                case null when contentType.HasValue && InputContentTypeProviders.TryGetValue(contentType.ToString(), out provider):
                    break;
                case null:
                    provider = Providers.Json;
                    break;
                case var _request:
                    provider = ResolveInputContentTypeProvider(_request, contentType);
                    _request.Headers.ContentType = provider.ContentType;
                    break;
            }
            _contentType = provider.ContentType;

            var stream = new RESTarStream(_contentType);
            switch (content)
            {
                case IDictionary<string, object> _:
                case JObject _:
                    provider.SerializeCollection(new[] {content}, stream);
                    break;
                case IEnumerable<object> ie:
                    provider.SerializeCollection(ie, stream);
                    break;
                case IEnumerable ie:
                    provider.SerializeCollection(ie.Cast<object>(), stream);
                    break;
                default:
                    provider.SerializeCollection(new[] {content}, stream);
                    break;
            }
            return stream.Rewind();
        }

        internal static string SHA256(this string input)
        {
            using (var hasher = System.Security.Cryptography.SHA256.Create())
                return Convert.ToBase64String(hasher.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        internal static byte[] ToBytes(this string str)
        {
            return str != null ? Encoding.UTF8.GetBytes(str) : null;
        }

        internal static string TotalMessage(this Exception e)
        {
            var message = new StringBuilder();
            if (string.IsNullOrWhiteSpace(e.Message))
                message.Append(e.Message);
            var ie = e.InnerException;
            while (ie != null)
            {
                if (!string.IsNullOrWhiteSpace(ie.Message))
                    message.Append(ie.Message);
                if (ie.InnerException != null)
                    message.Append(" | ");
                ie = ie.InnerException;
            }
            return message.ToString().Replace("\r\n", " | ");
        }

        internal static byte[] ToByteArray(this Stream stream)
        {
            switch (stream)
            {
                case null: return null;
                case MemoryStream _ms: return _ms.ToArray();
                case RESTarStream rsc: return rsc.GetBytes();
                default:
                    using (var ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        if (stream.CanSeek)
                            stream.Seek(0, SeekOrigin.Begin);
                        return ms.ToArray();
                    }
            }
        }

        internal static async Task<byte[]> ToByteArrayAsync(this Stream stream)
        {
            switch (stream)
            {
                case null: return null;
                case MemoryStream _ms: return _ms.ToArray();
                case RESTarStream rsc: return await rsc.GetBytesAsync();
                default:
                    using (var ms = new MemoryStream())
                    {
                        await stream.CopyToAsync(ms);
                        if (stream.CanSeek)
                            stream.Seek(0, SeekOrigin.Begin);
                        return ms.ToArray();
                    }
            }
        }

        //        /// <summary>
        //        /// Converts an IEnumerable of T to an Excel workbook
        //        /// </summary>
        //        public static ClosedXML.Excel.XLWorkbook ToExcel<T>(this IEnumerable<T> entities) where T : class
        //        {
        //            var resource = Resource<T>.Get;
        //            var dataSet = new DataSet();
        //            var table = entities.MakeDataTable(resource);
        //            if (table.Rows.Count == 0) return null;
        //            dataSet.Tables.Add(table);
        //            var workbook = new ClosedXML.Excel.XLWorkbook();
        //            workbook.AddWorksheet(dataSet);
        //            return workbook;
        //        }

        /// <summary>
        /// Converts a boolean into an XML boolean string, i.e. "true" or "false" 
        /// </summary>
        /// <param name="bool"></param>
        /// <returns></returns>
        public static string XMLBool(this bool @bool)
        {
            const string trueString = "true";
            const string FalseString = "false";
            if (@bool) return trueString;
            return FalseString;
        }

        /// <summary>
        /// Converts an HTTP status code to the underlying numeric code
        /// </summary>
        internal static ushort? ToCode(this HttpStatusCode statusCode) => (ushort) statusCode;

        /// <summary>
        /// Creates a formatted string representation of the URI components,
        /// a valid URI string according to the assigned protocol.
        /// </summary>
        public static string ToUriString(this IUriComponents uriComponents)
        {
            var protocolProvider = uriComponents.ProtocolProvider;
            var uriString = protocolProvider.MakeRelativeUri(uriComponents);
            if (ProtocolController.DefaultProtocolProvider.ProtocolProvider.Equals(protocolProvider))
                return uriString;
            return $"-{protocolProvider.ProtocolIdentifier}{uriString}";
        }

        internal static string ReplaceFirst(this string text, string search, string replace, out bool replaced)
        {
            var pos = text.IndexOf(search, Ordinal);
            if (pos < 0)
            {
                replaced = false;
                return text;
            }

            replaced = true;
            return $"{text.Substring(0, pos)}{replace}{text.Substring(pos + search.Length)}";
        }

        internal static Method[] ToMethodsArray(this string methodsString)
        {
            if (methodsString == null) return null;
            if (methodsString.Trim() == "*")
                return RESTarConfig.Methods;
            return methodsString.Split(',')
                .Where(s => s != "")
                .Select(s => (Method) Enum.Parse(typeof(Method), s))
                .ToArray();
        }

        #endregion
    }
}