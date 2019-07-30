using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RESTar.Resources;

namespace RESTar.Meta.Internal
{
    internal class RemoteResource : IEntityResource, IResourceInternal
    {
        public string Name { get; }

        public RemoteResource(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
        }

        #region Fail all other access

        private static string ErrorMessage([CallerMemberName] string propertyName = "")
        {
            return $"Cannot call {propertyName} on a remote resource";
        }

        public string Description
        {
            get => throw new InvalidOperationException(ErrorMessage());
            set => throw new InvalidOperationException(ErrorMessage());
        }

        public IReadOnlyList<IResource> InnerResources
        {
            get => throw new InvalidOperationException(ErrorMessage());
            set => throw new InvalidOperationException(ErrorMessage());
        }

        IReadOnlyCollection<Method> IResourceInternal.AvailableMethods
        {
            set => throw new InvalidOperationException(ErrorMessage());
        }

        public Type Type => throw new InvalidOperationException(ErrorMessage());
        public TermBindingRule ConditionBindingRule => throw new InvalidOperationException(ErrorMessage());
        public IReadOnlyDictionary<string, DeclaredProperty> Members => throw new InvalidOperationException(ErrorMessage());
        public bool Equals(IResource x, IResource y) => throw new InvalidOperationException(ErrorMessage());
        public int GetHashCode(IResource obj) => throw new InvalidOperationException(ErrorMessage());
        public int CompareTo(IResource other) => throw new InvalidOperationException(ErrorMessage());
        public string Provider => throw new InvalidOperationException(ErrorMessage());
        public bool ClaimedBy<T>() where T : IEntityResourceProvider => false;
        public bool IsDDictionary => throw new InvalidOperationException(ErrorMessage());
        public bool IsDynamic => throw new InvalidOperationException(ErrorMessage());
        public bool IsDeclared => throw new InvalidOperationException(ErrorMessage());
        public bool DynamicConditionsAllowed => throw new InvalidOperationException(ErrorMessage());
        public bool DeclaredPropertiesFlagged => throw new InvalidOperationException(ErrorMessage());
        public TermBindingRule OutputBindingRule => throw new InvalidOperationException(ErrorMessage());
        public bool RequiresValidation => throw new InvalidOperationException(ErrorMessage());
        public IEnumerable<ITarget> Views => throw new InvalidOperationException(ErrorMessage());
        public bool RequiresAuthentication => throw new InvalidOperationException(ErrorMessage());
        public IReadOnlyCollection<Method> AvailableMethods => throw new InvalidOperationException(ErrorMessage());
        public string Alias => throw new InvalidOperationException(ErrorMessage());
        public bool IsInternal => throw new InvalidOperationException(ErrorMessage());
        public bool IsGlobal => throw new InvalidOperationException(ErrorMessage());
        public bool IsInnerResource => throw new InvalidOperationException(ErrorMessage());
        public string ParentResourceName => throw new InvalidOperationException(ErrorMessage());
        public bool GETAvailableToAll => throw new InvalidOperationException(ErrorMessage());
        public Type InterfaceType => throw new InvalidOperationException(ErrorMessage());
        public ResourceKind ResourceKind => throw new InvalidOperationException(ErrorMessage());
        public void SetAlias(string alias) => throw new InvalidOperationException(ErrorMessage());

        #endregion
    }
}