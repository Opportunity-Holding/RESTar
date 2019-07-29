using System;
using RESTar.Resources;

namespace RESTar.Meta
{
    /// <summary>
    /// Encodes a member in a type
    /// </summary>
    public abstract class Member
    {
        /// <summary>
        /// The name of the property
        /// </summary>
        [RESTarMember(order: -5)] public string Name { get; internal set; }

        /// <summary>
        /// The name of the property, as defined in the type declaration
        /// </summary>
        public string ActualName { get; internal set; }

        /// <summary>
        /// The type of this member
        /// </summary>
        public Type Type { get; protected set; }

        /// <summary>
        /// Is this property nullable?
        /// </summary>
        public bool IsNullable { get; protected set; }

        /// <summary>
        /// Is the type an enum type?
        /// </summary>
        public bool IsEnum { get; protected set; }

        /// <summary>
        /// Is this member readable?
        /// </summary>
        public abstract bool IsReadable { get; }

        /// <summary>
        /// Is this member writable?
        /// </summary>
        public abstract bool IsWritable { get; }

        /// <summary>
        /// Is this member read-only?
        /// </summary>
        public bool IsReadOnly => IsReadable && !IsWritable;

        /// <summary>
        /// Is this member write-only?
        /// </summary>
        public bool IsWriteOnly => IsWritable && !IsReadable;

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}