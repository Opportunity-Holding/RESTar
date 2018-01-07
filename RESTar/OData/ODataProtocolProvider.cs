using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Results.Fail;
using RESTar.Results.Fail.BadRequest;
using RESTar.Results.Success;
using RESTar.Serialization;
using Formatting = Newtonsoft.Json.Formatting;

namespace RESTar.OData
{
    internal static class ODataProtocolProvider
    {
        internal static string MakeRelativeUri(IUriParameters parameters)
        {
            var hasFilter = parameters.Conditions.Count > 0;
            var hasOther = parameters.MetaConditions.Count > 0;

            using (var b = new StringWriter())
            {
                b.Write(parameters.ResourceSpecifier);
                if (hasFilter || hasOther)
                {
                    b.Write('?');
                    if (hasFilter)
                    {
                        b.Write("$filter=");
                        var conds = parameters.Conditions.Select(c => $"{c.Key} {GetOperatorString(c.Operator.OpCode)} {c.ValueLiteral}");
                        b.Write(string.Join(" and ", conds));
                    }

                    if (hasOther)
                    {
                        if (hasFilter) b.Write("&");
                        var conds = parameters.MetaConditions.Select(c =>
                        {
                            switch (c.Key)
                            {
                                case "order_asc": return $"$orderby={c.ValueLiteral} asc";
                                case "order_desc": return $"$orderby={c.ValueLiteral} desc";
                                case "select": return $"$select={c.ValueLiteral}";
                                case "offset": return $"$skip={c.ValueLiteral}";
                                case "limit": return $"$top={c.ValueLiteral}";
                                default: throw new Exception();
                            }
                        });
                        b.Write(string.Join("&", conds));
                    }
                }

                return b.ToString();
            }
        }

        internal static void PopulateUri(URI uri, string query)
        {
            var uriMatch = Regex.Match(query, RegEx.ODataRequestUri);
            if (!uriMatch.Success) throw new InvalidSyntax(ErrorCodes.InvalidUriSyntax, "Check URI syntax");
            var entitySet = uriMatch.Groups["entityset"].Value.TrimStart('/');
            var options = uriMatch.Groups["options"].Value.TrimStart('?');
            if (entitySet.Length != 0)
            {
                if (entitySet == "$metadata")
                    uri.ResourceSpecifier = URI.MetadataResourceSpecifier;
                else uri.ResourceSpecifier = entitySet;
            }
            if (options.Length != 0)
                PopulateFromOptions(uri, options);
        }

        private static void PopulateFromOptions(IUriParameters args, string options)
        {
            foreach (var (optionKey, optionValue) in options.Split('&').Select(option => option.TSplit('=')))
            {
                if (string.IsNullOrWhiteSpace(optionKey))
                    throw new InvalidSyntax(ErrorCodes.InvalidConditionSyntax, "An OData query option key was invalid");
                if (string.IsNullOrWhiteSpace(optionValue))
                    throw new InvalidSyntax(ErrorCodes.InvalidConditionSyntax, $"The OData query option value for {optionKey} was invalid");
                var decodedValue = HttpUtility.UrlDecode(optionValue);
                switch (optionKey)
                {
                    case var system when optionKey[0] == '$':

                        if (!Enum.TryParse(system.Substring(1), out QueryOptions option) || option == QueryOptions.none)
                            throw new FeatureNotImplemented($"Unknown or not implemented query option '{system}'");
                        switch (option)
                        {
                            case QueryOptions.filter:
                                if (Regex.Match(decodedValue, RegEx.UnsupportedODataOperatorRegex) is Match m && m.Success)
                                    throw new FeatureNotImplemented($"Not implemented operator '{m.Value}' in $filter");
                                decodedValue.Replace("(", "").Replace(")", "").Split(" and ").Select(c =>
                                {
                                    var parts = c.Split(' ');
                                    if (parts.Length != 3)
                                        throw new InvalidSyntax(ErrorCodes.InvalidConditionSyntax, "Invalid syntax in $filter query option");
                                    return new UriCondition(parts[0], GetOperator(parts[1]), parts[2]);
                                }).ForEach(args.Conditions.Add);

                                break;

                            case QueryOptions.@orderby:
                                if (decodedValue.Contains(","))
                                    throw new FeatureNotImplemented("Multiple expressions not implemented for $orderby");
                                var (term, order) = decodedValue.TSplit(' ');
                                switch (order)
                                {
                                    case null:
                                    case "":
                                    case "asc":
                                        args.MetaConditions.Add(new UriCondition("order_asc", Operators.EQUALS, term));
                                        break;
                                    case "desc":
                                        args.MetaConditions.Add(new UriCondition("order_desc", Operators.EQUALS, term));
                                        break;
                                    default:
                                        throw new InvalidSyntax(ErrorCodes.InvalidConditionSyntax,
                                            "The OData query option value for $orderby was invalid");
                                }

                                break;

                            case QueryOptions.@select:
                                args.MetaConditions.Add(new UriCondition("select", Operators.EQUALS, decodedValue));
                                break;

                            case QueryOptions.skip:
                                args.MetaConditions.Add(new UriCondition("offset", Operators.EQUALS, decodedValue));
                                break;

                            case QueryOptions.top:
                                args.MetaConditions.Add(new UriCondition("limit", Operators.EQUALS, decodedValue));
                                break;

                            default: throw new ArgumentOutOfRangeException();
                        }

                        break;

                    default:
                        args.MetaConditions.Add(new UriCondition(optionKey, Operators.EQUALS, optionValue));
                        break;
                }
            }
        }

        private static string GetOperatorString(Operators op)
        {
            switch (op)
            {
                case Operators.EQUALS: return "eq";
                case Operators.NOT_EQUALS: return "ne";
                case Operators.LESS_THAN: return "lt";
                case Operators.GREATER_THAN: return "gt";
                case Operators.LESS_THAN_OR_EQUALS: return "le";
                case Operators.GREATER_THAN_OR_EQUALS: return "ge";
                default: throw new FeatureNotImplemented($"Unknown or not implemented operator '{op}' in $filter");
            }
        }

        private static Operators GetOperator(string op)
        {
            switch (op)
            {
                case "eq": return Operators.EQUALS;
                case "ne": return Operators.NOT_EQUALS;
                case "lt": return Operators.LESS_THAN;
                case "gt": return Operators.GREATER_THAN;
                case "le": return Operators.LESS_THAN_OR_EQUALS;
                case "ge": return Operators.GREATER_THAN_OR_EQUALS;
                default: throw new FeatureNotImplemented($"Unknown or not implemented operator '{op}' in $filter");
            }
        }

        internal static bool IsCompliant(Arguments args, out Exception error)
        {
            error = null;
            switch (args.Headers["OData-Version"] ?? args.Headers["OData-MaxVersion"])
            {
                case null:
                case "4.0": return true;
                default:
                    error = new UnsupportedODataVersion();
                    return false;
            }
        }

        private static string GetServiceRoot(Entities entities)
        {
            var origin = entities.Request.Origin;
            var hostAndPath = $"{origin.Host}{Admin.Settings._Uri}-odata";
            return origin.HTTPS ? $"https://{hostAndPath}" : $"http://{hostAndPath}";
        }

        internal static IFinalizedResult FinalizeResult(Result result)
        {
            result.Headers["OData-Version"] = "4.0";
            if (!(result is Entities entities)) return result;

            var contextFragment = $"#{entities.Request.Resource.FullName}";
            var writeMetadata = true;
            switch (entities.Content)
            {
                case IEnumerable<AvailableResource> availableResources:
                    entities.Content = availableResources.Select(ODataServiceEntity.Convert);
                    contextFragment = null;
                    writeMetadata = false;
                    break;
                case IEnumerable<Metadata> metadata:
                    return new MetadataDocument(metadata.First());
            }

            var stream = new MemoryStream();
            using (var swr = new StreamWriter(stream, Serializer.UTF8, 1024, true))
            using (var jwr = new ODataJsonWriter(swr))
            {
                Serializer.Json.Formatting = Formatting.Indented;
                jwr.WritePre();
                jwr.WriteRaw($"\"@odata.context\": \"{GetServiceRoot(entities)}/$metadata{contextFragment}\",");
                jwr.WriteIndentation();
                jwr.WritePropertyName("value");
                Serializer.Json.Serialize(jwr, entities.Content);
                entities.EntityCount = jwr.ObjectsWritten;
                if (writeMetadata)
                {
                    jwr.WriteRaw(",");
                    jwr.WriteIndentation();
                    jwr.WriteRaw($"\"@odata.count\": {entities.EntityCount}");
                    if (entities.IsPaged)
                    {
                        jwr.WriteRaw(",");
                        jwr.WriteIndentation();
                        var pager = entities.GetNextPageLink();
                        jwr.WriteRaw($"\"@odata.nextLink\": {MakeRelativeUri(pager)}");
                    }
                }
                jwr.WritePost();
            }
            if (entities.EntityCount > 0)
            {
                stream.Seek(0, SeekOrigin.Begin);
                result.ContentType = MimeTypes.JSONOData;
                result.Body = stream;
            }else return new NoContent();
            return result;
        }
    }
}