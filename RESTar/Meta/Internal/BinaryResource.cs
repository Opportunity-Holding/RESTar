using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;

namespace RESTar.Meta.Internal
{
    internal interface IBinaryResource<T> : IBinaryResource, IResource<T> where T : class
    {
        /// <summary>
        /// Selects binary content from a binary resource
        /// </summary>
        (Stream stream, ContentType contentType) SelectBinary(IRequest<T> request);
    }

    internal class BinaryResource<T> : IResource<T>, IResourceInternal, IBinaryResource<T> where T : class
    {
        public string Name { get; }
        public string Description { get; set; }
        public Type Type { get; }
        public TermBindingRule ConditionBindingRule { get; }
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
        public bool Equals(IResource x, IResource y) => x?.Name == y?.Name;
        public int GetHashCode(IResource obj) => obj.Name.GetHashCode();
        public int CompareTo(IResource other) => string.Compare(Name, other.Name, StringComparison.Ordinal);

        public IReadOnlyCollection<Method> AvailableMethods { get; set; }
        public string Alias { get; private set; }
        public bool IsInternal { get; }
        public bool IsGlobal => !IsInternal;
        public bool IsInnerResource { get; }
        public string ParentResourceName { get; }
        public bool GETAvailableToAll { get; }
        public Type InterfaceType { get; }

        public IEnumerable<T> Select(IRequest<T> request) => throw new NotImplementedException();

        public (Stream stream, ContentType contentType) SelectBinary(IRequest<T> request)
        {
            return BinarySelector(request);
        }

        public IReadOnlyList<IResource> InnerResources { get; set; }
        public void SetAlias(string alias) => Alias = alias;
        public ResourceKind ResourceKind { get; }
        private BinarySelector<T> BinarySelector { get; }

        internal BinaryResource(BinarySelector<T> binarySelectorSelector)
        {
            Name = typeof(T).GetRESTarTypeName() ?? throw new Exception();
            Type = typeof(T);
            AvailableMethods = new[] {Method.GET};
            var attribute = typeof(T).GetCustomAttribute<RESTarAttribute>();
            IsInternal = attribute is RESTarInternalAttribute;
            InterfaceType = typeof(T).GetRESTarInterfaceType();
            ResourceKind = ResourceKind.BinaryResource;
            (_, ConditionBindingRule) = typeof(T).GetDynamicConditionHandling(attribute);
            Description = attribute.Description;
            BinarySelector = binarySelectorSelector;
            Members = typeof(T).GetDeclaredProperties();
            GETAvailableToAll = attribute.GETAvailableToAll;
            var typeName = typeof(T).FullName;
            if (typeName?.Contains('+') == true)
            {
                IsInnerResource = true;
                var location = typeName.LastIndexOf('+');
                ParentResourceName = typeName.Substring(0, location).Replace('+', '.');
                Name = typeName.Replace('+', '.');
            }
        }
    }
}