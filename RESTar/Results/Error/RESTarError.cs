﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using RESTar.Internal;
using RESTar.Logging;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Error.Forbidden;

namespace RESTar.Results.Error
{
    /// <summary>
    /// A super class for all custom RESTar exceptions
    /// </summary>
    public abstract class RESTarError : Exception, IFinalizedResult, ILogable
    {
        /// <summary>
        /// The error code for this error
        /// </summary>
        public ErrorCodes ErrorCode { get; }

        /// <inheritdoc />
        public HttpStatusCode StatusCode { get; protected set; }

        /// <inheritdoc />
        public string StatusDescription { get; protected set; }

        /// <inheritdoc />
        public Headers Headers { get; } = new Headers();

        /// <inheritdoc />
        public ICollection<string> Cookies { get; } = new List<string>();

        Stream IFinalizedResult.Body { get; } = null;
        string IFinalizedResult.ContentType { get; } = null;

        /// <inheritdoc />
        public string HeadersStringCache { get; set; }

        /// <inheritdoc />
        public bool ExcludeHeaders { get; }

        internal void SetTrace(ITraceable trace)
        {
            TraceId = trace.TraceId;
            TcpConnection = trace.TcpConnection;
        }

        /// <inheritdoc />
        public string TraceId { get; private set; }

        /// <inheritdoc />
        public TCPConnection TcpConnection { get; private set; }

        /// <inheritdoc />
        public LogEventType LogEventType => LogEventType.HttpOutput;

        /// <inheritdoc />
        public string LogMessage
        {
            get
            {
                var info = Headers["RESTar-Info"];
                var errorInfo = Headers["ErrorInfo"];
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

        internal RESTarError(ErrorCodes code, string message) : base(message)
        {
            ExcludeHeaders = false;
            ErrorCode = code;
            Headers["RESTar-info"] = Message;
        }

        internal RESTarError(ErrorCodes code, string message, Exception ie) : base(message, ie)
        {
            ExcludeHeaders = false;
            ErrorCode = code;
            Headers["RESTar-info"] = Message;
        }

        internal static RESTarError GetError(Exception exception)
        {
            switch (exception)
            {
                case RESTarError re: return re;
                case FormatException _: return new UnsupportedContent(exception);
                case JsonReaderException _: return new FailedJsonDeserialization(exception);
                case Starcounter.DbException _ when exception.Message.Contains("SCERR4034"): return new AbortedByCommitHook(exception);
                case Starcounter.DbException _: return new StarcounterDatabaseError(exception);
                case RuntimeBinderException _: return new BinderPermissions(exception);
                default: return new Unknown(exception);
            }
        }
    }
}