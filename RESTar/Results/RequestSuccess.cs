﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using RESTar.Internal;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc cref="ISerializedResult" />
    /// <inheritdoc cref="IResult" />
    /// <summary>
    /// The result of a RESTar request operation
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
                return protocolProvider.Serialize(this, acceptProvider).Finalize(acceptProvider);
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

        /// <summary>
        /// Generates a URI string from URI components, according to the protocol of this Content
        /// </summary>
        protected string GetUriString(IUriComponents components) => RequestInternal
            .CachedProtocolProvider
            .ProtocolProvider
            .MakeRelativeUri(components);

        internal RequestSuccess(IRequest request) : base(request)
        {
            Request = request;
            TimeElapsed = request.TimeElapsed;
            RequestInternal = (IRequestInternal) request;
        }
    }
}