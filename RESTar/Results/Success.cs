﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Internal;
using RESTar.Internal.Logging;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc cref="ISerializedResult"/>
    /// <inheritdoc cref="IResult"/>
    public abstract class Success : ISerializedResult, IResult
    {
        /// <inheritdoc />
        public string TraceId { get; }

        /// <inheritdoc />
        public Context Context { get; }

        /// <inheritdoc />
        public HttpStatusCode StatusCode { get; protected set; }

        /// <inheritdoc />
        public string StatusDescription { get; protected set; }

        /// <inheritdoc />
        public virtual Headers Headers { get; }

        /// <inheritdoc />
        public ICollection<string> Cookies { get; internal set; }

        /// <inheritdoc />
        public bool IsSerialized { get; protected set; }

        /// <inheritdoc />
        public virtual ISerializedResult Serialize(ContentType? contentType = null)
        {
            IsSerialized = true;
            return this;
        }

        /// <inheritdoc />
        public virtual Stream Body { get; set; }

        /// <inheritdoc />
        public TimeSpan TimeElapsed { get; protected set; }

        /// <inheritdoc />
        public virtual LogEventType LogEventType => LogEventType.HttpOutput;

        /// <inheritdoc />
        public virtual string LogMessage => $"{StatusCode.ToCode()}: {StatusDescription} ({Body?.Length ?? 0} bytes)";

        /// <inheritdoc />
        public string LogContent { get; protected set; }

        /// <inheritdoc />
        public string HeadersStringCache { get; set; }

        /// <inheritdoc />
        public bool ExcludeHeaders { get; }

        /// <inheritdoc />
        public DateTime LogTime { get; }

        /// <inheritdoc />
        public virtual IEntities<T> ToEntities<T>() where T : class => (Entities<T>) this;

        /// <inheritdoc />
        public void ThrowIfError() { }

        /// <inheritdoc />
        protected Success(ITraceable trace)
        {
            Context = trace.Context;
            TraceId = trace.TraceId;
            ExcludeHeaders = false;
            Headers = new Headers();
            IsSerialized = false;
            LogTime = DateTime.Now;
            Body = null;
        }

        /// <inheritdoc />
        public virtual string Metadata => $"{GetType().Name};;";

        /// <inheritdoc />
        public void Dispose()
        {
            if (Body is RESTarStream rsc)
                rsc.CanClose = true;
            Body?.Dispose();
        }
    }
}