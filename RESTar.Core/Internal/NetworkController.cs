using System.Linq;
using RESTar.Linq;
using RESTar.NetworkProviders;
using static RESTar.Admin.Settings;
using static RESTar.RESTarConfig;

namespace RESTar.Internal
{
    internal static class NetworkController
    {
        private static INetworkProvider[] Providers { get; set; }
        internal static void RemoveNetworkBindings() => Providers?.ForEach(provider => provider?.RemoveRoutes(Methods, _Uri, _Port));

        internal static void AddNetworkBindings(params INetworkProvider[] providers)
        {
            Providers = providers.ToArray();
            Providers.ForEach(provider => provider.AddRoutes(Methods, _Uri, _Port));
        }
    }
}