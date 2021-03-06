﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.Results;
using static System.StringComparison;
using static Newtonsoft.Json.JsonToken;

namespace RESTar
{
    internal class AggregatorTemplateConverter : CustomCreationConverter<Aggregator>
    {
        public override Aggregator Create(Type objectType) => new Aggregator();

        public override bool CanConvert(Type objectType) => objectType == typeof(object) || base.CanConvert(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case Null:
                case StartObject:
                    return base.ReadJson(reader, objectType, existingValue, serializer);
                default: return serializer.Deserialize(reader);
            }
        }
    }

    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="Dictionary{TKey,TValue}" />
    /// <summary>
    /// A resource for creating arbitrary aggregated reports from multiple
    /// internal requests.
    /// </summary>
    [RESTar(Method.GET, Description = description), JsonConverter(typeof(AggregatorTemplateConverter))]
    public class Aggregator : Dictionary<string, object>, ISelector<Aggregator>
    {
        private const string description = "A resource for creating arbitrary aggregated reports from multiple internal requests";

        /// <inheritdoc />
        public IEnumerable<Aggregator> Select(IRequest<Aggregator> request)
        {
            object populator(object node)
            {
                switch (node)
                {
                    case Aggregator obj:
                        obj.ToList().ForEach(pair => obj[pair.Key] = populator(pair.Value));
                        return obj;
                    case JArray array:
                        return array.Select(item => item.ToObject<object>()).Select(populator).ToList();
                    case JObject jobj:
                        return populator(jobj.ToObject<Aggregator>(JsonProvider.Serializer));
                    case string empty when string.IsNullOrWhiteSpace(empty): return empty;
                    case string stringValue:
                        Method method;
                        string uri;
                        if (stringValue.StartsWith("GET ", OrdinalIgnoreCase))
                        {
                            method = Method.GET;
                            uri = stringValue.Substring(4);
                        }
                        else if (stringValue.StartsWith("REPORT ", OrdinalIgnoreCase))
                        {
                            method = Method.REPORT;
                            uri = stringValue.Substring(7);
                        }
                        else return stringValue;
                        if (string.IsNullOrWhiteSpace(uri))
                            throw new Exception($"Invalid URI in aggregator template. Expected relative uri after '{method.ToString()}'.");
                        switch (request.Context.CreateRequest(uri, method, null, request.Headers).Evaluate())
                        {
                            case Error error: throw new Exception($"Could not get source data from '{uri}'. The resource returned: {error}");
                            case NoContent _: return null;
                            case Report report: return report.ReportBody.Count;
                            case IEntities entities: return entities;
                            case var other:
                                throw new Exception($"Unexpected result from {method.ToString()} request inside " +
                                                    $"Aggregator: {other.LogMessage}");
                        }
                    case var other: return other;
                }
            }

            var body = request.GetBody();
            if (!body.HasContent)
                throw new Exception("Missing data source for Aggregator request");
            var template = body.Deserialize<Aggregator>().FirstOrDefault();
            populator(template);
            return new[] {template}.Where(request.Conditions);
        }
    }
}