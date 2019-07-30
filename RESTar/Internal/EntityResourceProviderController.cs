using System.Collections.Generic;
using RESTar.Resources;

namespace RESTar.Internal
{
    internal static class EntityResourceProviderController
    {
        internal static readonly IDictionary<string, IEntityResourceProviderInternal> EntityResourceProviders;
        
        static EntityResourceProviderController() => EntityResourceProviders = new Dictionary<string, IEntityResourceProviderInternal>();
    }
}