using System;
using System.Collections.Generic;
using RESTar.Admin;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using Starcounter;

namespace RESTar.Internal.Sc
{
    internal class StarcounterDeclaredResourceProvider : EntityResourceProvider<object>
    {
        protected override Type AttributeType { get; } = null;
        public override IDatabaseIndexer DatabaseIndexer { get; } = new ScIndexer();
        internal override void Validate() { }

        internal override bool Include(Type type)
        {
            if (type.IsWrapper())
                return type.GetWrappedType().HasAttribute<DatabaseAttribute>() && !type.HasResourceProviderAttribute();
            return type.HasAttribute<DatabaseAttribute>() && !type.HasResourceProviderAttribute();
        }

        protected override IEnumerable<T> DefaultSelect<T>(IRequest<T> request) => StarcounterOperations<T>.Select(request);
        protected override int DefaultInsert<T>(IRequest<T> request) => StarcounterOperations<T>.Insert(request);
        protected override int DefaultUpdate<T>(IRequest<T> request) => StarcounterOperations<T>.Update(request);
        protected override int DefaultDelete<T>(IRequest<T> request) => StarcounterOperations<T>.Delete(request);
        protected override ResourceProfile DefaultProfile<T>(IEntityResource<T> resource) => StarcounterOperations<T>.Profile(resource);

        protected override bool IsValid(IEntityResource resource, out string reason) =>
            StarcounterOperations<object>.IsValid(resource, out reason);
    }
}