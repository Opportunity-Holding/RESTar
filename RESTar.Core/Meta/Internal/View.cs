using System;
using System.Collections.Generic;
using System.Reflection;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;

namespace RESTar.Meta.Internal
{
    /// <inheritdoc cref="IView" />
    /// <summary>
    /// Represents a RESTar resource view
    /// </summary>
    internal class View<TResource> : IView, ITarget<TResource> where TResource : class
    {
        /// <inheritdoc />
        /// <summary>
        /// The binding rule to use when binding conditions to this view
        /// </summary>
        public TermBindingRule ConditionBindingRule { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public Type Type { get; }

        private ViewSelector<TResource> ViewSelector { get; }

        public IEnumerable<TResource> Select(IRequest<TResource> request) => ViewSelector(request);

        /// <inheritdoc />
        public IEntityResource EntityResource { get; private set; }

        public void SetEntityResource(IEntityResource resource) => EntityResource = resource;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }

        internal View(Type viewType)
        {
            var viewAttribute = viewType.GetCustomAttribute<RESTarViewAttribute>();
            Type = viewType;
            Name = viewAttribute.CustomName ?? viewType.Name;
            ViewSelector = DelegateMaker.GetDelegate<ViewSelector<TResource>>(viewType);
            Members = viewType.GetDeclaredProperties();
            Description = viewAttribute.Description;
            ConditionBindingRule = viewAttribute.AllowDynamicConditions
                ? TermBindingRule.DeclaredWithDynamicFallback
                : TermBindingRule.OnlyDeclared;
        }

        /// <inheritdoc />
        public override string ToString() => $"{EntityResource.Name}-{Name}";

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is View<TResource> view && view.Name == Name;

        /// <inheritdoc />
        public override int GetHashCode() => Name.GetHashCode();
    }
}