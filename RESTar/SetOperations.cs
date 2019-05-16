using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.Results;
using static System.StringComparison;
using static RESTar.Method;
using JTokens = System.Collections.Generic.IEnumerable<Newtonsoft.Json.Linq.JToken>;

#pragma warning disable 1591

namespace RESTar
{
    /// <inheritdoc cref="JObject" />
    /// <inheritdoc cref="ISelector{T}" />
    /// <summary>
    /// The SetOperations resource can perform advanced operations on entities in one
    /// or more RESTar resources. See the RESTar Specification for details.
    /// </summary>
    [RESTar(GET, Singleton = true, Description = description, AllowDynamicConditions = true)]
    public class SetOperations : JObject, ISelector<SetOperations>
    {
        private const string description = "The SetOperations resource can perform advanced operations " +
                                           "on entities in one or more RESTar resources. See the RESTar " +
                                           "Specification for details.";

        public SetOperations() { }
        private SetOperations(JObject other) : base(other) { }

        /// <inheritdoc />
        public IEnumerable<SetOperations> Select(IRequest<SetOperations> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var body = request.GetBody();
            if (!body.HasContent)
                throw new Exception("Missing data source for SetOperations request");
            var jobject = body.Deserialize<JObject>().FirstOrDefault();

            JTokens recursor(JToken token)
            {
                switch (token)
                {
                    case JValue value when value.Type == JTokenType.String:
                        var argument = value.Value<string>();
                        switch (argument?.FirstOrDefault())
                        {
                            case null: throw new Exception("Invalid operation expression. Expected string, found null");
                            case default(char): throw new Exception("Operation expressions cannot be empty strings");
                            case '[': return JArray.Parse(argument);
                            case '/':
                                switch (request.Context.CreateRequest(argument).Evaluate())
                                {
                                    case NoContent _: return new JArray();
                                    case IEntities entities: return JArray.FromObject(entities, JsonProvider.Serializer);
                                    case var other: throw new Exception($"Could not get source data from '{argument}'. {other.LogMessage}");
                                }
                            default:
                                throw new Exception($"Invalid string '{argument}'. Must be a relative REST request URI " +
                                                    "beginning with '/<resource locator>' or a JSON array.");
                        }
                    case JObject obj when obj.Count == 1 && obj.First is JProperty prop && prop.Value is JArray arr:
                        switch (prop.Name.ToLower())
                        {
                            case "distinct":
                                if (arr.Count != 1)
                                    throw new Exception("Distinct takes one and only one set as operand");
                                return Distinct(recursor(arr.First));
                            case "except":
                                if (arr.Count != 2)
                                    throw new Exception("Except takes two and only two argument sets as operands");
                                return Except(recursor(arr[0]), recursor(arr[1]));
                            case "intersect":
                                if (arr.Count < 2)
                                    throw new Exception("Intersect takes at least two sets as operands");
                                return Intersect(arr.Select(recursor).ToArray());
                            case "union":
                                if (arr.Count < 2)
                                    throw new Exception("Union takes at least two sets as operands");
                                return Union(arr.Select(recursor).ToArray());
                            case "map":
                                if (arr.Count != 2)
                                    throw new Exception("Map takes two and only two arguments");
                                return Map(recursor(arr[0]), (string) arr[1], request);
                            default:
                                throw new ArgumentOutOfRangeException(
                                    $"Unknown operation '{prop.Name}'. Avaliable operations: distinct, except, " +
                                    "intersect, union.");
                        }
                    default:
                        throw new ArgumentException(
                            $"Invalid type '{token.Type}' in operations tree. Expected object with single array " +
                            "property, or string");
                }
            }

            return recursor(jobject).Select(token =>
            {
                switch (token)
                {
                    case JValue value: return new SetOperations(new JObject(new JProperty("Value", value)));
                    case JObject @object: return new SetOperations(@object);
                    default: throw new Exception("Invalid entity type in set operation");
                }
            }).Where(request.Conditions);
        }

        private static JTokens Distinct(JTokens array) => array?.Distinct(EqualityComparer);
        private static JTokens Intersect(params JTokens[] arrays) => Checked(arrays).Aggregate((x, y) => x.Intersect(y, EqualityComparer));
        private static JTokens Union(params JTokens[] arrays) => Checked(arrays).Aggregate((x, y) => x.Union(y, EqualityComparer));
        private static JTokens Except(params JTokens[] arrays) => Checked(arrays).Aggregate((x, y) => x.Except(y, EqualityComparer));

        private static JTokens Map(JTokens set, string mapper, IRequest request)
        {
            if (set == null) throw new ArgumentException(nameof(set));
            if (string.IsNullOrEmpty(mapper)) throw new ArgumentException(nameof(mapper));

            var mapped = new HashSet<JToken>(EqualityComparer);
            var index = 0;
            var keys = new List<string>();
            mapper = Regex.Replace(mapper, RegEx.MapMacro, match =>
            {
                keys.Add(match.Groups["value"].Value);
                return $"{{{index++}}}";
            });
            var argumentCount = keys.Count;
            var valueBuffer = new object[argumentCount];

            Distinct(set).ForEach(item =>
            {
                var obj = item as JObject ?? throw new Exception("JSON syntax error in map set. Set must be of objects");
                var localMapper = mapper;
                for (var i = 0; i < argumentCount; i += 1)
                {
                    string value;
                    switch (obj.GetValue(keys[i], OrdinalIgnoreCase))
                    {
                        case JValue jvalue when jvalue.Type == JTokenType.Null:
                        case null:
                            value = "null";
                            break;
                        case JValue jvalue when jvalue.Type == JTokenType.Date:
                            value = jvalue.Value<DateTime>().ToString("O");
                            break;
                        case JValue jvalue when jvalue.Value<string>() is string stringValue:
                            value = stringValue == "" ? "\"\"" : stringValue;
                            break;
                        default: return;
                    }
                    valueBuffer[i] = value.UriEncode();
                }
                localMapper = string.Format(localMapper, valueBuffer);
                switch (request.Context.CreateRequest(localMapper).Evaluate())
                {
                    case NoContent _: break;
                    case IEntities<object> entities:
                        mapped.UnionWith(entities.Select(e => e.ToJObject()));
                        break;
                    case var other:
                        throw new Exception($"Could not get source data from '{localMapper}'. {other.LogMessage}");
                }
            });
            return mapped;
        }

        private static JTokens[] Checked(JTokens[] arrays) => arrays == null || arrays.Length < 2 || arrays.Any(a => a == null)
            ? throw new ArgumentException(nameof(arrays))
            : arrays;
    }
}