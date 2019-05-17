using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RESTar.ContentTypeProviders.NativeJsonProtocol;
using RESTar.Internal.Auth;
using RESTar.Internal.Logging;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Meta.Internal;
using RESTar.ProtocolProviders;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.Results;
using Starcounter;

namespace RESTar.Admin
{
    /// <inheritdoc cref="IValidator{T}" />
    /// <inheritdoc cref="IEntity" />
    /// <summary>
    /// Webhooks are used to generate POST request callbacks to external URIs when events are triggered.
    /// </summary>
    [RESTar, Database]
    public class Webhook : IValidator<Webhook>, IEntity
    {
        internal const string All = "SELECT t FROM RESTar.Admin.Webhook t";
        internal const string ByEventName = All + " WHERE t.EventName =?";
        internal const string IdHeader = "RESTar-webhook-id";
        private static IDictionary<ulong, object> ConditionCache { get; }
        private static HttpClient HttpClient { get; }

        static Webhook()
        {
            HttpClient = new HttpClient();
            ConditionCache = new ConcurrentDictionary<ulong, object>();
        }

        /// <summary>
        /// A unique ID for this Webhook
        /// </summary>
        public string Id => this.GetObjectID();

        /// <summary>
        /// A descriptive label for this webhook
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The method to use in the webhook request
        /// </summary>
        public Method Method { get; set; }

        /// <summary>
        /// The underlying storage for Destination
        /// </summary>
        [RESTarMember(ignore: true)] public string DestinationString { get; private set; }

        /// <summary>
        /// The destination URL for this webhook
        /// </summary>
        public string Destination
        {
            get => DestinationString;
            set
            {
                DestinationHasChanged = DestinationHasChanged || DestinationString != value;
                DestinationString = value;
            }
        }

        /// <summary>
        /// Underlying storage for EventSelector
        /// </summary>
        [RESTarMember(ignore: true)] public string _EventSelector { get; private set; }

        /// <summary>
        /// The event selector used to define the events that trigger this webhook
        /// </summary>
        public string EventSelector
        {
            get => _EventSelector;
            set
            {
                EventHasChanged = EventHasChanged || _EventSelector != value;
                if (EventHasChanged)
                    EventName = null;
                _EventSelector = value;
            }
        }

        /// <summary>
        /// The event name of this webhook
        /// </summary>
        [RESTarMember(ignore: true)] public string EventName { get; private set; }

        /// <summary>
        /// Custom headers included in the POST request
        /// </summary>
        [JsonConverter(typeof(HeadersConverter<DbHeaders>), true)]
        public DbHeaders Headers { get; }

        /// <summary>
        /// Is this webhook currently paused?
        /// </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// A custom request to use to generate the body of the webhook's POST requests
        /// </summary>
        [RESTarMember(replaceOnUpdate: true)] public CustomPayloadRequest CustomPayloadRequest
        {
            get => CustomPayloadRequestStore;
            set
            {
                if (value == null || !value.Equals(CustomPayloadRequestStore))
                    CustomPayloadRequestStore?.Delete();
                CustomPayloadRequestStore = value;
            }
        }

        #region Internal

        /// <summary>
        /// The underlying storage for CustomPayloadRequest
        /// </summary>
        [RESTarMember(ignore: true)] public CustomPayloadRequest CustomPayloadRequestStore { get; private set; }

        [Transient] private bool DestinationHasChanged { get; set; }

        /// <summary>
        /// Does the destination refer to a local resource?
        /// </summary>
        [RESTarMember(ignore: true)] public bool DestinationIsLocal { get; private set; }

        /// <summary>
        /// The API key to use in requests to local destination resources
        /// </summary>
        [RESTarMember(ignore: true)] public string LocalDestinationAPIKey { get; private set; }

        [Transient] private bool EventHasChanged { get; set; }

        /// <summary>
        /// The error message, if any, of this webhook
        /// </summary>
        [RESTarMember(hideIfNull: true)] public string ErrorMessage { get; private set; }

        #endregion

        /// <inheritdoc />
        public Webhook()
        {
            Method = Method.POST;
            Headers = new DbHeaders();
        }

        /// <inheritdoc />
        public void OnDelete()
        {
            Headers.Delete();
            CustomPayloadRequest?.Delete();
        }

        private bool CheckIfValid() => IsValid(this, out _);

        private bool TryParseConditions<TEvent, TPayload>(IEventResource<TEvent, TPayload> resource, string conds, out string formatted,
            out Results.Error error) where TEvent : Event<TPayload> where TPayload : class
        {
            if (!Condition<TPayload>.TryParse(DefaultProtocolProvider.ParseUriConditions(conds), resource.PayloadTarget, out var parsed, out error))
            {
                formatted = null;
                return false;
            }
            formatted = DefaultProtocolProvider.ToUriString(parsed);
            ConditionCache[this.GetObjectNo()] = parsed;
            return true;
        }

        /// <inheritdoc />
        public bool IsValid(Webhook entity, out string invalidReason)
        {
            #region Destination

            if (string.IsNullOrWhiteSpace(entity.Destination))
            {
                invalidReason = "Invalid or missing Destination in webhook";
                return false;
            }
            if (Uri.TryCreate(entity.Destination, UriKind.Absolute, out var _uri))
            {
                entity.Destination = _uri.ToString();
                if (entity.DestinationIsLocal)
                    entity.Headers.Authorization = null;
                entity.DestinationIsLocal = false;
            }
            else
            {
                if (!Context.Root.UriIsValid(entity.Destination, out var error, out var resource, out var components))
                {
                    if (!entity.DestinationHasChanged)
                    {
                        entity.ErrorMessage =
                            $"The Destination URL of webhook '{entity.Label ?? entity.Id}' is no longer valid, and has been changed to " +
                            "protect against unsafe behavior. Please change the destination to a valid local URI " +
                            "to repair the webhook. Previous Destination: " + entity.Destination;
                        entity.Destination = $"/{Resource<Blank>.ResourceSpecifier}";
                        entity.IsPaused = true;
                        invalidReason = null;
                        return true;
                    }
                    entity.ErrorMessage = null;
                    invalidReason = "Invalid Destination URI syntax. Was not an absolute URI, and failed validation " +
                                    "as a local URI. " + error.Headers.Info;
                    return false;
                }
                entity.Destination = components.ToUriString();
                entity.DestinationIsLocal = true;
                if (RESTarConfig.RequireApiKey)
                {
                    switch (entity.Headers.Authorization)
                    {
                        case Authenticator.AuthHeaderMask: break;
                        case string _:
                            entity.LocalDestinationAPIKey = Authenticator.GetAccessRights(entity.Headers)?.ApiKey;
                            break;
                        default:
                            entity.LocalDestinationAPIKey = null;
                            break;
                    }
                    var context = Context.Webhook(entity.LocalDestinationAPIKey, out var authError);
                    if (authError != null)
                    {
                        entity.Headers.Authorization = null;
                        entity.LocalDestinationAPIKey = null;
                        invalidReason = "Missing or invalid 'Authorization' header. Webhooks with local destinations require a " +
                                        "valid API key to be included in the 'Authorization' header.";
                        return false;
                    }

                    if (!context.MethodIsAllowed(entity.Method, resource, out var methodError))
                    {
                        invalidReason = $"Authorization error: {methodError.Headers.Info}";
                        return false;
                    }
                }
            }

            #endregion

            #region Event

            if (string.IsNullOrWhiteSpace(entity.EventSelector))
            {
                invalidReason = "Invalid or missing Event in webhook";
                return false;
            }

            bool EventSelectorIsValid(string eventSelector, out string _formatted, out IEventResource _event, out Results.Error _error)
            {
                (_formatted, _event, _error) = (null, null, null);
                var match = Regex.Match(eventSelector, RegEx.EventSelector);
                if (!match.Success)
                {
                    _error = new InvalidSyntax(ErrorCodes.InvalidEventSelector, "Invalid event selector syntax");
                    return false;
                }
                if (!Meta.Resource.TryFind(match.Groups["event"].Value, out _event, out _error))
                    return false;
                var conds = match.Groups["cond"].Value;
                var formattedConds = default(string);
                if (conds != "" && !entity.TryParseConditions((dynamic) _event, conds.Substring(1), out formattedConds, out _error))
                    return false;
                _formatted = formattedConds != null ? $"/{_event.Name}/{formattedConds}" : $"/{_event.Name}";
                return true;
            }

            if (EventSelectorIsValid(entity.EventSelector, out var formatted, out var @event, out var eventError))
            {
                entity.EventSelector = formatted;
                entity.EventName = @event.Name;
            }
            else
            {
                if (!entity.EventHasChanged)
                {
                    entity.ErrorMessage =
                        $"The Event selector '{entity.EventSelector}' of webhook '{entity.Label ?? entity.Id}' is no longer valid. Please change the 'Event' " +
                        "property to a valid event to repair the webhook. To list available events, use the 'RESTar.Event' resource.";
                    entity.IsPaused = true;
                    invalidReason = null;
                    return true;
                }
                invalidReason = eventError.Headers.Info;
                return false;
            }

            #endregion

            #region Custom request

            if (entity.CustomPayloadRequest != null)
            {
                if (!entity.CustomPayloadRequest.IsValid(entity, out var _invalidReason, out var errorMessage))
                {
                    invalidReason = _invalidReason;
                    return false;
                }
                if (errorMessage != null)
                {
                    entity.ErrorMessage = errorMessage;
                    invalidReason = null;
                    return true;
                }
            }

            #endregion

            entity.ErrorMessage = null;
            invalidReason = null;
            return true;
        }

        private IRequest GetLocalRequest(Stream body, ContentType contentType)
        {
            var context = Context.Webhook(LocalDestinationAPIKey, out var error);
            if (error != null) throw error;
            var request = context.CreateRequest(Destination, Method, headers: Headers.ToTransient());
            if (request.IsValid)
                request.SetBody(body, contentType);
            return request;
        }

        private HttpRequestMessage GetGlobalRequest(Stream body, ContentType? contentType)
        {
            var message = new HttpRequestMessage(new HttpMethod(Method.ToString()), Destination);
            if (body != null)
            {
                var content = new StreamContent(body);
                content.Headers.ContentType = contentType.GetValueOrDefault();
                message.Content = content;
            }
            foreach (var header in Headers)
                message.Headers.TryAddWithoutValidation(header.Key, header.Value);
            if (Headers.Authorization != null)
                message.Headers.Authorization = AuthenticationHeaderValue.Parse(Headers.Authorization);
            message.Headers.Add(IdHeader, Id);
            return message;
        }

        internal async Task Post<TPayload>(IEventInternal<TPayload> @event) where TPayload : class
        {
            var @break = IsPaused || ConditionCache.TryGetValue(this.GetObjectNo(), out var c) && c is List<Condition<TPayload>> conds &&
                         !conds.AllHoldFor(@event.Payload);
            if (@break) return;

            Stream body;
            long contentLength;
            var contentType = Headers.ContentType ?? ContentType.JSON;

            if (CustomPayloadRequest == null)
                (body, contentType, contentLength) = @event.Payload.ToStream(@event.NativeContentType ?? contentType);
            else
            {
                using (var request = CustomPayloadRequest.CreateRequest(out var error))
                {
                    if (error != null)
                    {
                        await WebhookLog.Log(this, true, ForCustomRequest(error.LogMessage));
                        return;
                    }
                    var result = request.Evaluate().Serialize(contentType);
                    switch (result)
                    {
                        case Results.Error _:
                            await WebhookLog.Log(this, true, ForCustomRequest(result.LogMessage));
                            await Db.TransactAsync(() => CheckIfValid());
                            if (CustomPayloadRequest.BreakOnError)
                            {
                                result.Dispose();
                                return;
                            }
                            break;
                        case NoContent _:
                            result.Dispose();
                            if (CustomPayloadRequestStore.BreakOnNoContent) return;
                            break;
                    }
                    (body, contentType, contentLength) = (result.Body, result.Headers.ContentType.GetValueOrDefault(), result.Body.Length);
                }
            }

            try
            {
                var success = false;
                var fail = false;
                var attempts = 0;
                while (!success)
                {
                    var disposableRequest = default(IDisposable);
                    try
                    {
                        if (DestinationIsLocal)
                        {
                            var request = GetLocalRequest(body, contentType);
                            disposableRequest = request;
                            using (var response = request.Evaluate())
                            {
                                response.ThrowIfError();
                                await WebhookLog.Log(this, false, response.LogMessage, contentLength);
                            }
                        }
                        else
                        {
                            var request = GetGlobalRequest(body, contentType);
                            disposableRequest = request;
                            using (var response = await HttpClient.SendAsync(request))
                            {
                                response.EnsureSuccessStatusCode();
                                await WebhookLog.Log(this, false, $"{response.StatusCode}: {response.ReasonPhrase}", contentLength);
                            }
                        }
                        success = true;
                    }
                    catch
                    {
                        if (attempts >= 3)
                        {
                            fail = true;
                            throw;
                        }
                        attempts += 1;
                    }
                    finally
                    {
                        if (success || fail)
                            disposableRequest?.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                await Scheduling.RunTask(() => Db.TransactAsync(() => CheckIfValid()));
                try
                {
                    await WebhookLog.Log(this, true, e.ToString(), contentLength);
                }
                catch (Exception _e)
                {
                    Log.Error($"Could not log webhook error: {e}. Exception: {_e}");
                }
            }
        }

        private static string ForCustomRequest(string message) => $"Error when evaluating custom request: {message}";

        internal static void Check() => Db.TransactAsync(() => Db
            .SQL<Webhook>(All)
            .ForEach(wh => wh.CheckIfValid())
        );
    }
}