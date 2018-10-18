using System;
using System.Linq;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using Starcounter;
using static RESTar.Admin.Settings;
using static RESTar.Method;

namespace RESTar.Admin
{
    /// <summary>
    /// The Error resource records instances where an error was encountered while
    /// handling a request. You can control how long entities remain in the resource
    /// by setting the daysToSaveErrors parameter in the call to RESTarConfig.Init().
    /// </summary>
    [Database, RESTar(GET, DELETE, Description = description)]
    public class Error
    {
        private const string description = "The Error resource records instances where an " +
                                           "error was encountered while handling a request.";

        internal const string All = "SELECT t FROM RESTar.Admin.Error t";
        internal const string ByTimeLessThan = All + " WHERE t.\"Time\" <?";

        /// <summary>
        /// A unique ID for this error instance
        /// </summary>
        public string Id => this.GetObjectID();

        /// <summary>
        /// The date and time when this error was created
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// The name of the resource that the request was aimed at
        /// </summary>
        public string ResourceName { get; private set; }

        /// <summary>
        /// The method used when the error was created
        /// </summary>
        public Method Method { get; private set; }

        /// <summary>
        /// The error code of the error
        /// </summary>
        public ErrorCodes ErrorCode { get; private set; }

        /// <summary>
        /// The runtime stack trace for the thrown exception
        /// </summary>
        public string StackTrace { get; private set; }

        /// <summary>
        /// A message describing the error
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// The URI of the request that generated the error
        /// </summary>
        public string Uri { get; private set; }

        /// <summary>
        /// The headers of the request that generated the error (API keys are not saved here)
        /// </summary>
        public string Headers { get; private set; }

        /// <summary>
        /// The body of the request that generated the error
        /// </summary>
        public string Body { get; private set; }

        private Error() { }

        private const int MaxStringLength = 10000;

        internal static Error Create(Results.Error error, IRequest request)
        {
            var resource = request.SafeSelect(a => a.Resource);
            var uri = request.UriComponents.ToString();
            var stackTrace = $"{error.StackTrace} §§§ INNER: {error.InnerException?.StackTrace}";
            var totalMessage = error.TotalMessage();
            return new Error
            {
                Time = DateTime.UtcNow,
                ResourceName = (resource?.Name ?? "<unknown>") +
                               (resource?.Alias != null ? $" ({resource.Alias})" : ""),
                Method = request.Method,
                ErrorCode = error.ErrorCode,
                Body = request.GetBody().ToString(),
                StackTrace = stackTrace.Length > MaxStringLength ? stackTrace.Substring(0, MaxStringLength) : stackTrace,
                Message = totalMessage.Length > MaxStringLength ? totalMessage.Substring(0, MaxStringLength) : totalMessage,
                Uri = uri,
                Headers = resource is IEntityResource e && e.RequiresAuthentication
                    ? null
                    : request.Headers.StringJoin(" | ", dict => dict.Select(header =>
                    {
                        switch (header.Key.ToLower())
                        {
                            case "authorization": return "Authorization: apikey *******";
                            case "x-original-url" when header.Value.Contains("key="): return "*******";
                            default: return $"{header.Key}: {header.Value}";
                        }
                    }))
            };
        }

        private static DateTime Checked;

        internal static void ClearOld()
        {
            if (Checked >= DateTime.UtcNow.Date) return;
            var matches = Db.SQL<Error>(ByTimeLessThan, DateTime.UtcNow.AddDays(0 - _DaysToSaveErrors));
            matches.ForEach(Db.Delete);
            Checked = DateTime.UtcNow.Date;
        }

        /// <summary/>
        [RESTarMember(ignore: true), Obsolete] public long Action { get; set; }

        /// <summary/>
        [RESTarMember(ignore: true), Obsolete] public long HandlerAction { get; set; }
    }
}