using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.ContentTypeProviders;
using RESTar.Meta;
using RESTar.ProtocolProviders;
using RESTar.Requests;
using RESTar.Results;

// ReSharper disable All
#pragma warning disable 1591

namespace RESTar.Palindrom
{
    internal class PalindromUriComponents : IUriComponents
    {
        public string ResourceSpecifier { get; set; }
        public string ViewName { get; }
        public IReadOnlyCollection<IUriCondition> Conditions { get; }
        public IReadOnlyCollection<IUriCondition> MetaConditions { get; }
        public IMacro Macro { get; }
        public IProtocolProvider ProtocolProvider { get; }

        public PalindromUriComponents
        (
            string resourceSpecifier,
            IProtocolProvider protocolProvider,
            params IUriCondition[] conditions
        )
        {
            ResourceSpecifier = resourceSpecifier;
            ViewName = null;
            Conditions = conditions;
            MetaConditions = null;
            Macro = null;
            ProtocolProvider = protocolProvider;
        }
    }

    internal class PalindromResourceNotFound : NotFound
    {
        public PalindromResourceNotFound(string info) : base(ErrorCodes.UnknownResource, info, null) { }
    }

    internal class InvalidPalindromRequestSyntax : InvalidSyntax
    {
        public InvalidPalindromRequestSyntax(string message) : base(ErrorCodes.InvalidUriSyntax, message) { }
    }

    public class PalindromProtocolProvider : IProtocolProvider
    {
        public string ProtocolName { get; }
        public string ProtocolIdentifier { get; }
        public ExternalContentTypeProviderSettings ExternalContentTypeProviderSettings { get; }

        public PalindromProtocolProvider()
        {
            ProtocolName = "Palindrom";
            ProtocolIdentifier = "palindrom";
            ExternalContentTypeProviderSettings = ExternalContentTypeProviderSettings.DontAllow;
        }

        public IEnumerable<IContentTypeProvider> GetCustomContentTypeProviders()
        {
            yield return new JsonProvider();
            yield return new JsonPatchProvider();
        }

        private UriCondition GetSessionIdCondition(string id) => new UriCondition
        (
            key: nameof(Session.ID),
            op: Operators.EQUALS,
            valueLiteral: id,
            valueTypeCode: TypeCode.String
        );

        public IUriComponents GetUriComponents(string uriString, Context context)
        {
            var parts = uriString.Split("/");
            var sessionId = parts.ElementAtOrDefault(1) ?? throw new InvalidPalindromRequestSyntax("Missing session ID");
            var sessionIdCondition = GetSessionIdCondition(sessionId);

            if (context.HasWebSocket) // we're going for the palindrom terminal
            {
                return new PalindromUriComponents
                (
                    resourceSpecifier: Resource<Session>.ResourceSpecifier,
                    protocolProvider: this,
                    conditions: sessionIdCondition
                );
            }

            // we're going for some palindrom entity resource, like reconnect or patch

            switch (parts.ElementAtOrDefault(2))
            {
                case "":
                case null:
                    return new PalindromUriComponents
                    (
                        resourceSpecifier: Resource<SessionRoot>.ResourceSpecifier,
                        protocolProvider: this,
                        sessionIdCondition
                    );

                case "reconnect":
                    return new PalindromUriComponents
                    (
                        resourceSpecifier: Resource<SessionReconnect>.ResourceSpecifier,
                        protocolProvider: this,
                        sessionIdCondition
                    );
                default:

                    throw new Exception();
            }
        }

        public bool IsCompliant(IRequest request, out string invalidReason)
        {
            invalidReason = null;
            return true;
        }

        public string MakeRelativeUri(IUriComponents components)
        {
            var sessionId = components.Conditions.FirstOrDefault(c => c.Key == "ID").ValueLiteral;
            switch (components.ResourceSpecifier)
            {
                case var root when root == Resource<SessionRoot>.ResourceSpecifier:
                case var session when session == Resource<Session>.ResourceSpecifier:
                    return "/" + sessionId;
                case var reconnect when reconnect == Resource<SessionReconnect>.ResourceSpecifier:
                    return "/" + sessionId + "/reconnect";

                default: throw new Exception();
            }
        }

        public ISerializedResult Serialize(IResult result, IContentTypeProvider contentTypeProvider)
        {
            if (result is IEntities<object> entities) // a request for session root, that returned success
            {
                var first = true;
                object firstObject = null;
                foreach (var entity in entities)
                {
                    if (!first)
                        throw new AmbiguousMatchException("Expected single entity when serializing Palindrom data");
                    firstObject = entity;
                    first = false;
                }

                Providers.Json.SerializeStream(firstObject).WriteTo(entities.Body);
                return entities;
            }

            throw new Exception();
        }

        public void OnInit() { }
    }
}