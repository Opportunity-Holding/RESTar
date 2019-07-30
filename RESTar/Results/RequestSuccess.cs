using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using RESTar.Internal;
using RESTar.Requests;
using Starcounter.Nova;

namespace RESTar.Results
{
    /// <inheritdoc cref="ISerializedResult" />
    /// <inheritdoc cref="IResult" />
    /// <summary>
    /// The successful result of a RESTar request operation
    /// </summary>
    public abstract class RequestSuccess : Success
    {
        /// <summary>
        /// The request that generated this result
        /// </summary>
        public IRequest Request { get; }

        private IRequestInternal RequestInternal { get; }

        private Stream _body;
        private bool IsSerializing;

        /// <inheritdoc />
        public override Stream Body
        {
            get => _body ?? (IsSerializing ? _body = new RESTarStream(default) : null);
            set
            {
                if (ReferenceEquals(_body, value)) return;
                if (_body is RESTarStream rsc)
                    rsc.CanClose = true;
                _body?.Dispose();
                _body = value;
            }
        }

        private static readonly TransactOptions ReadonlyOptions = new TransactOptions(TransactionFlags.ReadOnly);

        /// <inheritdoc />
        public override ISerializedResult Serialize(ContentType? contentType = null)
        {
            if (IsSerialized) return this;
            IsSerializing = true;
            var stopwatch = Stopwatch.StartNew();
            ISerializedResult result = this;
            try
            {
                var protocolProvider = RequestInternal.CachedProtocolProvider.ProtocolProvider;
                var acceptProvider = ContentTypeController.ResolveOutputContentTypeProvider(RequestInternal, contentType);
                return Db.Transact(() => protocolProvider.Serialize(this, acceptProvider).Finalize(acceptProvider), ReadonlyOptions);
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
                TimeElapsed += stopwatch.Elapsed;
                Headers.Elapsed = TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal RequestSuccess(IRequest request) : base(request)
        {
            Request = request;
            TimeElapsed = request.TimeElapsed;
            RequestInternal = (IRequestInternal) request;
        }
    }
}