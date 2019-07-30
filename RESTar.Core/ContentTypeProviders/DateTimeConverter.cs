using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RESTar.ContentTypeProviders
{
    internal class DateTimeConverter : IsoDateTimeConverter
    {
        internal static IDictionary<string, DateTimeConverter> Converters { get; }
        static DateTimeConverter() => Converters = new ConcurrentDictionary<string, DateTimeConverter>();

        internal DateTimeConverter(string formatString)
        {
            DateTimeFormat = formatString;
            DateTimeStyles = DateTimeStyles.AssumeUniversal;
        }

        /// <inheritdoc />
        /// <summary>
        /// This really should not be necessary, but DateTimeStyles does not work when writing JSON. It
        /// keeps assuming local when kind is unspecified. Hence this workaround.
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is DateTime dt)
                base.WriteJson(writer, DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToUniversalTime(), serializer);
            else base.WriteJson(writer, value, serializer);
        }
    }
}