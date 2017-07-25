﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RESTar.Linq;

namespace RESTar
{
    internal class HttpRequest
    {
        internal RESTarMethods Method { get; private set; }
        internal Uri URI { get; private set; }
        internal Dictionary<string, string> Headers { get; }
        internal string Accept;
        internal string ContentType;
        internal bool Internal { get; private set; }
        private HttpRequest() => Headers = new Dictionary<string, string>();
        private static readonly Regex regex = new Regex(@"\[(?<header>.+):[\s]*(?<value>.+)\]");

        internal static HttpRequest Parse(string uriString)
        {
            var r = new HttpRequest();
            uriString.Trim()
                .Split(new[] {' '}, 3)
                .ForEach((part, index) =>
                {
                    switch (index)
                    {
                        case 0:
                            RESTarMethods method;
                            if (!Enum.TryParse(part, true, out method))
                                throw new Exception("Invalid or missing method");
                            r.Method = method;
                            break;
                        case 1:
                            if (!part.StartsWith("/"))
                            {
                                r.Internal = false;
                                if (!Uri.TryCreate(part, UriKind.Absolute, out Uri uri))
                                    throw new Exception($"Invalid uri '{part}'");
                                r.URI = uri;
                            }
                            else
                            {
                                r.Internal = true;
                                if (!Uri.TryCreate(part, UriKind.Relative, out Uri uri))
                                    throw new Exception($"Invalid uri '{part}'");
                                r.URI = uri;
                            }
                            break;
                        case 2:
                            var matches = regex.Matches(part);
                            if (matches.Count == 0) throw new Exception("Invalid header syntax");
                            foreach (Match match in matches)
                            {
                                var header = match.Groups["header"].ToString();
                                var value = match.Groups["value"].ToString();
                                switch (header.ToLower())
                                {
                                    case "accept":
                                        r.Accept = value;
                                        break;
                                    case "content-type":
                                        r.ContentType = value;
                                        break;
                                    default:
                                        r.Headers[header] = value;
                                        break;
                                }
                            }
                            break;
                    }
                });
            return r;
        }
    }
}