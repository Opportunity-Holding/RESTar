using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using RESTar.Linq;
using RESTar.Requests;

namespace RESTar.Internal
{
    internal class HttpRequest : ITraceable
    {
        public Method Method { get; }
        public string URI { get; }
        public Headers Headers { get; }
        public Stream Body { get; }
        public string TraceId { get; }
        public Context Context { get; }
        public async Task<HttpResponse> GetResponseAsync() => await MakeExternalRequestAsync(this, Method.ToString(), new Uri(URI), Body, Headers);

        internal HttpRequest(ITraceable trace, HeaderRequestParameters parameters, Stream body)
        {
            TraceId = trace.TraceId;
            Context = trace.Context;
            URI = parameters.URI;
            Body = body;
            Headers = parameters.Headers;
            Method = parameters.Method;
        }

        private static async Task<HttpResponse> MakeExternalRequestAsync(ITraceable trace, string method, Uri uri, Stream body, Headers headers)
        {
            try
            {
                var request = (HttpWebRequest) WebRequest.Create(uri);
                request.AllowAutoRedirect = false;
                request.Method = method;
                headers.Where(pair => pair.Key != "Content-Type" && pair.Key != "Accept")
                    .ForEach(pair => request.Headers[pair.Key] = pair.Value);
                if (headers.ContentType != null) request.ContentType = headers.ContentType.ToString();
                if (headers.Accept != null) request.Accept = headers.Accept.ToString();
                if (body != null)
                {
                    request.ContentLength = body.Length;
                    using (var requestStream = await request.GetRequestStreamAsync())
                    using (body) await body.CopyToAsync(requestStream);
                }
                var webResponse = (HttpWebResponse) await request.GetResponseAsync();
                var respLoc = webResponse.Headers["Location"];
                if (webResponse.StatusCode == HttpStatusCode.MovedPermanently && respLoc != null)
                    return await MakeExternalRequestAsync(trace, method, new Uri(respLoc), body, headers);
                return new HttpResponse(trace, webResponse);
            }
            catch (WebException we)
            {
                if (!(we.Response is HttpWebResponse response)) return null;
                var _response = new HttpResponse(trace, response.StatusCode, response.StatusDescription);
                foreach (var header in response.Headers.AllKeys)
                    _response.Headers[header] = response.Headers[header];
                return _response;
            }
            catch (Exception e)
            {
                return new HttpResponse(trace, HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }
}