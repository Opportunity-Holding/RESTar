using System.Reflection;

namespace RESTar.Meta
{
    /// <inheritdoc />
    /// <summary>
    /// Encodes a field in a type
    /// </summary>
    public class Field : Member
    {
        /// <inheritdoc />
        public override bool IsReadable { get; }

        /// <inheritdoc />
        public override bool IsWritable { get; }

        internal Field(FieldInfo fieldInfo)
        {
            Name = fieldInfo.RESTarMemberName();
            ActualName = fieldInfo.Name;
            IsReadable = IsWritable = true;
            if (fieldInfo.IsInitOnly)
                IsWritable = false;
            Type = fieldInfo.FieldType;
            IsNullable = !fieldInfo.FieldType.IsValueType || fieldInfo.FieldType.IsNullable(out _);
            IsEnum = fieldInfo.FieldType.IsEnum || fieldInfo.FieldType.IsNullable(out var @base) && @base.IsEnum;
        }
    }
}