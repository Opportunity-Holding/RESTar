using System;
using Newtonsoft.Json;

namespace RESTar.ContentTypeProviders
{
    internal class TypeConverter : JsonConverter<Type>
    {
        public override bool CanRead { get; } = false;
        public override void WriteJson(JsonWriter writer, Type value, JsonSerializer _) => writer.WriteValue(value.RESTarTypeName());
        public override Type ReadJson(JsonReader r, Type o, Type e, bool h, JsonSerializer _) => throw new NotImplementedException();
    }
}