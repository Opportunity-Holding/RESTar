using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders.NativeJsonProtocol;
using RESTar.Internal.Auth;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using Starcounter;
using Binary = Starcounter.Binary;

namespace RESTar.Admin
{
    /// <inheritdoc cref="IEntity" />
    /// <summary>
    /// A request used for getting data from a local resource, to post in a WebHook
    /// </summary>
    [Database]
    public class CustomPayloadRequest : IEntity
    {
        /// <summary>
        /// The method of the WebHook request. Always GET
        /// </summary>
        public Method Method => Method.GET;

        /// <summary>
        /// The URI of the WebHook request. Must point to a local resource.
        /// </summary>
        public string URI
        {
            get => UriString;
            set
            {
                URIHasChanged = URIHasChanged || UriString != value;
                UriString = value;
            }
        }

        /// <summary>
        /// The headers for this payload request
        /// </summary>
        [JsonConverter(typeof(HeadersConverter<DbHeaders>), true)]
        public DbHeaders Headers { get; }

        /// <summary>
        /// The body of this WebHook request
        /// </summary>
        public JToken Body
        {
            get => HasBody ? JToken.Parse(BodyUTF8) : null;
            set
            {
                if (value != null)
                {
                    BodyBinary = new Binary(Encoding.UTF8.GetBytes(value.ToString()));
                    Headers.ContentType = ContentType.JSON;
                }
                else BodyBinary = default;
            }
        }

        /// <summary>
        /// Should the POST request be aborted if the custom request returns an error?
        /// </summary>
        public bool BreakOnError { get; set; }

        /// <summary>
        /// Should the POST request be aborted if the custom request returns 404: No content?
        /// </summary>
        public bool BreakOnNoContent { get; set; }

        #region Internal

        [Transient] private bool URIHasChanged { get; set; }

        /// <summary>
        /// The underlying storage for URI
        /// </summary>
        [RESTarMember(ignore: true)] public string UriString { get; private set; }

        /// <summary>
        /// The API key to use in requests
        /// </summary>
        [RESTarMember(ignore: true)] public string APIKey { get; private set; }

        /// <summary>
        /// The underlying storage for the body of this WebHook request
        /// </summary>
        [RESTarMember(ignore: true)] public Binary BodyBinary { get; set; }

        #endregion

        /// <inheritdoc />
        public CustomPayloadRequest() => Headers = new DbHeaders();

        /// <inheritdoc />
        public void OnDelete() => Headers.Delete();

        internal bool IsValid(Webhook webhook, out string invalidReason, out string errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrWhiteSpace(URI))
            {
                invalidReason = "Invalid or missing URI for webhook custom request.";
                return false;
            }
            if (Uri.TryCreate(URI, UriKind.Absolute, out _))
            {
                invalidReason = "Invalid URI for webhook custom request. Must point to a local resource.";
                return false;
            }
            if (!Context.Root.UriIsValid(URI, out var error, out var resource, out var components))
            {
                if (!URIHasChanged)
                {
                    errorMessage = $"The URI of the custom request of webhook '{webhook.Label ?? webhook.Id}' is no longer " +
                                   "valid, and has been changed to protect against unsafe behavior. Please change the URI " +
                                   "to a valid local URI to repair the webhook. Previous URI: " + URI;
                    URI = $"/{Resource<Echo>.ResourceSpecifier}/Info={errorMessage.UriEncode()}";
                    invalidReason = null;
                    return true;
                }
                invalidReason = "Invalid Destination URI syntax. Was not an absolute URI, and failed validation " +
                                "as a local URI. " + error.Headers.Info;
                return false;
            }
            URI = components.ToUriString();
            if (RESTarConfig.RequireApiKey)
            {
                switch (Headers.Authorization)
                {
                    case Authenticator.AuthHeaderMask: break;
                    case string _:
                        APIKey = Authenticator.GetAccessRights(Headers)?.ApiKey;
                        break;
                    default:
                        APIKey = null;
                        break;
                }
                var context = Context.Webhook(APIKey, out var authError);
                if (authError != null)
                {
                    Headers.Authorization = null;
                    APIKey = null;
                    invalidReason = "Missing or invalid 'Authorization' header. Webhook custom requests require a " +
                                    "valid API key to be included in the 'Authorization' header.";
                    return false;
                }

                if (!context.MethodIsAllowed(Method, resource, out var methodError))
                {
                    invalidReason = $"Webhook custom request authorization error: {methodError.Headers.Info}";
                    return false;
                }
            }
            invalidReason = null;
            return true;
        }

        internal IRequest CreateRequest(out Results.Error error)
        {
            var context = Context.Webhook(APIKey, out error);
            return error != null ? null : context.CreateRequest(URI, Method, GetBody(), Headers.ToTransient());
        }

        private byte[] GetBody() => HasBody ? BodyBinary.ToArray() : new byte[0];
        private bool HasBody => !BodyBinary.IsNull && BodyBinary.Length > 0;
        private string BodyUTF8 => !HasBody ? "" : Encoding.UTF8.GetString(BodyBinary.ToArray());
    }
}