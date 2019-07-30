using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Meta.Internal;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using static RESTar.Internal.EntityResourceProviderController;

namespace RESTar.Admin
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="IUpdater{T}" />
    /// <summary>
    /// A meta-resource that provides representations of all resources in a RESTar instance
    /// </summary>
    [RESTar(Method.GET, Description = description)]
    public class Resource : ISelector<Resource>
    {
        private const string description = "A meta-resource that provides representations " +
                                           "of all resources in a RESTar instance.";

        /// <summary>
        /// The name of the resource
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Resource descriptions are visible in the AvailableMethods resource
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The methods that have been enabled for this resource
        /// </summary>
        public Method[] EnabledMethods { get; set; }

        /// <summary>
        /// Is this resource declared, as opposed to procedural?
        /// </summary>
        public bool IsDeclared { get; internal set; }

        /// <summary>
        /// Is this resource procedural, as opposed to declared?
        /// </summary>
        [RESTarMember(name: "IsProcedural")] public bool _IsProcedural => !IsDeclared;

        /// <summary>
        /// Is this resource internal?
        /// </summary>
        public bool IsInternal { get; set; }

        /// <summary>
        /// The type targeted by this resource.
        /// </summary>
        public Type Type { get; internal set; }

        /// <summary>
        /// The views for this resource
        /// </summary>
        [RESTarMember(hideIfNull: true)] public ViewInfo[] Views { get; private set; }

        /// <summary>
        /// The IResource of this resource
        /// </summary>
        [RESTarMember(hide: true)] public IResource IResource { get; private set; }

        /// <summary>
        /// The resource provider that generated this resource
        /// </summary>
        public string Provider { get; private set; }

        /// <summary>
        /// The resource type, entity resource or terminal resource
        /// </summary>
        public ResourceKind Kind { get; set; }

        /// <summary>
        /// Inner resources for this resource
        /// </summary>
        [RESTarMember(hideIfNull: true)] public IEnumerable<Resource> InnerResources { get; private set; }

        private bool IsProcedural(out IProceduralEntityResource proceduralResource, out IEntityResource entityResource,
            out IEntityResourceProviderInternal provider)
        {
            proceduralResource = null;
            entityResource = null;
            provider = null;

            if (IResource is IEntityResource _entityResource)
                entityResource = _entityResource;
            else return false;
            if (!EntityResourceProviders.TryGetValue(entityResource.Provider, out provider) || !(provider is IProceduralEntityResourceProvider))
                return false;
            var resource = entityResource;
            if (provider.SelectProceduralResources().FirstOrDefault(r => r.Name == resource.Name) is IProceduralEntityResource _dynamicResource)
                proceduralResource = _dynamicResource;
            else return false;
            return true;
        }

        internal static T Make<T>(IResource iresource) where T : Resource, new()
        {
            if (iresource == null) return null;
            var entityResource = iresource as IEntityResource;
            return new T
            {
                Name = iresource.Name,
                Description = iresource.Description ?? "No description",
                EnabledMethods = iresource.AvailableMethods.ToArray(),
                IsInternal = iresource.IsInternal,
                IsDeclared = entityResource?.IsDeclared ?? true,
                Type = iresource.Type,
                Views = entityResource != null
                    ? entityResource.Views?.Select(v => new ViewInfo(v.Name, v.Description ?? "No description")).ToArray()
                      ?? new ViewInfo[0]
                    : null,
                IResource = iresource,
                Provider = entityResource?.Provider ?? (iresource is IBinaryResource ? "Binary" : "Terminal"),
                Kind = iresource.ResourceKind,
                InnerResources = ((IResourceInternal) iresource).InnerResources?.Select(Make<T>).ToArray()
            };
        }

        /// <inheritdoc />
        public IEnumerable<Resource> Select(IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return RESTarConfig.Resources
                .Where(r => r.IsGlobal)
                .OrderBy(r => r.Name)
                .Select(Make<Resource>)
                .Where(request.Conditions);
        }

    }
}