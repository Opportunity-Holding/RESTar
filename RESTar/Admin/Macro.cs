using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders;
using RESTar.ContentTypeProviders.NativeJsonProtocol;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.ProtocolProviders;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using Starcounter;
using static System.StringComparison;

namespace RESTar.Admin
{
    /// <inheritdoc cref="IValidator{T}" />
    /// <summary>
    /// A resource for all macros available for this RESTar instance
    /// </summary>
    [Database, RESTar(Description = description)]
    public class Macro : IMacro, IValidator<Macro>, IUriComponents
    {
        private const string description = "Contains all available macros for this RESTar instance";
        internal const string All = "SELECT t FROM RESTar.Admin.Macro t";
        internal const string ByName = All + " WHERE t.Name =?";

        /// <inheritdoc />
        public string Name { get; }

        [Transient] private bool UriChanged { get; set; }

        /// <summary>
        /// The underlying storage for URIs
        /// </summary>
        [RESTarMember(ignore: true)] public string UriString { get; private set; }

        /// <summary>
        /// The URI of the macro
        /// </summary>
        public string Uri
        {
            get => UriString;
            set
            {
                UriChanged = UriChanged || UriString != value;
                UriString = value;
            }
        }

        /// <summary>
        /// The body of the macro
        /// </summary>
        [RESTarMember(replaceOnUpdate: true)] public JToken Body
        {
            get => HasBody ? JToken.Parse(Encoding.UTF8.GetString(BodyBinary.ToArray())) : null;
            set => BodyBinary = value != null ? new Binary(Encoding.UTF8.GetBytes(Providers.Json.Serialize(value, Formatting.None))) : default;
        }

        /// <summary>
        /// The headers of the macro
        /// </summary>
        [JsonConverter(typeof(HeadersConverter<DbHeaders>))]
        public DbHeaders Headers { get; }

        /// <inheritdoc />
        public bool OverwriteBody { get; set; }

        /// <inheritdoc />
        public bool OverwriteHeaders { get; set; }

        /// <summary>
        /// The underlying storage for Body
        /// </summary>
        [RESTarMember(ignore: true)] public Binary BodyBinary { get; set; }

        /// <inheritdoc />
        [RESTarMember(ignore: true)] public bool HasBody => !BodyBinary.IsNull && BodyBinary.Length > 0;

        /// <inheritdoc />
        [JsonConstructor]
        public Macro(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Missing or invalid macro name");
            if (!Db.SQL<Macro>(ByName, name).All(macro => macro.Equals(this)))
                throw new ArgumentException($"Invalid macro name. '{name}' is already used by another macro");
            Name = name;
            Headers = new DbHeaders();
        }

        internal bool CheckIfValid() => IsValid(this, out _);

        internal static string GetErrorURI(string macroName, string invalidUri, string invalidReason)
        {
            var info = $"The URI of RESTar macro '{macroName}' is no longer valid, and has been replaced to protect " +
                       $"against unsafe behavior. Please update the '{nameof(Uri)}' property to a valid RESTar URI to " +
                       "repair the macro.";
            return $"/{Resource<Echo>.ResourceSpecifier}/" +
                   $"Info={info.UriEncode()}&" +
                   $"InvalidUri={invalidUri.UriEncode()}&" +
                   $"InvalidReason={invalidReason.UriEncode()}";
        }

        /// <inheritdoc />
        /// <summary>
        /// Validates the macro
        /// </summary>
        public bool IsValid(Macro macro, out string invalidReason)
        {
            if (string.IsNullOrWhiteSpace(macro.Uri))
            {
                invalidReason = "Invalid or missing Uri in macro";
                return false;
            }
            if (macro.Uri.IndexOf($"${macro.Name}", OrdinalIgnoreCase) >= 0)
            {
                invalidReason = "Invalid macro Uri: Cannot contain self-references";
                return false;
            }
            if (macro.Headers.Authorization != null)
            {
                invalidReason = "Macro headers cannot contain the 'Authorization' header. If API keys are " +
                                "required, they are expected in each request invoking the macro.";
                return false;
            }
            if (macro.MakeUriComponents(out var error) == null)
            {
                if (!macro.UriChanged)
                {
                    macro.Uri = GetErrorURI(macro.Name, macro.Uri, error.Headers.Info);
                    invalidReason = null;
                    return true;
                }
                invalidReason = "Invalid macro Uri: " + error.Headers.Info;
                return false;
            }
            invalidReason = null;
            return true;
        }

        #region IUriComponents

//        [Transient] private IUriComponents UriComponentsStore { get; set; }

        private IUriComponents MakeUriComponents(out Results.Error error)
        {
            if (Context.Root.UriIsValid(Uri, out error, out _, out var components))
            {
                Db.TransactAsync(() => Uri = components.ToUriString());
                return components;
            }
            return null;
        }

        /// <summary>
        /// If asked for through IUriComponents API, require IsValid for non null value
        /// </summary>
        private IUriComponents UriComponents => MakeUriComponents(out _); // UriComponentsStore ?? (UriComponentsStore = MakeUriComponents(out _));

        IProtocolProvider IUriComponents.ProtocolProvider => ProtocolController.DefaultProtocolProvider.ProtocolProvider;
        string IUriComponents.ResourceSpecifier => UriComponents?.ResourceSpecifier;
        string IUriComponents.ViewName => UriComponents?.ViewName;
        IReadOnlyCollection<IUriCondition> IUriComponents.Conditions => UriComponents?.Conditions;
        IReadOnlyCollection<IUriCondition> IUriComponents.MetaConditions => UriComponents?.MetaConditions;

        #endregion

        #region IMacro

        IMacro IUriComponents.Macro => this;
        byte[] IMacro.Body => HasBody ? BodyBinary.ToArray() : new byte[0];
        ContentType IMacro.ContentType => ContentType.JSON;
        IHeaders IMacro.Headers => Headers;

        #endregion

        internal static void Check() => Db.TransactAsync(() => Db.SQL<Macro>(All).ForEach(m => m.CheckIfValid()));
    }
}