using System.Collections.Generic;

namespace RESTar.Meta.Internal
{
    internal interface IResourceInternal
    {
        IReadOnlyList<IResource> InnerResources { get; set; }
        string Description { set; }
        IReadOnlyCollection<Method> AvailableMethods { set; }
    }
}