﻿using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.ContentTypeProviders;
using RESTar.Internal;
using RESTar.Internal.Logging;
using RESTar.Meta;
using RESTar.Results;

namespace RESTar.Requests
{
    internal class InvalidParametersRequest : IRequest, IRequestInternal
    {
        public bool IsValid { get; }
        private Exception Error { get; }
        public IResult Evaluate() => Error.AsResultOf(this);
        public Type TargetType => null;
        public bool HasConditions => false;

        #region Logable

        private ILogable LogItem => Parameters;
        LogEventType ILogable.LogEventType => LogItem.LogEventType;
        string ILogable.LogMessage => LogItem.LogMessage;
        string ILogable.LogContent => LogItem.LogContent;

        /// <inheritdoc />
        public DateTime LogTime { get; } = DateTime.Now;

        string ILogable.HeadersStringCache
        {
            get => LogItem.HeadersStringCache;
            set => LogItem.HeadersStringCache = value;
        }

        bool ILogable.ExcludeHeaders => LogItem.ExcludeHeaders;

        #endregion

        #region Parameter bindings

        public RequestParameters Parameters { get; }
        public string TraceId => Parameters.TraceId;
        public Context Context => Parameters.Context;
        public CachedProtocolProvider CachedProtocolProvider => Parameters.CachedProtocolProvider;
        public IUriComponents UriComponents => Parameters.Uri;
        public Headers Headers => Parameters.Headers;
        public IResource Resource { get; }
        public bool IsWebSocketUpgrade => Parameters.IsWebSocketUpgrade;
        public TimeSpan TimeElapsed => Parameters.Stopwatch.Elapsed;

        #endregion

        public Method Method { get; set; }
        public MetaConditions MetaConditions { get; }

        private readonly Body body;

        public Body GetBody()
        {
            return body;
        }

        public Headers ResponseHeaders { get; }
        public ICollection<string> Cookies { get; }

        public void SetBody(object content, ContentType? contentType = null) =>
            throw new InvalidOperationException("Cannot set body of an invalid request");

        internal InvalidParametersRequest(RequestParameters parameters)
        {
            IsValid = false;
            Parameters = parameters;
            Error = parameters.Error;
            Resource = parameters.iresource;
            MetaConditions = null;
            Method = parameters.Method;
            if (parameters.BodyBytes?.Any() == true)
                body = new Body
                (
                    stream: new RESTarStream(parameters.BodyBytes),
                    contentType: Headers.ContentType
                                 ?? CachedProtocolProvider?.DefaultInputProvider.ContentType
                                 ?? Providers.Json.ContentType,
                    protocolProvider: parameters.CachedProtocolProvider
                );
            ResponseHeaders = null;
            Cookies = null;
        }

        public void Dispose() => GetBody().Dispose();
    }
}