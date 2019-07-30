using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Builder;
using RESTar.ContentTypeProviders;
using RESTar.NetworkProviders;
using RESTar.ProtocolProviders;
using RESTar.Resources;

namespace RESTar.AspNetCore
{
    public static class RESTarExtensions
    {
        internal static byte[] ReadFully(this Stream input)
        {
            var buffer = new byte[16 * 1024];
            using var ms = new MemoryStream();
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }

        public static IApplicationBuilder UseRESTar
        (
            this IApplicationBuilder builder,
            string uri = "/rest",
            bool requireApiKey = false,
            bool allowAllOrigins = true,
            string configFilePath = null,
            bool prettyPrint = true,
            LineEndings lineEndings = LineEndings.Windows,
            IEnumerable<IEntityResourceProvider> entityResourceProviders = null,
            IEnumerable<IProtocolProvider> protocolProviders = null,
            IEnumerable<IContentTypeProvider> contentTypeProviders = null
        )
        {
            builder.UseWebSockets();
            builder.UseRouter(router =>
            {
                var networkProvider = new AspNetCoreNetworkProvider(router);
                RESTarConfig.Init
                (
                    uri: uri,
                    requireApiKey: requireApiKey,
                    allowAllOrigins: allowAllOrigins,
                    configFilePath: configFilePath,
                    prettyPrint: prettyPrint,
                    daysToSaveErrors: 30,
                    lineEndings: lineEndings,
                    entityResourceProviders: entityResourceProviders,
                    protocolProviders: protocolProviders,
                    contentTypeProviders: contentTypeProviders,
                    networkProviders: new List<INetworkProvider> {networkProvider}
                );
            });
            return builder;
        }
    }
}