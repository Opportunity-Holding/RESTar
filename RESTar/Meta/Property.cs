using System;
using RESTar.Requests;

namespace RESTar.Meta
{
    /// <inheritdoc />
    /// <summary>
    /// Describes a property of a resource
    /// </summary>
    public abstract class Property : Member
    {
        /// <summary>
        /// Is this property a dynamic member?
        /// </summary>
        public abstract bool IsDynamic { get; }

        /// <summary>
        /// Is this property a declared member?
        /// </summary>
        public bool IsDeclared => !IsDynamic;

        /// <summary>
        /// The allowed condition operators for this property
        /// </summary>
        public Operators AllowedConditionOperators { get; protected set; } = Operators.All;

        /// <summary>
        /// Can this property be referred to in a Starcounter SQL query?
        /// </summary>
        public bool IsScQueryable { get; internal set; }

        /// <summary>
        /// Gets the value of this property, for a given target object
        /// </summary>
        public virtual dynamic GetValue(object target) => Getter?.Invoke(target);

        /// <summary>
        /// Sets the value of this property, for a given target object and a given value
        /// </summary>
        public virtual void SetValue(object target, dynamic value) => Setter?.Invoke(target, value);

        /// <summary>
        /// </summary>
        internal Setter Setter { get; set; }

        /// <summary>
        /// </summary>
        internal Getter Getter { get; set; }

        /// <inheritdoc />
        public override bool IsReadable => Getter != null;

        /// <inheritdoc />
        public override bool IsWritable => Setter != null;

        /// <summary>
        /// Parses an input property name string and returns a Property describing the 
        /// corresponding resource property.
        /// </summary>
        public static Property Parse(string keyString, Type resource, bool dynamic)
        {
            if (dynamic) return DynamicProperty.Parse(keyString);
            return DeclaredProperty.Find(resource, keyString);
        }
    }
}