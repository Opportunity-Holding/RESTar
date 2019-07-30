using System;
using RESTar.Resources;

namespace RESTar.Meta.Internal
{
    internal class VirtualResourceProvider : EntityResourceProvider<object>
    {
        internal override bool Include(Type type) => !type.HasResourceProviderAttribute();
        internal override void Validate() { }
        protected override Type AttributeType { get; } = null;
    }
}