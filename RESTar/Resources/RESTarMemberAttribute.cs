using System;
using RESTar.Requests;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Adds a configuration to this RESTar resource type member
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RESTarMemberAttribute : Attribute
    {
        /// <summary>
        /// Should this property be completely ignored by RESTar? Equivalent to using the .NET standard IgnoreDataMemberAttribute 
        /// </summary>
        public bool Ignored { get; }

        /// <summary>
        /// A new name for this property, used by RESTar. Equivalent to using the .NET standard DataMemberAttribute attribute.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The order at which this property appears when all properties are enumerated
        /// </summary>
        public int? Order { get; }

        /// <summary>
        /// Should this property be hidden in output by default? It can still be added and queried against.
        /// To make RESTar completely ignore a property, use the Ignored property.
        /// </summary>
        public bool Hidden { get; }

        /// <summary>
        /// Should this property be hidden in output if the value is null? Only applies to JSON output.
        /// </summary>
        public bool HiddenIfNull { get; }

        /// <summary>
        /// Makes this property read only over the REST API, even if it has a public setter.
        /// </summary>
        public bool ReadOnly { get; }

        /// <summary>
        /// Sets the Skip property of all conditions matched against this property to true by default.
        /// </summary>
        public bool SkipConditions { get; }

        /// <summary>
        /// These operators will be allowed in conditions targeting this property.
        /// </summary>
        public Operators AllowedOperators { get; }

        /// <summary>
        /// The name of the excel reducer, passed as argument in the constructor
        /// </summary>
        public string ExcelReducerName { get; }

        /// <summary>
        /// Should this object be replaced with a new instance on update, or reused? Applicable for types 
        /// such as Dictionaries and Lists.
        /// </summary>
        public bool ReplaceOnUpdate { get; }

        /// <summary>
        /// The custom datetime format string of this property (if any)
        /// </summary>
        public string DateTimeFormat { get; }

        /// <summary>
        /// Should this member, and all its members, be merged onto the owner type when serializing?
        /// </summary>
        public bool MergeOntoOwner { get; }

        /// <inheritdoc />
        /// <summary>
        /// Creates a new RESTar property configuration for a property
        /// </summary>
        /// <param name="ignore">Should this property be completely ignored by RESTar?</param>
        /// <param name="name">A new name for this property.</param>
        /// <param name="order">The order at which this property appears when all properties are enumerated.</param>
        /// <param name="hide">Should this property be hidden in serialized output by default? It can still be added and queried against.</param>
        /// <param name="hideIfNull">Should this property be hidden in output if the value is null? Only applies to JSON output.</param>
        /// <param name="readOnly">Makes this property read-only over the REST API, even if it has a public setter.</param>
        /// <param name="skipConditions">Sets the Skip property of all conditions matched against this property to true by default.</param>
        /// <param name="allowedOperators">These operators will be allowed in conditions targeting this property.</param>
        /// <param name="excelReducer">The name of an optional public ToString-like method, declared in the same scope as the property, that reduces the property to an excel-compatible string.</param>
        /// <param name="replaceOnUpdate">Should this object be replaced with a new instance on update, or reused? Applicable for types such as Dictionaries and Lists.</param>
        /// <param name="dateTimeFormat">A custom datetime format string to use when writing and reading this property</param>
        /// <param name="mergeOntoOwner">Should this member, and all its members, be merged onto the owner type when serializing?</param>
        public RESTarMemberAttribute(bool ignore = false, string name = null, int order = int.MinValue, bool hide = false,
            bool hideIfNull = false, bool readOnly = false, bool skipConditions = false, Operators allowedOperators = Operators.All,
            string excelReducer = null, bool replaceOnUpdate = false, string dateTimeFormat = null, bool mergeOntoOwner = false)
        {
            Ignored = ignore;
            Name = name;
            if (order != int.MinValue)
                Order = order;
            Hidden = hide;
            HiddenIfNull = hideIfNull;
            ReadOnly = readOnly;
            SkipConditions = skipConditions;
            AllowedOperators = allowedOperators;
            ExcelReducerName = excelReducer;
            ReplaceOnUpdate = replaceOnUpdate;
            DateTimeFormat = dateTimeFormat;
            MergeOntoOwner = mergeOntoOwner;
        }
    }
}