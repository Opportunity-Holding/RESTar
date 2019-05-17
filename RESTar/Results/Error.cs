using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc cref="Exception" />
    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="ISerializedResult" />
    /// <summary>
    /// A super class for all custom RESTar exceptions
    /// </summary>
    public abstract class Error : RESTarException, IResult, ISerializedResult, ITraceable
    {
        /// <inheritdoc />
        public HttpStatusCode StatusCode { get; protected set; }

        /// <inheritdoc />
        public string StatusDescription { get; protected set; }

        /// <inheritdoc />
        public Headers Headers { get; } = new Headers();

        /// <inheritdoc />
        public Cookies Cookies { get; } = new Cookies();

        /// <inheritdoc />
        public bool IsSuccess { get; }

        /// <inheritdoc />
        public bool IsError => !IsSuccess;

        #region ITraceable, ILogable

        internal void SetTrace(ITraceable request)
        {
            TraceId = request.TraceId;
            Context = request.Context;
        }

        /// <inheritdoc />
        public string TraceId { get; private set; }

        /// <inheritdoc />
        public Context Context { get; private set; }

        /// <inheritdoc />
        public MessageType MessageType => MessageType.HttpOutput;

        /// <inheritdoc />
        public string LogMessage
        {
            get
            {
                var info = Headers.Info;
                var errorInfo = Headers.Error;
                var tail = "";
                if (info != null)
                    tail += $". {info}";
                if (errorInfo != null)
                    tail += $" (see {errorInfo})";
                return $"{StatusCode.ToCode()}: {StatusDescription}{tail}";
            }
        }

        /// <inheritdoc />
        public string LogContent { get; } = null;

        /// <inheritdoc />
        public DateTime LogTime { get; } = DateTime.Now;

        /// <inheritdoc />
        public string HeadersStringCache { get; set; }

        /// <inheritdoc />
        public bool ExcludeHeaders { get; }

        #endregion

        internal Error(ErrorCodes code, string message) : base(code, message)
        {
            ExcludeHeaders = false;
            Headers.Info = Message;
            IsSuccess = false;
        }

        internal Error(ErrorCodes code, string message, Exception ie) : base(code, message, ie)
        {
            ExcludeHeaders = false;
            if (message == null)
                Headers.Info = ie?.Message;
            else Headers.Info = message;
            IsSuccess = false;
        }

        /// <inheritdoc />
        public IEntities<T> ToEntities<T>() where T : class => throw this;

        /// <inheritdoc />
        public void ThrowIfError() => throw this;

        internal IRequestInternal RequestInternal { get; set; }

        private Stream _body;
        private bool IsSerializing;

        /// <inheritdoc />
        public Stream Body
        {
            get => _body ?? (IsSerializing ? _body = new RESTarStream(default) : null);
            set
            {
                if (_body is RESTarStream rsc)
                    rsc.CanClose = true;
                _body?.Dispose();
                _body = value;
            }
        }

        /// <inheritdoc />
        public bool IsSerialized { get; private set; }

        /// <inheritdoc />
        public ISerializedResult Serialize(ContentType? contentType = null)
        {
            if (RequestInternal == null)
            {
                IsSerialized = true;
                Headers.Elapsed = TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
                return this;
            }
            IsSerializing = true;
            var stopwatch = Stopwatch.StartNew();
            ISerializedResult result = this;
            var cachedProvider = RequestInternal.CachedProtocolProvider ?? ProtocolController.DefaultProtocolProvider;
            var acceptProvider = RequestInternal.SafeSelect(request => ContentTypeController.ResolveOutputContentTypeProvider(request, contentType))
                                 ?? cachedProvider.DefaultOutputProvider;
            try
            {
                return cachedProvider.ProtocolProvider.Serialize(this, acceptProvider).Finalize(acceptProvider);
            }
            catch (Exception exception)
            {
                result.Body?.Dispose();
                return exception.AsResultOf(RequestInternal).Serialize();
            }
            finally
            {
                IsSerializing = false;
                IsSerialized = true;
                stopwatch.Stop();
                TimeElapsed = TimeElapsed + stopwatch.Elapsed;
                Headers.Elapsed = TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <inheritdoc />
        public virtual string Metadata => $"{GetType().Name};;";

        /// <inheritdoc />
        /// <summary>
        /// The time elapsed from the start of reqeust evaluation
        /// </summary>
        public TimeSpan TimeElapsed { get; internal set; }

        /// <inheritdoc />
        public override string ToString() => LogMessage;

        /// <inheritdoc />
        public void Dispose()
        {
            if (Body is RESTarStream rsc)
                rsc.CanClose = true;
            Body?.Dispose();
        }
    }
}