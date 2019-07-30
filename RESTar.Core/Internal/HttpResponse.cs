using System;
using System.IO;
using System.Net;
using RESTar.Requests;

namespace RESTar.Internal
{
    internal class HttpResponse : ITraceable
    {
        public HttpStatusCode StatusCode { get; }
        public string StatusDescription { get; }
        public long ContentLength { get; }
        public ContentType? ContentType { get; }
        public Stream Body { get; }
        public Headers Headers { get; }
        internal bool IsSuccessStatusCode => StatusCode >= (HttpStatusCode) 200 && StatusCode < (HttpStatusCode) 300;
        public string TraceId { get; }
        public Context Context { get; }
        public string LogMessage => $"{StatusCode.ToCode()}: {StatusDescription} ({Body?.Length ?? 0} bytes) {Headers.Info}";

        private HttpResponse(ITraceable trace)
        {
            TraceId = trace.TraceId;
            Context = trace.Context;
            Headers = new Headers();
        }

        internal HttpResponse(ITraceable trace, HttpStatusCode statusCode, string statusDescription) : this(trace)
        {
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }

        internal HttpResponse(ITraceable trace, HttpWebResponse webResponse) : this(trace)
        {
            StatusCode = webResponse.StatusCode;
            StatusDescription = webResponse.StatusDescription;
            ContentLength = webResponse.ContentLength;
            ContentType = webResponse.ContentType;
            Body = webResponse.GetResponseStream() ?? throw new NullReferenceException("ResponseStream was null");
            foreach (var header in webResponse.Headers.AllKeys)
                Headers[header] = webResponse.Headers[header];
        }
    }
}