using System;
using RESTar.Resources;

namespace RESTar.Meta.Internal
{
    internal class VirtualResourceProvider : EntityResourceProvider<object>
    {
        protected override bool Include(Type type) => !type.HasResourceProviderAttribute();
        protected override void Validate() { }
        protected override Type AttributeType { get; } = null;
    }
}