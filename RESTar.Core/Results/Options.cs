using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using RESTar.Internal;
using RESTar.Meta;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on a successful CORS preflight
    /// </summary>
    internal class Options : Success
    {
        private IResource Resource { get; }
        private bool HasResource => Resource != null;

        internal static Options Create(RequestParameters parameters)
        {
            var options = new Options(parameters);
            if (!parameters.IsValid)
                return options;
            if (RESTarConfig.AllowAllOrigins)
                options.Headers.AccessControlAllowOrigin = "*";
            else if (Uri.TryCreate(parameters.Headers.Origin, UriKind.Absolute, out var origin) && RESTarConfig.AllowedOrigins.Contains(origin))
            {
                options.Headers.AccessControlAllowOrigin = origin.ToString();
                options.Headers.Vary = "Origin";
            }
            else return options;
            if (options.HasResource)
                options.Headers.AccessControlAllowMethods = string.Join(", ", options.Resource.AvailableMethods);
            options.Headers.AccessControlMaxAge = "120";
            options.Headers.AccessControlAllowCredentials = "true";
            options.Headers.AccessControlAllowHeaders = "origin, content-type, accept, authorization, source, destination";
            return options;
        }

        private Options(RequestParameters parameters) : base(parameters)
        {
            StatusCode = HttpStatusCode.OK;
            StatusDescription = "OK";
            Resource = parameters.iresource;
        }

        public override ISerializedResult Serialize(ContentType? contentType = null)
        {
            if (IsSerialized) return this;
            if (!HasResource)
                return base.Serialize(contentType);
            var stopwatch = Stopwatch.StartNew();
            var optionsBody = new OptionsBody(Resource.Name, Resource.ResourceKind, Resource.AvailableMethods);
            var provider = ContentTypeController.ResolveOutputContentTypeProvider(null, contentType);
            Body = new MemoryStream();
            provider.SerializeCollection(new[] {optionsBody}, Body);
            this.Finalize(provider);
            IsSerialized = true;
            stopwatch.Stop();
            TimeElapsed = TimeElapsed + stopwatch.Elapsed;
            Headers.Elapsed = TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            return this;
        }
    }

    internal class OptionsBody
    {
        public string Resource { get; }
        public ResourceKind ResourceKind { get; }
        public IEnumerable<Method> Methods { get; }

        public OptionsBody(string resource, ResourceKind resourceKind, IEnumerable<Method> methods)
        {
            Resource = resource;
            ResourceKind = resourceKind;
            Methods = methods;
        }
    }
}