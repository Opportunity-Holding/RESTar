using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using RESTar.ContentTypeProviders;
using RESTar.Internal;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Results;
using RESTar.WebSockets;

namespace RESTar.ProtocolProviders
{
    internal class DefaultProtocolUriComponents : IUriComponents
    {
        public string ResourceSpecifier { get; internal set; }
        public string ViewName { get; internal set; }
        public IMacro Macro { get; internal set; }
        IReadOnlyCollection<IUriCondition> IUriComponents.Conditions => Conditions;
        IReadOnlyCollection<IUriCondition> IUriComponents.MetaConditions => MetaConditions;
        public List<IUriCondition> Conditions { get; }
        public List<IUriCondition> MetaConditions { get; }
        public IProtocolProvider ProtocolProvider { get; }

        public DefaultProtocolUriComponents(IProtocolProvider protocolProvider)
        {
            ProtocolProvider = protocolProvider;
            Conditions = new List<IUriCondition>();
            MetaConditions = new List<IUriCondition>();
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Contains the logic for the default RESTar protocol. This protocol is used if no 
    /// protocol indicator is included in the request URI.
    /// </summary>
    internal sealed class DefaultProtocolProvider : IProtocolProvider
    {
        /// <inheritdoc />
        public string ProtocolName { get; } = "RESTar";

        /// <inheritdoc />
        public string ProtocolIdentifier { get; } = "restar";

        /// <inheritdoc />
        public IUriComponents GetUriComponents(string uriString, Context context)
        {
            var uri = new DefaultProtocolUriComponents(this);
            var match = Regex.Match(uriString, RegEx.RESTarRequestUri);
            if (!match.Success) throw new InvalidSyntax(ErrorCodes.InvalidUriSyntax, "Check URI syntax");
            var resourceOrMacro = match.Groups["res"].Value.TrimStart('/');
            var view = match.Groups["view"].Value.TrimStart('-');
            var conditions = match.Groups["cond"].Value.TrimStart('/');
            var metaConditions = match.Groups["meta"].Value.TrimStart('/');

            switch (conditions)
            {
                case var _ when conditions.Length == 0:
                case var _ when conditions == "_": break;
                default:
                    ParseUriConditions(conditions, true).ForEach(uri.Conditions.Add);
                    break;
            }

            switch (metaConditions)
            {
                case var _ when metaConditions.Length == 0:
                case var _ when metaConditions == "_": break;
                default:
                    ParseUriConditions(metaConditions, true).ForEach(uri.MetaConditions.Add);
                    break;
            }

            if (view.Length != 0)
                uri.ViewName = view;

            switch (resourceOrMacro)
            {
                case "":
                case "_":
                    uri.ResourceSpecifier = context.WebSocket?.Status == WebSocketStatus.Waiting
                        ? Shell.TerminalResource.Name
                        : EntityResource<AvailableResource>.ResourceSpecifier;
                    break;
                case var resource when resourceOrMacro[0] != '$':
                    uri.ResourceSpecifier = resource;
                    break;
                case var _macro when _macro.Substring(1) is string _: throw new MacrosNotSupported();
            }

            return uri;
        }

        internal static List<IUriCondition> ParseUriConditions(string conditionsString, bool check = false) => conditionsString
            .Split('&')
            .Select(s => ParseUriCondition(s, check))
            .ToList();

        internal static IUriCondition ParseUriCondition(string conditionString, bool check = false)
        {
            if (check)
            {
                if (string.IsNullOrEmpty(conditionString))
                    throw new InvalidSyntax(ErrorCodes.InvalidConditionSyntax, "Invalid condition syntax");
                conditionString = conditionString.ReplaceFirst("%3E=", ">=", out var replaced);
                if (!replaced) conditionString = conditionString.ReplaceFirst("%3C=", "<=", out replaced);
                if (!replaced) conditionString = conditionString.ReplaceFirst("%3E", ">", out replaced);
                if (!replaced) conditionString = conditionString.ReplaceFirst("%3C", "<", out replaced);
            }
            var match = Regex.Match(conditionString, RegEx.UriCondition);
            if (!match.Success) throw new InvalidSyntax(ErrorCodes.InvalidConditionSyntax, $"Invalid condition syntax at '{conditionString}'");
            var (key, opString, valueLiteral) = (match.Groups["key"].Value, match.Groups["op"].Value, match.Groups["val"].Value);
            if (!Operator.TryParse(opString, out var @operator)) throw new InvalidOperator(conditionString);
            return new UriCondition
            (
                key: key.UriDecode(),
                op: @operator.OpCode,
                valueLiteral: valueLiteral.UriDecode(),
                valueTypeCode: TypeCode.String
            );
        }

        public ExternalContentTypeProviderSettings ExternalContentTypeProviderSettings { get; } = ExternalContentTypeProviderSettings.AllowAll;

        public IEnumerable<IContentTypeProvider> GetCustomContentTypeProviders() => null;

        /// <inheritdoc />
        public string MakeRelativeUri(IUriComponents components) => ToUriString(components);

        private static string ToUriString(IUriComponents components)
        {
            var view = components.ViewName != null ? $"-{components.ViewName}" : null;
            var resource = components.Macro == null ? $"/{components.ResourceSpecifier}{view}" : $"/${components.Macro.Name}";
            var str = new StringBuilder(resource);
            if (components.Conditions.Count > 0)
            {
                str.Append($"/{ToUriString(components.Conditions)}");
                if (components.MetaConditions.Count > 0)
                    str.Append($"/{UnescapeMetaconditions(ToUriString(components.MetaConditions))}");
            }
            else if (components.MetaConditions.Count > 0)
                str.Append($"/_/{UnescapeMetaconditions(ToUriString(components.MetaConditions))}");
            return str.ToString();
        }

        private static string UnescapeMetaconditions(string metaconditionsString) => metaconditionsString
            .Replace("%2C", ",")
            .Replace("%3E", ">")
            .Replace("%24", "$");

        internal static string ToUriString(IEnumerable<IUriCondition> conditions)
        {
            if (conditions == null) return "_";
            var uriString = string.Join("&", conditions.Select(ToUriString));
            if (uriString.Length == 0) return "_";
            return uriString;
        }

        private static string ToUriString(IUriCondition condition)
        {
            if (condition == null) return "";
            var op = ((Operator) condition.Operator).Common;
            var value = ToUriValueString(condition);
            return $"{condition.Key.UriEncode()}{op}{value}";
        }

        private static string ToUriValueString(IUriCondition condition)
        {
            var valueLiteral = condition.ValueLiteral;
            switch (condition.ValueTypeCode)
            {
                case TypeCode.Empty: return "null";
                case TypeCode.Char:
                case TypeCode.String:
                    var encoded = valueLiteral.UriEncode();
                    switch (encoded)
                    {
                        case null: return "null";
                        case "false":
                        case "False":
                        case "FALSE":
                        case "true":
                        case "True":
                        case "TRUE":
                        case var _ when encoded.All(char.IsDigit): return $"'{encoded}'";
                        default: return encoded;
                    }
                default: return valueLiteral;
            }
        }

        /// <inheritdoc />
        public ISerializedResult Serialize(IResult result, IContentTypeProvider contentTypeProvider)
        {
            switch (result)
            {
                case Report report:
                    contentTypeProvider.SerializeCollection(new[] {report.ReportBody}, report.Body, report.Request);
                    return report;

                case Head head:
                    head.Headers.EntityCount = head.EntityCount.ToString();
                    return head;

                case IEntities<object> entities:

                    ISerializedResult SerializeEntities()
                    {
                        var entityCount = contentTypeProvider.SerializeCollection((dynamic) entities, entities.Body, entities.Request);
                        if (entityCount == 0) return new NoContent(entities.Request);
                        entities.Body.Seek(0, SeekOrigin.Begin);
                        entities.Headers.EntityCount = entityCount.ToString();
                        entities.EntityCount = entityCount;
                        if (entities.IsPaged)
                        {
                            var pager = entities.GetNextPageLink();
                            entities.Headers.Pager = pager.ToUriString();
                        }
                        entities.SetContentDisposition(contentTypeProvider.ContentDispositionFileExtension);
                        return entities;
                    }

                    if (entities.Request.Headers.Destination == null)
                        return SerializeEntities();
                    try
                    {
                        var parameters = new HeaderRequestParameters(entities.Request.Headers.Destination);
                        if (parameters.IsInternal)
                        {
                            var internalRequest = entities.Context.CreateRequest(parameters.URI, parameters.Method, null, parameters.Headers);
                            var serializedEntities = SerializeEntities();
                            if (!(serializedEntities is Content content))
                                return serializedEntities;
                            internalRequest.SetBody(content.Body);
                            return internalRequest.Evaluate().Serialize();
                        }
                        var serialized = SerializeEntities();
                        var externalRequest = new HttpRequest(serialized, parameters, serialized.Body);
                        var response = externalRequest.GetResponseAsync().Result
                                       ?? throw new InvalidExternalDestination(externalRequest, "No response");
                        if (response.StatusCode >= HttpStatusCode.BadRequest)
                            throw new InvalidExternalDestination(externalRequest,
                                $"Received {response.StatusCode.ToCode()} - {response.StatusDescription}. {response.Headers.Info}");
                        if (serialized.Headers.AccessControlAllowOrigin is string h)
                            response.Headers.AccessControlAllowOrigin = h;
                        return new ExternalDestinationResult(entities.Request, response);
                    }
                    catch (HttpRequestException re)
                    {
                        throw new InvalidSyntax(ErrorCodes.InvalidDestination, $"{re.Message} in the Destination header");
                    }

                default: return result as ISerializedResult;
            }
        }

        private static void SetSelector<TRequest, TEntity>(IRequest<TRequest> r, IEntities<TEntity> e)
            where TRequest : class where TEntity : class, TRequest => r.Selector = () => e;

        public bool IsCompliant(IRequest request, out string invalidReason)
        {
            invalidReason = null;
            return true;
        }

        public void OnInit()
        {
        }
    }
}