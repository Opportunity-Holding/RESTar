using System;
using System.Collections.Generic;
using System.Linq;

namespace RESTar.Requests
{
    internal static class HeadersExtensions
    {
        internal static IEnumerable<KeyValuePair<string, string>> GetCustom(this IHeadersInternal headers, HashSet<string> whitelist = null)
        {
            return headers.Where(pair => whitelist?.Contains(pair.Key) == true || IsCustomHeaderName(pair.Key));
        }

        internal static HashSet<string> NonCustomHeaders { get; }

        static HeadersExtensions() => NonCustomHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "host", "authorization", "connection", "upgrade", "restar-metadata", "sec-websocket-version", "sec-websocket-key",
            "sec-websocket-extensions"
        };

        internal static bool IsCustomHeaderName(this string key) => !NonCustomHeaders.Contains(key);


        internal static string _Get(this IHeadersInternal headers, string key)
        {
            switch (key)
            {
                case var _ when key.EqualsNoCase(nameof(IHeaders.Accept)): return headers.Accept?.ToString();
                case var _ when key.EqualsNoCase("Content-Type"): return headers.ContentType?.ToString();
                case var _ when key.EqualsNoCase(nameof(IHeaders.Source)): return headers.Source;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Destination)): return headers.Destination;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Authorization)): return headers.Authorization;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Origin)): return headers.Origin;
                case var _ when headers.TryGetCustomHeader(key, out var value): return value;
                default: return default;
            }
        }

        internal static void _Set(this IHeadersInternal headers, string key, string value)
        {
            switch (key)
            {
                case var _ when key.EqualsNoCase(nameof(IHeaders.Accept)):
                    if (!string.IsNullOrWhiteSpace(value))
                        headers.Accept = ContentType.ParseMany(value);
                    break;
                case var _ when key.EqualsNoCase("Content-Type"):
                    if (!string.IsNullOrWhiteSpace(value))
                        headers.ContentType = ContentType.Parse(value);
                    break;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Source)):
                    headers.Source = value;
                    break;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Destination)):
                    headers.Destination = value;
                    break;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Authorization)):
                    headers.Authorization = value;
                    break;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Origin)):
                    headers.Origin = value;
                    break;
                default:
                    headers.SetCustomHeader(key, value);
                    break;
            }
        }

        internal static bool _Contains(this IHeadersInternal headers, KeyValuePair<string, string> item)
        {
            switch (item.Key)
            {
                case var _ when item.Key.EqualsNoCase(nameof(IHeaders.Accept)): return headers.Accept?.ToString().EqualsNoCase(item.Value) == true;
                case var _ when item.Key.EqualsNoCase("Content-Type"): return headers.ContentType?.ToString().EqualsNoCase(item.Value) == true;
                case var _ when item.Key.EqualsNoCase(nameof(IHeaders.Source)): return headers.Source.EqualsNoCase(item.Value);
                case var _ when item.Key.EqualsNoCase(nameof(IHeaders.Destination)): return headers.Destination.EqualsNoCase(item.Value);
                case var _ when item.Key.EqualsNoCase(nameof(IHeaders.Authorization)): return headers.Authorization.EqualsNoCase(item.Value);
                case var _ when item.Key.EqualsNoCase(nameof(IHeaders.Origin)): return headers.Origin.EqualsNoCase(item.Value);
                default: return headers.ContainsCustomHeader(item);
            }
        }

        internal static bool _ContainsKey(this IHeadersInternal headers, string key)
        {
            switch (key)
            {
                case var _ when key.EqualsNoCase(nameof(IHeaders.Accept)): return headers.Accept != null;
                case var _ when key.EqualsNoCase("Content-Type"): return headers.ContentType != null;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Source)): return headers.Source != null;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Destination)): return headers.Destination != null;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Authorization)): return headers.Authorization != null;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Origin)): return headers.Origin != null;
                default: return headers.ContainsCustomHeaderName(key);
            }
        }

        internal static bool _Remove(this IHeadersInternal headers, string key)
        {
            switch (key)
            {
                case var _ when key.EqualsNoCase(nameof(IHeaders.Accept)):
                    headers.Accept = null;
                    return true;
                case var _ when key.EqualsNoCase("Content-Type"):
                    headers.ContentType = null;
                    return true;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Source)):
                    headers.Source = null;
                    return true;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Destination)):
                    headers.Destination = null;
                    return true;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Authorization)):
                    headers.Authorization = null;
                    return true;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Origin)):
                    headers.Origin = null;
                    return true;
                default: return headers.RemoveCustomHeader(key);
            }
        }

        internal static bool _Remove(this IHeadersInternal headers, KeyValuePair<string, string> item)
        {
            switch (item.Key)
            {
                case var _ when item.Key.EqualsNoCase(nameof(IHeaders.Accept)) && item.Value == headers.Accept?.ToString():
                    headers.Accept = null;
                    return true;
                case var _ when item.Key.EqualsNoCase("Content-Type") && item.Value == headers.ContentType?.ToString():
                    headers.ContentType = null;
                    return true;
                case var _ when item.Key.EqualsNoCase(nameof(IHeaders.Source)) && item.Value == headers.Source:
                    headers.Source = null;
                    return true;
                case var _ when item.Key.EqualsNoCase(nameof(IHeaders.Destination)) && item.Value == headers.Destination:
                    headers.Destination = null;
                    return true;
                case var _ when item.Key.EqualsNoCase(nameof(IHeaders.Authorization)) && item.Value == headers.Authorization:
                    headers.Authorization = null;
                    return true;
                case var _ when item.Key.EqualsNoCase(nameof(IHeaders.Origin)) && item.Value == headers.Origin:
                    headers.Origin = null;
                    return true;
                default: return headers.RemoveCustomHeader(item);
            }
        }

        internal static bool _TryGetValue(this IHeadersInternal headers, string key, out string value)
        {
            switch (key)
            {
                case var _ when key.EqualsNoCase(nameof(IHeaders.Accept)):
                    value = headers.Accept?.ToString();
                    return value != null;
                case var _ when key.EqualsNoCase("Content-Type"):
                    value = headers.ContentType?.ToString();
                    return value != null;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Source)):
                    value = headers.Source;
                    return value != null;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Destination)):
                    value = headers.Destination;
                    return value != null;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Authorization)):
                    value = headers.Authorization;
                    return value != null;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Origin)):
                    value = headers.Origin;
                    return value != null;
                default: return headers.TryGetCustomHeader(key, out value);
            }
        }

        internal static void _CopyTo(this IHeadersInternal headers, KeyValuePair<string, string>[] array, int arrayIndex) =>
            headers.ToList().CopyTo(array, arrayIndex);

        internal static ICollection<string> _Keys(this IHeadersInternal headers) => headers.Select(kvp => kvp.Key).ToList();
        internal static ICollection<string> _Values(this IHeadersInternal headers) => headers.Select(kvp => kvp.Value).ToList();
    }
}