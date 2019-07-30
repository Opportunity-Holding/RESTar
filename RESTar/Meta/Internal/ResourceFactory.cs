using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Internal.Sc;
using RESTar.Linq;
using RESTar.Resources;
using RESTar.Resources.Operations;
using static System.Reflection.BindingFlags;
using static RESTar.Internal.EntityResourceProviderController;

namespace RESTar.Meta.Internal
{
    internal static class ResourceFactory
    {
        private static IEntityResourceProviderInternal StarcounterDeclaredProvider { get; }
        private static IEntityResourceProviderInternal VrProvider { get; }
        private static TerminalResourceProvider TerminalProvider { get; }
        private static BinaryResourceProvider BinaryProvider { get; }
        
        static ResourceFactory()
        {
            StarcounterDeclaredProvider = new StarcounterDeclaredResourceProvider();
            VrProvider = new VirtualResourceProvider();
            EntityResourceProviders.Add(StarcounterDeclaredProvider.Id, StarcounterDeclaredProvider);
            EntityResourceProviders.Add(VrProvider.Id, VrProvider);
            TerminalProvider = new TerminalResourceProvider();
            BinaryProvider = new BinaryResourceProvider();
        }

        private static void ValidateEntityResourceProviders(ICollection<IEntityResourceProvider> _entityResourceProviders)
        {
            if (_entityResourceProviders == null) return;
            var entityResourceProviders = _entityResourceProviders
                .Select(p => p as IEntityResourceProviderInternal ??
                             throw new InvalidEntityResourceProviderException(p.GetType(),
                                 "Must be a subclass of 'RESTar.Resources.EntityResourceProvider'"))
                .ToArray();
            entityResourceProviders.ForEach(p =>
            {
                if (p == null)
                    throw new ArgumentNullException(nameof(entityResourceProviders), "Found null value in entity resource providers collection");
                p.Validate();
            });
            if (entityResourceProviders.ContainsDuplicates(p => p.GetType().GetRESTarTypeName(), out var typeDupe))
                throw new InvalidEntityResourceProviderException(typeDupe.GetType(),
                    $"Two or more external ResourceProviders with the same type '{typeDupe.GetType().GetRESTarTypeName()}' was found. Include " +
                    "only one in the call to RESTarConfig.Init()");
            if (entityResourceProviders.Select(p => p.Id.ToLower()).ContainsDuplicates(out var idDupe))
                throw new InvalidEntityResourceProviderException(idDupe.GetType(),
                    "Two or more external ResourceProviders had simliar type names, which could lead to confusion. Only one provider " +
                    $"should be associated with '{idDupe}'");
            foreach (var provider in entityResourceProviders.Where(provider => provider is IProceduralEntityResourceProvider))
            {
                var methods = provider.GetType().GetMethods(DeclaredOnly | Instance | NonPublic);
                if (methods.All(method => method.Name != "SelectProceduralResources"
                                          && method.Name != "InsertProceduralResource"
                                          && method.Name != "SetProceduralResourceMethods"
                                          && method.Name != "SetProceduralResourceDescription"
                                          && method.Name != "DeleteProceduralResource"))
                    throw new InvalidEntityResourceProviderException(provider.GetType(),
                        $"Resource provider '{provider.GetType()}' was declared to support procedural resources, but did not override methods " +
                        "'SelectProceduralResources()', 'InsertProceduralResource()', 'SetProceduralResourceMethods', 'SetProceduralResourceDescription' " +
                        "and 'DeleteProceduralResource' from 'EntityResourceProvider'."
                    );
            }
            foreach (var provider in entityResourceProviders)
                EntityResourceProviders.Add(provider.Id, provider);
        }

        /// <summary>
        /// All types covered by RESTar are selected and validated here
        /// 
        /// Resources
        ///   entity
        ///     regular
        ///     wrapper
        ///   terminal
        ///   binary
        /// Views 
        /// 
        /// </summary>
        /// <returns></returns>
        private static void ValidateAndBuildTypeLists(out List<Type> regularTypes, out List<Type> wrapperTypes, out List<Type> terminalTypes,
            out List<Type> binaryTypes, out List<Type> eventTypes)
        {
            var allTypes = typeof(object).GetSubclasses().ToList();
            var resourceTypes = allTypes.Where(t => t.HasAttribute<RESTarAttribute>(out var a) && !(a is RESTarProceduralAttribute)).ToArray();
            var viewTypes = allTypes.Where(t => t.HasAttribute<RESTarViewAttribute>()).ToArray();
            if (resourceTypes.Union(viewTypes).ContainsDuplicates(t => t.GetRESTarTypeName()?.ToLower() ?? "unknown", out var dupe))
                throw new InvalidResourceDeclarationException("Types used by RESTar must have unique case insensitive names. Found " +
                                                              $"multiple types with case insensitive name '{dupe}'.");

            void ValidateViewTypes(ICollection<Type> _viewTypes)
            {
                foreach (var viewType in _viewTypes)
                {
                    var resource = viewType.DeclaringType;
                    if (!viewType.IsClass || !viewType.IsNestedPublic || resource == null)
                        throw new InvalidResourceViewDeclarationException(viewType,
                            "Resource view types must be declared as public classes nested within the the " +
                            "resource type they are views for");
                    if (viewType.IsSubclassOf(resource))
                        throw new InvalidResourceViewDeclarationException(viewType, "Views cannot inherit from their resource types");
                    if (typeof(IResourceWrapper).IsAssignableFrom(resource))
                    {
                        var wrapped = resource.GetWrappedType();
                        if (!viewType.ImplementsGenericInterface(typeof(ISelector<>), out var param) || param[0] != wrapped)
                            throw new InvalidResourceViewDeclarationException(viewType,
                                $"Expected view type to implement ISelector<{wrapped.GetRESTarTypeName()}>");
                    }
                    else if (!viewType.ImplementsGenericInterface(typeof(ISelector<>), out var param) || param[0] != resource)
                        throw new InvalidResourceViewDeclarationException(viewType,
                            $"Expected view type to implement ISelector<{resource.GetRESTarTypeName()}>");
                    var resourceProperties = resource.GetDeclaredProperties();
                    foreach (var property in viewType.FindAndParseDeclaredProperties().Where(prop => resourceProperties.ContainsKey(prop.Name)))
                        throw new InvalidResourceViewDeclarationException(viewType,
                            $"Invalid property '{property.Name}'. Resource view types must not contain any public instance " +
                            "properties with the same name (case insensitive) as a property of the corresponding resource. " +
                            "All properties in the resource are automatically inherited for use in conditions with the view.");
                }
            }

            (regularTypes, wrapperTypes, terminalTypes, binaryTypes, eventTypes) = ResourceValidator.Validate(resourceTypes);
            ValidateViewTypes(viewTypes);
        }

        private static void ValidateInnerResources() => RESTarConfig.Resources
            .GroupBy(r => r.ParentResourceName)
            .Where(group => group.Key != null)
            .ForEach(group =>
            {
                var parentResource = (IResourceInternal) Resource.SafeGet(group.Key);
                if (parentResource == null)
                    throw new InvalidResourceDeclarationException(
                        $"Resource type(s) {string.Join(", ", group.Select(item => $"'{item.Name}'"))} is/are declared " +
                        $"within the scope of another class '{group.Key}', that is not a RESTar resource. Inner " +
                        "resources must be declared within a resource class.");
                parentResource.InnerResources = group.ToList();
            });

        internal static void MakeResources(IEntityResourceProvider[] externalProviders)
        {
            ValidateEntityResourceProviders(externalProviders);

            ValidateAndBuildTypeLists
            (
                out var regularTypes,
                out var wrapperTypes,
                out var terminalTypes,
                out var binaryTypes,
                out _
            );

            foreach (var provider in EntityResourceProviders.Values)
            {
                var claim = regularTypes.Where(provider.Include).ToList();
                regularTypes = regularTypes.Except(claim).ToList();
                provider.MakeClaimRegular(claim);
            }

            foreach (var provider in EntityResourceProviders.Values)
            {
                var claim = wrapperTypes.Where(provider.Include).ToList();
                wrapperTypes = wrapperTypes.Except(claim).ToList();
                provider.MakeClaimWrapped(claim);
            }

            foreach (var provider in EntityResourceProviders.Values)
            {
                provider.ReceiveClaimed(Resource.ClaimedBy(provider));
                if (provider is IProceduralEntityResourceProvider)
                    provider.MakeClaimProcedural();
            }

            TerminalProvider.RegisterTerminalTypes(terminalTypes);
            BinaryProvider.RegisterBinaryTypes(binaryTypes);
            ValidateInnerResources();
        }

        internal static void BindControllers()
        {
            foreach (var (provider, controller, baseType) in typeof(Admin.Resource)
                .GetConcreteSubclasses()
                .Select(@class =>
                {
                    foreach (var baseType in @class.GetBaseTypes())
                    {
                        if (baseType.IsGenericType
                            && baseType.GetGenericTypeDefinition() == typeof(ResourceController<,>)
                            && baseType.GetGenericArguments().LastOrDefault() is Type provider)
                            return (provider, @class, baseType);
                    }
                    return default;
                })
                .Where(t => t.provider != null))
            {
                var resourceProvider = EntityResourceProviders.Values
                    .Where(_provider => _provider is IProceduralEntityResourceProvider)
                    .FirstOrDefault(_provider => _provider.GetType() == provider);
                if (resourceProvider == null)
                    throw new InvalidResourceControllerException($"Invalid resource controller '{controller}'. A binding was made to " +
                                                                 $"an EntityResourceProvider of type '{provider}'. No such provider has " +
                                                                 "been included in the call to RESTarConfig.Init().");
                var providerProperty = baseType.GetProperty("ResourceProvider", Static | NonPublic)
                                       ?? throw new Exception($"Unable to locate property 'ResourceProvider' in type '{controller}'");
                var baseNamespaceProperty = baseType.GetProperty("BaseNamespace", Static | NonPublic)
                                            ?? throw new Exception($"Unable to locate property 'BaseNamespace' in type '{controller}'");
                providerProperty.SetValue(null, resourceProvider);
                baseNamespaceProperty.SetValue(null, controller.Namespace);
            }
        }

        /// <summary>
        /// All resources are now in place and metadata can be built. This checks for any additional errors
        /// </summary>
        internal static void FinalCheck()
        {
            var metadata = Metadata.Get(MetadataLevel.Full);
            foreach (var enumType in metadata.PeripheralTypes.Keys.Where(t => t.IsEnum))
            {
                if (Enum.GetNames(enumType).Select(name => name.ToLower()).ContainsDuplicates(out var dupe))
                    throw new InvalidReferencedEnumDeclarationException("A reference was made in some resource type to an enum type with name " +
                                                                        $"'{enumType.GetRESTarTypeName()}', containing multiple named constants equal to '{dupe}' " +
                                                                        "(case insensitive). All enum types referenced by some RESTar resource " +
                                                                        "type must have unique case insensitive named constants");
            }
        }
    }
}