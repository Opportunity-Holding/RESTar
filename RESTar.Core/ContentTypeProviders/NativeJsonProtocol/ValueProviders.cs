using System;
using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace RESTar.ContentTypeProviders.NativeJsonProtocol
{
    /// <inheritdoc />
    /// <summary>
    /// Tries to get the value of a collection, and returns an empty array if the value is null
    /// </summary>
    internal class ArrayNullToEmptyListProvider : IValueProvider
    {
        private readonly PropertyInfo Property;
        public ArrayNullToEmptyListProvider(PropertyInfo property) => Property = property;
        public object GetValue(object target) => Property.GetValue(target) ?? new object[0];
        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }

    /// <inheritdoc />
    /// <summary>
    /// Tries to get the value of a value type, and returns an empty object if the value is null
    /// </summary>
    internal class ClassNullToEmptyObjectProvider : IValueProvider
    {
        private readonly PropertyInfo Property;
        public ClassNullToEmptyObjectProvider(PropertyInfo property) => Property = property;
        public object GetValue(object target) => Property.GetValue(target) ?? new object();
        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }

    /// <inheritdoc />
    /// <summary>
    /// Wraps an object in the RESTar macro for delivery to the view model
    /// </summary>
    internal class RESTarMacroWrapperProvider : IValueProvider
    {
        private readonly PropertyInfo Property;
        public RESTarMacroWrapperProvider(PropertyInfo property) => Property = property;
        public void SetValue(object target, object value) { }

        public object GetValue(object target)
        {
            switch (Property.GetValue(target))
            {
                case DateTime dateTime: return $"@RESTar(\"{dateTime:O}\")";
                case string @string: return $"@RESTar(\"{@string}\")";
                case var other: return $"@RESTar({other})";
            }
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Makes the empty string act as the default for strings in view models
    /// </summary>
    internal class NullToEmptyStringValueProvider : IValueProvider
    {
        private readonly PropertyInfo Property;
        public NullToEmptyStringValueProvider(PropertyInfo property) => Property = property;
        public object GetValue(object target) => Property.GetValue(target) ?? "";
        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }
}