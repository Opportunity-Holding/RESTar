using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RESTar.Meta
{
    /// <summary>
    /// A struct to describe a member of an enumeration
    /// </summary>
    public struct EnumMember
    {
        /// <summary>
        /// The name of the enumeration members
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The numeric value of the enumeration
        /// </summary>
        public readonly int NumericValue;

        /// <summary>
        /// The attributes of the enumeration member
        /// </summary>
        public readonly IEnumerable<Attribute> Attributes;

        /// <summary>
        ///  </summary>
        internal EnumMember(string name, int numericValue, IEnumerable<Attribute> attributes)
        {
            Name = name;
            NumericValue = numericValue;
            Attributes = attributes;
        }

        /// <summary>
        /// Gets the members of an enumeration
        /// </summary>
        public static EnumMember[] GetMembers(Type enumType)
        {
            return enumType.IsEnum
                ? enumType.GetFields()
                    .Where(t => t.FieldType.IsEnum)
                    .Select(t => new EnumMember
                    (
                        name: t.Name,
                        numericValue: (int) (Convert.ChangeType(t.GetValue(null), TypeCode.Int32) ?? -1),
                        attributes: t.GetCustomAttributes<Attribute>()
                    ))
                    .ToArray()
                : throw new ArgumentException($"Type must be enum, found '{enumType.RESTarTypeName()}'");
        }

        /// <summary>
        /// Returns true if and only if the enumeration member has an attribute
        /// of the given attribute type
        /// </summary>
        public bool HasAttribute<T1>() where T1 : Attribute => Attributes.OfType<T1>().Any();

        /// <summary>
        /// Returns an attribute for an enumeration, or null if there is no such
        /// attribute decoration for this member
        /// </summary>
        public T1 GetAttribute<T1>() where T1 : Attribute => Attributes.OfType<T1>().FirstOrDefault();
    }

    /// <summary>
    /// A generic struct to describe a member of an enumeration
    /// </summary>
    public struct EnumMember<T> where T : Enum
    {
        /// <summary>
        /// The name of the enumeration members
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The integer value of the enumeration
        /// </summary>
        public readonly int NumericValue;

        /// <summary>
        /// The value of the enumeration
        /// </summary>
        public readonly T Value;

        /// <summary>
        /// The attributes of the enumeration member
        /// </summary>
        public readonly IEnumerable<Attribute> Attributes;

        /// <summary>
        ///  </summary>
        internal EnumMember(IEnumerable<Attribute> attributes, T value)
        {
            Name = value.ToString();
            NumericValue = Convert.ToInt32(value);
            Value = value;
            Attributes = attributes;
        }

        /// <summary>
        /// Gets the members of an enumeration
        /// </summary>
        public static EnumMember<T>[] GetMembers() => GetMembers(null);

        /// <summary>
        /// Gets the members of an enumeration
        /// </summary>
        public static EnumMember<T>[] GetMembers(T except) => GetMembers(new[] {except});

        /// <summary>
        /// Gets the members of an enumeration
        /// </summary>
        public static EnumMember<T>[] GetMembers(IEnumerable<T> except)
        {
            var exceptionStrings = except?.Select(e => e.ToString()).ToList();
            return typeof(T).IsEnum
                ? typeof(T).GetFields()
                    .Where(field => field.FieldType.IsEnum)
                    .Where(field => exceptionStrings?.Contains(field.Name) != true)
                    .Select(field => new EnumMember<T>
                    (
                        value: (T) (field.GetValue(null) ?? -1),
                        attributes: field.GetCustomAttributes<Attribute>()
                    ))
                    .ToArray()
                : throw new ArgumentException($"Type must be enum, found '{typeof(T).RESTarTypeName()}'");
        }

        /// <summary>
        /// Gets all values for named constants of an enumeration
        /// </summary>
        public static T[] Values => typeof(T).IsEnum
            ? typeof(T).GetFields()
                .Where(field => field.FieldType.IsEnum)
                .Select(field => (T) (field.GetValue(null) ?? -1))
                .ToArray()
            : throw new ArgumentException($"Type must be enum, found '{typeof(T).RESTarTypeName()}'");

        /// <summary>
        /// Gets all names for named constants of an enumeration
        /// </summary>
        public static string[] Names => typeof(T).IsEnum
            ? Enum.GetNames(typeof(T))
            : throw new ArgumentException($"Type must be enum, found '{typeof(T).RESTarTypeName()}'");

        /// <summary>
        /// Returns true if and only if the enumeration member has an attribute
        /// of the given attribute type
        /// </summary>
        public bool HasAttribute<T1>() where T1 : Attribute => Attributes.OfType<T1>().Any();

        /// <summary>
        /// Returns an attribute for an enumeration, or null if there is no such
        /// attribute decoration for this member
        /// </summary>
        public T1 GetAttribute<T1>() where T1 : Attribute => Attributes.OfType<T1>().FirstOrDefault();
    }
}