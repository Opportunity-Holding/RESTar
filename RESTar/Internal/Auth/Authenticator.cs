﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Results;

namespace RESTar.Internal.Auth
{
    internal static class Authenticator
    {
        internal static IDictionary<string, AccessRights> ApiKeys { get; private set; }
        internal static void NewState() => ApiKeys = new Dictionary<string, AccessRights>();
        private const string AuthHeaderMask = "*******";

        internal static void RunResourceAuthentication<T>(this IRequest<T> request, IEntityResource<T> resource) where T : class
        {
            var authResults = resource.Authenticate(request);
            if (!authResults.Success)
                throw new FailedResourceAuthentication(authResults.Reason);
        }

        internal static AccessRights GetAccessRights(ref string uri, Headers headers)
        {
            string authorizationHeader;
            if (Regex.Match(uri, RegEx.UriKey) is Match keyMatch && keyMatch.Success)
            {
                var keyGroup = keyMatch.Groups["key"];
                uri = uri.Remove(keyGroup.Index, keyGroup.Length);
                authorizationHeader = $"apikey {HttpUtility.UrlDecode(keyGroup.Value.Substring(1, keyGroup.Length - 2))}";
            }
            else if (headers.Authorization is string header && !string.IsNullOrWhiteSpace(header))
                authorizationHeader = header;
            else return null;
            headers.Authorization = AuthHeaderMask;
            var (method, key) = authorizationHeader.TSplit(' ');
            if (key == null) return null;
            switch (method)
            {
                case var apikey when apikey.EqualsNoCase("apikey"): break;
                case var basic when basic.EqualsNoCase("basic"):
                    key = Encoding.UTF8.GetString(Convert.FromBase64String(key)).Split(":").ElementAtOrDefault(1);
                    if (key == null) return null;
                    break;
                default: return null;
            }
            return ApiKeys.TryGetValue(key.SHA256(), out var _rights) ? _rights : null;
        }
    }
}