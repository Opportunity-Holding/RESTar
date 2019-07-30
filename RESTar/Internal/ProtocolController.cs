using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Linq;
using RESTar.ProtocolProviders;
using RESTar.Resources;
using RESTar.Results;

namespace RESTar.Internal
{
    internal static class ProtocolController
    {
        internal static IDictionary<string, CachedProtocolProvider> ProtocolProviders { get; private set; }
        internal static CachedProtocolProvider DefaultProtocolProvider { get; private set; }

        internal static CachedProtocolProvider ResolveProtocolProvider(string protocolIdentifier) => protocolIdentifier == null
            ? DefaultProtocolProvider
            : ProtocolProviders.SafeGet(protocolIdentifier) ?? throw new UnknownProtocol(protocolIdentifier);

        private static CachedProtocolProvider GetCachedProtocolProvider(IProtocolProvider provider)
        {
            var cProvider = new CachedProtocolProvider(provider);
            var contentTypeProviders = provider.GetCustomContentTypeProviders()?.ToList();
            contentTypeProviders?.ForEach(contentTypeProvider =>
            {
                if (contentTypeProvider.CanRead)
                    contentTypeProvider.MatchStrings?.ForEach(mimeType => cProvider.InputMimeBindings[mimeType] = contentTypeProvider);
                if (contentTypeProvider.CanWrite)
                    contentTypeProvider.MatchStrings?.ForEach(mimeType => cProvider.OutputMimeBindings[mimeType] = contentTypeProvider);
            });
            switch (provider.ExternalContentTypeProviderSettings)
            {
                case ExternalContentTypeProviderSettings.AllowAll:
                    ContentTypeController.InputContentTypeProviders.Where(p => !cProvider.InputMimeBindings.ContainsKey(p.Key))
                        .ForEach(cProvider.InputMimeBindings.Add);
                    ContentTypeController.OutputContentTypeProviders.Where(p => !cProvider.OutputMimeBindings.ContainsKey(p.Key))
                        .ForEach(cProvider.OutputMimeBindings.Add);
                    break;
                case ExternalContentTypeProviderSettings.AllowInput:
                    ContentTypeController.InputContentTypeProviders.Where(p => !cProvider.InputMimeBindings.ContainsKey(p.Key))
                        .ForEach(cProvider.InputMimeBindings.Add);
                    break;
                case ExternalContentTypeProviderSettings.AllowOutput:
                    ContentTypeController.OutputContentTypeProviders.Where(p => !cProvider.OutputMimeBindings.ContainsKey(p.Key))
                        .ForEach(cProvider.OutputMimeBindings.Add);
                    break;
            }
            return cProvider;
        }

        private static void ValidateProtocolProvider(IProtocolProvider provider)
        {
            if (provider == null)
                throw new InvalidProtocolProviderException("External protocol provider cannot be null");
            if (string.IsNullOrWhiteSpace(provider.ProtocolIdentifier))
                throw new InvalidProtocolProviderException($"Invalid protocol provider '{provider.GetType().RESTarTypeName()}'. " +
                                                           "ProtocolIdentifier cannot be null or whitespace");
            if (!Regex.IsMatch(provider.ProtocolIdentifier, "^[a-zA-Z]+$"))
                throw new InvalidProtocolProviderException($"Invalid protocol provider '{provider.GetType().RESTarTypeName()}'. " +
                                                           "ProtocolIdentifier can only contain letters a-z and A-Z");
            if (provider.ExternalContentTypeProviderSettings == ExternalContentTypeProviderSettings.DontAllow)
            {
                var contentProviders = provider.GetCustomContentTypeProviders()?.ToList();
                if (contentProviders?.Any() != true)
                    throw new InvalidProtocolProviderException($"Invalid protocol provider '{provider.GetType().RESTarTypeName()}'. " +
                                                               "The protocol provider allows no external content type providers " +
                                                               "and does not provide any content type providers of its own.");
                if (contentProviders.All(p => !p.CanRead) && contentProviders.All(p => !p.CanWrite))
                    throw new InvalidProtocolProviderException($"Invalid protocol provider '{provider.GetType().RESTarTypeName()}'. " +
                                                               "The protocol provider allows no external content type providers " +
                                                               "and none of the provided content type providers can read or write.");
            }
        }

        internal static void SetupProtocolProviders(List<IProtocolProvider> protocolProviders)
        {
            ProtocolProviders = new Dictionary<string, CachedProtocolProvider>(StringComparer.OrdinalIgnoreCase);
            protocolProviders = protocolProviders ?? new List<IProtocolProvider>();
            protocolProviders.Add(new DefaultProtocolProvider());
            protocolProviders.ForEach(provider =>
            {
                ValidateProtocolProvider(provider);
                var cachedProvider = GetCachedProtocolProvider(provider);
                if (provider is DefaultProtocolProvider)
                {
                    DefaultProtocolProvider = cachedProvider;
                    ProtocolProviders[""] = cachedProvider;
                }
                var protocolId = provider.ProtocolIdentifier;
                if (ProtocolProviders.TryGetValue(protocolId, out var existing))
                {
                    if (existing.GetType() == provider.GetType())
                        throw new InvalidProtocolProviderException(
                            $"A protocol provider of type '{existing.GetType()}' has already been added");
                    throw new InvalidProtocolProviderException(
                        $"Protocol identifier '{protocolId}' already claimed by a protocol provider of type '{existing.GetType()}'");
                }
                ProtocolProviders[protocolId] = cachedProvider;
            });
        }

        internal static void OnInit() => ProtocolProviders.Values.ForEach(c => c.ProtocolProvider.OnInit());
    }
}