using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using RESTar.Linq;
using RESTar.Meta.IL;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Results;
using Starcounter;
using Starcounter.Metadata;

namespace RESTar.Meta
{
    /// <inheritdoc />
    /// <summary>
    /// A declared property represents a compile time known property of a type.
    /// </summary>
    public class DeclaredProperty : Property
    {
        /// <summary>
        /// A unique identifier for this property
        /// </summary>
        private int MetadataToken { get; }

        /// <inheritdoc />
        public override bool IsDynamic => false;

        /// <summary>
        /// The order at which this property appears when all properties are enumerated
        /// </summary>
        public int? Order { get; }

        /// <summary>
        /// Hidden properties are not included in regular output, but can be added and queried on.
        /// </summary>
        public bool Hidden { get; }

        /// <summary>
        /// Should this property be hidden in output if the value is null? Only applies to JSON output.
        /// </summary>
        public bool HiddenIfNull { get; }

        /// <summary>
        /// Automatically sets the Skip property of conditions matched against this property to true
        /// </summary>
        public bool SkipConditions { get; }

        /// <summary>
        /// The function to use when reduding this property to an Excel-compatible string
        /// </summary>
        public dynamic ExcelReducer { get; }

        /// <summary>
        /// Should this object be replaced with a new instance on update, or reused? Applicable for types 
        /// such as Dictionaries and Lists.
        /// </summary>
        public bool ReplaceOnUpdate { get; }

        /// <summary>
        /// Does this declared property represent a datetime?
        /// </summary>
        public bool IsDateTime { get; }

        /// <summary>
        /// A custom datetime format string of this property (if any)
        /// </summary>
        public string CustomDateTimeFormat { get; }

        /// <summary>
        /// The starcounter indexable column for this property (if any)
        /// </summary>
        [RESTarMember(ignore: true)] public Column ScIndexableColumn { get; }

        /// <summary>
        /// The name of the starcounter indexable column for this property (if any)
        /// </summary>
        public string ScIndexableColumnName => ScIndexableColumn?.Name;

        /// <summary>
        /// The names of the indexes registered for this property
        /// </summary>
        public IEnumerable<string> ScIndexWhereFirstNames => ScIndexesWhereFirst?.Select(i => i.Name);

        /// <summary>
        /// The index belonging to this property (if any) 
        /// </summary>
        [RESTarMember(ignore: true)] public IEnumerable<Index> ScIndexesWhereFirst => ScIndexableColumn is Column column
            ? Db.SQL<Index>("SELECT t.\"Index\" FROM Starcounter.Metadata.IndexedColumn t " +
                            "WHERE t.\"Column\" =? AND t.\"Position\" =?", column, 0UL)
            : null;


        /// <summary>
        /// The attributes that this property has been decorated with
        /// </summary>  
        private ICollection<Attribute> Attributes { get; }

        /// <summary>
        /// Gets the first instance of a given attribute type that this resource property 
        /// has been decorated with.
        /// </summary>
        public T GetAttribute<T>() where T : Attribute => Attributes?.OfType<T>().FirstOrDefault();

        /// <summary>
        /// Returns true if and only if this resource property has been decorated with the given 
        /// attribute type.
        /// </summary>
        public bool HasAttribute<TAttribute>() where TAttribute : Attribute => GetAttribute<TAttribute>() != null;

        /// <summary>
        /// Returns true if and only if this resource property has been decorated with the given 
        /// attribute type.
        /// </summary>
        public bool HasAttribute<TAttribute>(out TAttribute attribute) where TAttribute : Attribute => (attribute = GetAttribute<TAttribute>()) != null;

        /// <summary>
        /// Used in SpecialProperty
        /// </summary>
        internal DeclaredProperty(int metadataToken, string name, string actualName, Type type, int? order, bool isScQueryable,
            ICollection<Attribute> attributes, bool skipConditions, bool hidden, bool hiddenIfNull, bool isEnum, string customDateTimeFormat,
            Operators allowedConditionOperators, Getter getter, Setter setter)
        {
            MetadataToken = metadataToken;
            Name = name;
            Type = type;
            ActualName = actualName;
            Order = order;
            IsScQueryable = isScQueryable;
            Attributes = attributes;
            SkipConditions = skipConditions;
            Hidden = hidden;
            HiddenIfNull = hiddenIfNull;
            IsEnum = isEnum;
            AllowedConditionOperators = allowedConditionOperators;
            IsNullable = !type.IsValueType || type.IsNullable(out _) || hidden;
            CustomDateTimeFormat = customDateTimeFormat;
            IsDateTime = type == typeof(DateTime) || type == typeof(DateTime?);
            Getter = getter;
            Setter = setter;
            ScIndexableColumn = null;
        }

        /// <summary>
        /// The regular constructor, called by the type cache when creating declared properties
        /// </summary>
        internal DeclaredProperty(PropertyInfo p, bool flagName = false)
        {
            if (p == null) return;
            MetadataToken = p.MetadataToken;
            Name = p.RESTarMemberName(flagName);
            Type = p.PropertyType;
            ActualName = p.Name;
            Attributes = p.GetCustomAttributes().ToList();
            var memberAttribute = GetAttribute<RESTarMemberAttribute>();
            var jsonAttribute = GetAttribute<JsonPropertyAttribute>();
            CustomDateTimeFormat = memberAttribute?.DateTimeFormat;
            Order = memberAttribute?.Order ?? jsonAttribute?.Order;
            IsScQueryable = p.DeclaringType?.HasAttribute<DatabaseAttribute>() == true && p.PropertyType.IsStarcounterCompatible();
            SkipConditions = memberAttribute?.SkipConditions == true || p.DeclaringType.HasAttribute<RESTarViewAttribute>();
            Hidden = memberAttribute?.Hidden == true;
            HiddenIfNull = memberAttribute?.HiddenIfNull == true || jsonAttribute?.NullValueHandling == NullValueHandling.Ignore;
            AllowedConditionOperators = memberAttribute?.AllowedOperators ?? Operators.All;
            IsNullable = !p.PropertyType.IsValueType || p.PropertyType.IsNullable(out _) || Hidden;
            IsEnum = p.PropertyType.IsEnum || p.PropertyType.IsNullable(out var @base) && @base.IsEnum;
            IsDateTime = p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?);
            if (memberAttribute?.ExcelReducerName != null)
                ExcelReducer = MakeExcelReducer(memberAttribute.ExcelReducerName, p);
            Getter = p.MakeDynamicGetter();
            if (memberAttribute?.ReadOnly != true)
                Setter = p.MakeDynamicSetter();
            ReplaceOnUpdate = memberAttribute?.ReplaceOnUpdate == true;
            if (p.DeclaringType.HasAttribute<DatabaseAttribute>() && !p.HasAttribute<TransientAttribute>())
            {
                const string columnSQL = "SELECT t FROM Starcounter.Metadata.\"Column\" t WHERE t.\"Table\".FullName =? AND t.Name =?";
                var method = p.GetGetMethod();

                string columnNameGuess;
                if (method.HasAttribute<CompilerGeneratedAttribute>())
                    columnNameGuess = p.Name;
                else
                {
                    columnNameGuess = method
                        .SafeSelect(m => m
                            .GetInstructions()
                            .Select(i =>
                            {
                                if (i.OpCode == OpCodes.Call
                                    && i.Operand is MethodInfo calledMethod
                                    && calledMethod.DeclaringType == p.DeclaringType
                                    && calledMethod.ReturnType == p.PropertyType
                                    && !calledMethod.IsStatic
                                    && !calledMethod.HasAttribute<CompilerGeneratedAttribute>()
                                    && calledMethod.Name.StartsWith("get_"))
                                    return calledMethod.Name.Substring(4);
                                return null;
                            }))?
                        .LastOrDefault(n => n != null);
                }
                if (columnNameGuess != null)
                    ScIndexableColumn = Db.SQL<Column>(columnSQL, p.DeclaringType.RESTarTypeName(), columnNameGuess).FirstOrDefault();
            }
        }

        private static dynamic MakeExcelReducer(string methodName, PropertyInfo p)
        {
            if (p.DeclaringType == null) throw new Exception("Type error, cannot cache property " + p);
            try
            {
                var method = p.DeclaringType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance) ?? throw new Exception();
                return method.CreateDelegate(typeof(Func<,>).MakeGenericType(p.DeclaringType, typeof(string)));
            }
            catch
            {
                throw new Exception($"Invalid or unknown excel reduce function '{methodName}' for property '{p.Name}' in type '" +
                                    $"{p.DeclaringType.RESTarTypeName()}'. Must be public instance method with signature 'public " +
                                    "string <insert-name-here>()'");
            }
        }

        /// <summary>
        /// Parses a declared property from a key string and a type
        /// </summary>
        /// <param name="type">The type to match the property from</param>
        /// <param name="key">The string to match a property from</param>
        /// <returns></returns>
        public static DeclaredProperty Find(Type type, string key)
        {
            if (!type.GetDeclaredProperties().TryGetValue(key, out var prop))
            {
                if (type.IsNullable(out var underlying))
                    type = underlying;
                throw new UnknownProperty(type, key);
            }
            return prop;
        }

        /// <summary>
        /// Parses a declared property from a key string and a type
        /// </summary>
        /// <param name="type">The type to match the property from</param>
        /// <param name="key">The string to match a property from</param>
        /// <param name="declaredProperty">The declared property found</param>
        /// <returns></returns>
        public static bool TryFind(Type type, string key, out DeclaredProperty declaredProperty)
        {
            return type.GetDeclaredProperties().TryGetValue(key, out declaredProperty);
        }

        internal long ByteCount(object target)
        {
            if (target == null) throw new NullReferenceException(nameof(target));
            switch (GetValue(target))
            {
                case null: return 0;
                case string str: return Encoding.UTF8.GetByteCount(str);
                case Starcounter.Binary binary: return binary.ToArray().Length;
                default: return Type.CountBytes();
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{Type.RESTarTypeName()}.{Name}";

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is DeclaredProperty p && p.MetadataToken == MetadataToken;

        /// <inheritdoc />
        public override int GetHashCode() => MetadataToken;

        /// <summary>
        /// Compares properties by name
        /// </summary>
        public static readonly IEqualityComparer<DeclaredProperty> NameComparer = new _NameComparer();

        /// <summary>
        /// Compares properties by identity
        /// </summary>
        public static IEqualityComparer<DeclaredProperty> IdentityComparer = new _IdentityComparer();

        private class _NameComparer : IEqualityComparer<DeclaredProperty>
        {
            public bool Equals(DeclaredProperty x, DeclaredProperty y) => x?.Name == y?.Name;
            public int GetHashCode(DeclaredProperty obj) => obj.Name.GetHashCode();
        }

        private class _IdentityComparer : IEqualityComparer<DeclaredProperty>
        {
            public bool Equals(DeclaredProperty x, DeclaredProperty y) => x?.MetadataToken == y?.MetadataToken;
            public int GetHashCode(DeclaredProperty obj) => obj.MetadataToken;
        }
    }
}