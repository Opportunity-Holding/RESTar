using System;
using System.Net;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Wraps a result and maps operations to its members
    /// </summary>
    public abstract class ResultWrapper : IResult
    {
        /// <inheritdoc />
        public string TraceId => Result.TraceId;

        /// <inheritdoc />
        public Context Context => Result.Context;

        /// <inheritdoc />
        public MessageType MessageType => Result.MessageType;

        /// <inheritdoc />
        public string LogMessage => Result.LogMessage;

        /// <inheritdoc />
        public string LogContent => Result.LogContent;

        /// <inheritdoc />
        public Headers Headers => Result.Headers;

        /// <inheritdoc />
        public string HeadersStringCache
        {
            get => Result.HeadersStringCache;
            set => Result.HeadersStringCache = value;
        }

        /// <inheritdoc />
        public bool IsSuccess => Result.IsSuccess;

        /// <inheritdoc />
        public bool IsError => Result.IsError;

        /// <inheritdoc />
        public bool ExcludeHeaders => Result.ExcludeHeaders;

        /// <inheritdoc />
        public DateTime LogTime => Result.LogTime;

        /// <inheritdoc />
        public HttpStatusCode StatusCode => Result.StatusCode;

        /// <inheritdoc />
        public string StatusDescription => Result.StatusDescription;

        /// <inheritdoc />
        public Cookies Cookies => Result.Cookies;

        /// <inheritdoc />
        public bool IsSerialized => Result.IsSerialized;

        /// <inheritdoc />
        public ISerializedResult Serialize(ContentType? contentType = null) => Result.Serialize();

        /// <inheritdoc />
        public void ThrowIfError() => Result.ThrowIfError();

        /// <inheritdoc />
        public IEntities<T> ToEntities<T>() where T : class => Result.ToEntities<T>();

        /// <inheritdoc />
        public TimeSpan TimeElapsed => Result.TimeElapsed;

        /// <inheritdoc />
        public string Metadata => Result.Metadata;

        /// <inheritdoc />
        public void Dispose() => Result.Dispose();

        /// <summary>
        /// The wrapped result
        /// </summary>
        protected readonly IResult Result;

        /// <inheritdoc />
        protected ResultWrapper(IResult result) => Result = result;
    }
}