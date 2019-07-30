using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Internal;
using RESTar.ProtocolProviders;
using RESTar.Results;

namespace RESTar.Requests
{
    /// <inheritdoc />
    /// <summary>
    /// Encodes read and writable URI components
    /// </summary>
    public class UriComponents : IUriComponents
    {
        /// <inheritdoc />
        public string ResourceSpecifier { get; }

        /// <inheritdoc />
        public string ViewName { get; }

        /// <summary>
        /// The read and writable conditions list
        /// </summary>
        public List<IUriCondition> Conditions { get; }

        /// <summary>
        /// The read and writable meta-conditions list
        /// </summary>
        public List<IUriCondition> MetaConditions { get; }

        /// <inheritdoc />
        public IProtocolProvider ProtocolProvider { get; }

        /// <inheritdoc />
        public IMacro Macro { get; }

        /// <inheritdoc />
        IReadOnlyCollection<IUriCondition> IUriComponents.Conditions => Conditions;

        /// <inheritdoc />
        IReadOnlyCollection<IUriCondition> IUriComponents.MetaConditions => MetaConditions;

        /// <inheritdoc />
        public UriComponents(string resourceSpecifier, string viewName, IEnumerable<IUriCondition> conditions,
            IEnumerable<IUriCondition> metaConditions, IProtocolProvider protocolProvider, IMacro macro)
        {
            ResourceSpecifier = resourceSpecifier;
            ViewName = viewName;
            Conditions = conditions.ToList();
            MetaConditions = metaConditions.ToList();
            ProtocolProvider = protocolProvider;
            Macro = macro;
        }

        internal UriComponents(IUriComponents existing)
        {
            ResourceSpecifier = existing.ResourceSpecifier;
            ViewName = existing.ViewName;
            Conditions = existing.Conditions.ToList();
            MetaConditions = existing.MetaConditions.ToList();
            ProtocolProvider = existing.ProtocolProvider;
            Macro = existing.Macro;
        }

        /// <inheritdoc />
        public override string ToString() => this.ToUriString();
    }

    /// <inheritdoc />
    /// <summary>
    /// Encodes a URI that is used in a request
    /// </summary>
    internal class URI : IUriComponents
    {
        /// <inheritdoc />
        public string ResourceSpecifier { get; private set; }

        /// <inheritdoc />
        public string ViewName { get; private set; }

        /// <inheritdoc />
        public IReadOnlyCollection<IUriCondition> Conditions { get; private set; }

        /// <inheritdoc />
        public IReadOnlyCollection<IUriCondition> MetaConditions { get; private set; }

        public IMacro Macro { get; private set; }

        internal Exception Error { get; private set; }
        internal bool HasError => Error != null;

        /// <inheritdoc />
        public IProtocolProvider ProtocolProvider { get; private set; }

        internal static URI ParseInternal(string uriString, bool percentCharsEscaped, Context context,
            out CachedProtocolProvider cachedProtocolProvider)
        {
            var uri = new URI();
            if (percentCharsEscaped) uriString = uriString.Replace("%25", "%");
            var groups = Regex.Match(uriString, RegEx.Protocol).Groups;
            var protocolString = groups["proto"].Value;
            if (protocolString.StartsWith("-"))
                protocolString = protocolString.Substring(1);
            var tail = groups["tail"].Value;
            if (!ProtocolController.ProtocolProviders.TryGetValue(protocolString, out cachedProtocolProvider))
            {
                uri.Error = new UnknownProtocol(protocolString);
                return uri;
            }
            uri.ProtocolProvider = cachedProtocolProvider.ProtocolProvider;
            try
            {
                uri.Populate(cachedProtocolProvider.ProtocolProvider.GetUriComponents(tail, context));
            }
            catch (Exception e)
            {
                uri.Error = e;
            }
            return uri;
        }

        private void Populate(IUriComponents components)
        {
            ResourceSpecifier = components.ResourceSpecifier;
            ViewName = components.ViewName;
            Conditions = components.Conditions;
            MetaConditions = components.MetaConditions;
            Macro = components.Macro;
        }

        private URI()
        {
            Conditions = new List<IUriCondition>();
            MetaConditions = new List<IUriCondition>();
            ProtocolProvider = ProtocolController.DefaultProtocolProvider.ProtocolProvider;
        }

        internal URI(string resourceSpecifier, string viewName) : this()
        {
            ResourceSpecifier = resourceSpecifier;
            ViewName = viewName;
        }

        /// <inheritdoc />
        public override string ToString() => this.ToUriString();
    }
}