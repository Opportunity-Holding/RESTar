using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Meta.Internal;
using RESTar.Requests;
using RESTar.Resources.Operations;
using static System.Reflection.BindingFlags;
using static RESTar.Resources.Operations.DelegateMaker;
using IResource = RESTar.Meta.IResource;
using Resource = RESTar.Meta.Resource;

namespace RESTar.Resources
{
    /// <summary>
    /// A common interface for all entity resource providers
    /// </summary>
    public interface IEntityResourceProvider
    {
        /// <summary>
        /// The ID of the entity resource provider
        /// </summary>
        string Id { get; }
    }

    /// <inheritdoc />
    /// <summary>
    /// A common abstract class for generic EntityResourceProvider instances
    /// </summary>
    internal interface IEntityResourceProviderInternal : IEntityResourceProvider
    {
        /// <summary>
        /// IDatabaseIndexers are plugins for the DatabaseIndex resource, that allow resources 
        /// created by this provider to have database indexes managed by that resource.
        /// </summary>
        IDatabaseIndexer DatabaseIndexer { get; }

        /// <summary>
        /// Should the given type be included in the claim of this entity resource provider?
        /// </summary>
        bool Include(Type type);

        /// <summary>
        /// Marks a collection of regular entity resource types as claimed by this entity resource provider
        /// </summary>
        void MakeClaimRegular(IEnumerable<Type> types);

        /// <summary>
        /// Marks a collection of wrapped entity resource types as claimed by this entity resource provider
        /// </summary>
        void MakeClaimWrapped(IEnumerable<Type> types);

        /// <summary>
        /// Triggers the collection of all procedural resources belonging to this entity resource provider
        /// </summary>
        void MakeClaimProcedural();

        /// <summary>
        /// Inserts the given resource as a new resource claimed by this entity resource provider
        /// </summary>
        void InsertProcedural(IProceduralEntityResource resource);

        /// <summary>
        /// Validates the entity resource provider
        /// </summary>
        void Validate();

        /// <summary>
        /// Returns all procedural entity resources from the provider. Used by RESTar internally. Don't call this method.
        /// </summary>
        IEnumerable<IProceduralEntityResource> SelectProceduralResources();

        /// <summary>
        /// Creates a new dynamic entity resource object with the given name, description and methods. Used by RESTar internally. Don't call this method.
        /// </summary>
        IProceduralEntityResource InsertProceduralResource(string name, string description, Method[] methods, dynamic data);

        /// <summary>
        /// Runs a given update operation. Used by RESTar internally. Don't call this method.
        /// </summary>
        void SetProceduralResourceMethods(IProceduralEntityResource resource, Method[] methods);

        /// <summary>
        /// Runs a given update operation. Used by RESTar internally. Don't call this method.
        /// </summary>
        void SetProceduralResourceDescription(IProceduralEntityResource resource, string newDescription);

        /// <summary>
        /// Deletes a dynamic entity resource entity. Used by RESTar internally. Don't call this method.
        /// </summary>
        bool DeleteProceduralResource(IProceduralEntityResource resource);

        /// <summary>
        /// The ReceiveClaimed method is called by RESTar once one or more resources provided
        /// by this ResourceProvider have been added. Override this to provide additional 
        /// logic once resources have been validated and set up.
        /// </summary>
        void ReceiveClaimed(ICollection<IEntityResource> claimedResources);

        /// <summary>
        /// An optional method for modifying the RESTar resource attribute of a type before the resource is generated
        /// </summary>
        void ModifyResourceAttribute(Type type, RESTarAttribute attribute);

        /// <summary>
        /// Removes the procedural resource belonging to the given type
        /// </summary>
        bool RemoveProceduralResource(Type type);
    }

    /// <inheritdoc />
    /// <summary>
    /// An EntityResourceProvider gives default implementations for the operations of a group of entity resources, 
    /// and defines an attribute that can be used to decorate entity resource types in the application domain. By 
    /// including the ResourceProvider in the call to RESTarConfig.Init(), RESTar can claim entity resource 
    /// types decorated with the attribute and bind the defined resource operations to them.
    /// </summary>
    /// <typeparam name="TBase">The base type for all resources claimed by this ResourceProvider. 
    /// Can be <see cref="object"/> if no such base type exists.</typeparam>
    public abstract class EntityResourceProvider<TBase> : IEntityResourceProviderInternal where TBase : class
    {
        /// <inheritdoc />
        public string Id => GetType().GetEntityResourceProviderId();

        #region IEntityResourceProviderInternal

        #region Helpers

        private void InsertProcedural(IProceduralEntityResource resource)
        {
            var attribute = new RESTarProceduralAttribute(resource.Methods) {Description = resource.Description};
            var type = resource.Type;
            ResourceValidator.ValidateRuntimeInsertion(type, resource.Name, attribute);
            ResourceValidator.Validate(type);
            var inserted = _InsertResource(type, resource.Name, attribute);
            ReceiveClaimed(new[] {inserted});
        }

        private bool RemoveProceduralResource(Type resourceType)
        {
            var iresource = Resource.SafeGet(resourceType);
            if (iresource == null) return true;
            return RemoveResource(iresource);
        }

        private bool RemoveResource(IResource resource)
        {
            if (resource is IEntityResource er && er.Provider == Id)
            {
                RESTarConfig.RemoveResource(resource);
                return true;
            }
            return false;
        }

        #endregion

        IEnumerable<IProceduralEntityResource> IEntityResourceProviderInternal.SelectProceduralResources() => SelectProceduralResources();
        bool IEntityResourceProviderInternal.DeleteProceduralResource(IProceduralEntityResource resource) => DeleteProceduralResource(resource);
        IDatabaseIndexer IEntityResourceProviderInternal.DatabaseIndexer => DatabaseIndexer;
        void IEntityResourceProviderInternal.ReceiveClaimed(ICollection<IEntityResource> claimedResources) => ReceiveClaimed(claimedResources);
        void IEntityResourceProviderInternal.ModifyResourceAttribute(Type type, RESTarAttribute attribute) => ModifyResourceAttribute(type, attribute);
        bool IEntityResourceProviderInternal.RemoveProceduralResource(Type resourceType) => RemoveProceduralResource(resourceType);
        void IEntityResourceProviderInternal.InsertProcedural(IProceduralEntityResource resource) => InsertProcedural(resource);
        bool IEntityResourceProviderInternal.Include(Type type) => Include(type);
        void IEntityResourceProviderInternal.MakeClaimProcedural() => SelectProceduralResources().ForEach(InsertProcedural);
        void IEntityResourceProviderInternal.Validate() => Validate();

        IProceduralEntityResource IEntityResourceProviderInternal.InsertProceduralResource(string n, string d, Method[] m, dynamic data)
        {
            return InsertProceduralResource(n, d, m, data);
        }

        void IEntityResourceProviderInternal.SetProceduralResourceMethods(IProceduralEntityResource resource, Method[] methods)
        {
            SetProceduralResourceMethods(resource, methods);
        }

        void IEntityResourceProviderInternal.SetProceduralResourceDescription(IProceduralEntityResource resource, string newDescription)
        {
            SetProceduralResourceDescription(resource, newDescription);
        }

        void IEntityResourceProviderInternal.MakeClaimRegular(IEnumerable<Type> types) => types.ForEach(type =>
        {
            var resource = _InsertResource(type);
            if (!IsValid(resource, out var reason))
                throw new InvalidResourceDeclarationException("An error was found in the declaration for resource " +
                                                              $"type '{type.GetRESTarTypeName()}': " + reason);
        });

        void IEntityResourceProviderInternal.MakeClaimWrapped(IEnumerable<Type> types) => types.ForEach(type =>
        {
            var resource = _InsertWrapperResource(type, type.GetWrappedType());
            if (!IsValid(resource, out var reason))
                throw new InvalidResourceDeclarationException("An error was found in the declaration for wrapper resource " +
                                                              $"type '{type.GetRESTarTypeName()}': " + reason);
        });

        #endregion

        #region Internal virtual 

        protected virtual bool Include(Type type)
        {
            if (!typeof(TBase).IsAssignableFrom(type))
                throw new InvalidResourceDeclarationException(
                    $"Invalid resource declaration for type '{type.GetRESTarTypeName()}'. Expected type to " +
                    $"inherit from base type '{typeof(TBase).GetRESTarTypeName()}' as required by resource " +
                    $"provider of type '{GetType().GetRESTarTypeName()}'.");
            return type.HasAttribute(AttributeType);
        }

        protected virtual void Validate()
        {
            if (AttributeType == null)
                throw new InvalidEntityResourceProviderException(GetType(), "AttributeType cannot be null");
            if (!AttributeType.IsSubclassOf(typeof(Attribute)))
                throw new InvalidEntityResourceProviderException(GetType(), "Provided AttributeType is not an attribute type");
            if (!AttributeType.IsSubclassOf(typeof(EntityResourceProviderAttribute)))
                throw new InvalidEntityResourceProviderException(GetType(), $"Provided AttributeType '{AttributeType.GetRESTarTypeName()}' " +
                                                                            "does not inherit from RESTar.ResourceProviderAttribute");
        }

        #endregion

        #region Protected

        /// <summary>
        /// The attribute type associated with this ResourceProvider. Used to decorate 
        /// resource types that should be claimed by this ResourceProvider.
        /// </summary>
        protected abstract Type AttributeType { get; }

        /// <summary>
        /// IDatabaseIndexers are plugins for the DatabaseIndex resource, that allow resources 
        /// created by this provider to have database indexes managed by that resource.
        /// </summary>
        protected virtual IDatabaseIndexer DatabaseIndexer { get; } = null;

        /// <summary>
        /// The ReceiveClaimed method is called by RESTar once one or more resources provided
        /// by this ResourceProvider have been added. Override this to provide additional 
        /// logic once resources have been validated and set up.
        /// </summary>
        protected virtual void ReceiveClaimed(ICollection<IEntityResource> claimedResources) { }

        /// <summary>
        /// An optional method for modifying the RESTar resource attribute of a type before the resource is generated
        /// </summary>
        protected virtual void ModifyResourceAttribute(Type type, RESTarAttribute attribute) { }

        /// <summary>
        /// Override this method to add a validation step to the resource claim process. 
        /// </summary>
        /// <param name="resource">The resource to check validity for</param>
        /// <param name="reason">Return the reason for this Type not being valid</param>
        protected virtual bool IsValid(IEntityResource resource, out string reason)
        {
            reason = null;
            return true;
        }

        /// <summary>
        /// Returns all procedural entity resources from the provider. Used by RESTar internally. Don't call this method.
        /// </summary>
        protected virtual IEnumerable<IProceduralEntityResource> SelectProceduralResources()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new procedural entity resource object with the given name, description and methods. Used by RESTar internally. Don't call this method.
        /// </summary>
        protected virtual IProceduralEntityResource InsertProceduralResource(string name, string description, Method[] methods, dynamic data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs a given update operation. Used by RESTar internally. Don't call this method.
        /// </summary>
        protected virtual void SetProceduralResourceMethods(IProceduralEntityResource resource, Method[] methods)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs a given update operation. Used by RESTar internally. Don't call this method.
        /// </summary>
        protected virtual void SetProceduralResourceDescription(IProceduralEntityResource resource, string newDescription)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes a dynamic entity resource entity. Used by RESTar internally. Don't call this method.
        /// </summary>
        protected virtual bool DeleteProceduralResource(IProceduralEntityResource resource)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Selector to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual IEnumerable<T> DefaultSelect<T>(IRequest<T> request) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Inserter to use for resources claimed by this ResourceProvider
        /// </summary>
        ///  <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual int DefaultInsert<T>(IRequest<T> request) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Updater to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual int DefaultUpdate<T>(IRequest<T> request) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Deleter to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual int DefaultDelete<T>(IRequest<T> request) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The default Counter to use for resources claimed by this ResourceProvider
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        [MethodNotImplemented]
        protected virtual long DefaultCount<T>(IRequest<T> request) where T : class, TBase
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Add resource API

        /// <summary>
        /// Inserts a new resource into the RESTar instance, with the given type, name and attribute.
        /// </summary>
        /// <param name="type">The resource type to insert</param>
        /// <param name="fullName">The name of the resource to insert. If null, type.FullName is used</param>
        /// <param name="attribute">The attribute to use when creating the resource. If null, the attribute
        /// is fetched from the resource type declaration.</param>
        /// <returns></returns>
        private IEntityResource InsertResource(Type type, string fullName = null, RESTarAttribute attribute = null)
        {
            ResourceValidator.ValidateRuntimeInsertion(type, fullName, attribute);
            ResourceValidator.Validate(type);
            return _InsertResource(type, fullName, attribute);
        }

        /// <summary>
        /// Inserts a new resource wrapper into the RESTar instance, with the given type, name, attribute and operations.
        /// </summary>
        /// <param name="wrapperType">The wrapper type of the resource</param>
        /// <param name="wrappedType">The type wrapped by the wrapper</param>
        /// <param name="fullName">The name of the resource to insert. If null, type.FullName is used</param>
        /// <param name="attribute">The attribute to use when creating the resource. If null, the attribute
        /// is fetched from the resource type declaration.</param>
        private IEntityResource InsertWrapperResource(Type wrapperType, Type wrappedType, string fullName = null, RESTarAttribute attribute = null)
        {
            return _InsertWrapperResource(wrapperType, wrappedType, fullName, attribute);
        }

        /// <summary>
        /// Inserts a new resource into the RESTar instance, with the given type, name, attribute and operations.
        /// </summary>
        /// <param name="fullName">The name of the resource to insert. If null, type.FullName is used</param>
        /// <param name="attribute">The attribute to use when creating the resource. If null, the attribute
        /// is fetched from the resource type declaration.</param>
        /// <param name="selector">The selector to use. If null, the default selector is used</param>
        /// <param name="inserter">The inserter to use. If null, the default inserter is used</param>
        /// <param name="updater">The updater to use. If null, the default updater is used</param>
        /// <param name="deleter">The deleter to use. If null, the default deleter is used</param>
        /// <param name="counter">The counter to use. If null, the default counter is used</param>
        /// <param name="authenticator">The authenticator to use. If null, the default authenticator is used</param>
        /// <typeparam name="TResource">The type to create the resource for</typeparam>
        /// <returns></returns>
        private IEntityResource<TResource> InsertResource<TResource>(string fullName = null, RESTarAttribute attribute = null,
            Selector<TResource> selector = null, Inserter<TResource> inserter = null, Updater<TResource> updater = null,
            Deleter<TResource> deleter = null, Counter<TResource> counter = null,
            Authenticator<TResource> authenticator = null) where TResource : class, TBase
        {
            return _InsertResource(fullName, attribute, selector, inserter, updater, deleter, counter, authenticator);
        }

        /// <summary>
        /// Inserts a new resource wrapper into the RESTar instance, with the given type, name, attribute and operations.
        /// </summary>
        /// <param name="fullName">The name of the resource to insert. If null, type.FullName is used</param>
        /// <param name="attribute">The attribute to use when creating the resource. If null, the attribute
        /// is fetched from the resource's type declaration.</param>
        /// <param name="selector">The selector to use. If null, the default selector is used</param>
        /// <param name="inserter">The inserter to use. If null, the default inserter is used</param>
        /// <param name="updater">The updater to use. If null, the default updater is used</param>
        /// <param name="deleter">The deleter to use. If null, the default deleter is used</param>
        /// <param name="counter">The counter to use. If null, the default counter is used</param>
        /// <param name="validator">The validator to use. If null, no validator is used.</param>
        /// <param name="authenticator">The authenticator to use. If null, the default authenticator is used</param>
        /// <typeparam name="TWrapper">The resource wrapper type</typeparam>
        /// <typeparam name="TWrapped">The wrapped resource type</typeparam>
        /// <returns></returns>
        private IEntityResource<TWrapped> InsertWrapperResource<TWrapper, TWrapped>(string fullName = null, RESTarAttribute attribute = null,
            Selector<TWrapped> selector = null, Inserter<TWrapped> inserter = null, Updater<TWrapped> updater = null, Deleter<TWrapped> deleter = null,
            Counter<TWrapped> counter = null,  Validator<TWrapped> validator = null,
            Authenticator<TWrapped> authenticator = null)
            where TWrapper : ResourceWrapper<TWrapped> where TWrapped : class, TBase
        {
            return _InsertWrapperResource<TWrapper, TWrapped>(fullName, attribute, selector, inserter, updater, deleter, counter,
                authenticator, validator);
        }

        /// <summary>
        /// Removes the resource corresponding with the given resource type from the RESTar instance
        /// </summary>
        /// <returns>True if and only if a resource was successfully removed</returns>
        private bool RemoveResource<TResource>() where TResource : class, TBase => RemoveResource(Resource<TResource>.SafeGet);

        #endregion

        #region Internals

        private static readonly MethodInfo InsertResourceMethod;
        private static readonly MethodInfo InsertResourceWrappedMethod;

        static EntityResourceProvider()
        {
            var methods = typeof(EntityResourceProvider<TBase>).GetMethods(Instance | NonPublic);
            InsertResourceMethod = methods.First(m => m.Name == nameof(_InsertResource) && m.IsGenericMethod);
            InsertResourceWrappedMethod = methods.First(m => m.Name == nameof(_InsertWrapperResource) && m.IsGenericMethod);
        }

        private IEntityResource _InsertResource(Type type, string fullName = null, RESTarAttribute attribute = null)
        {
            var method = InsertResourceMethod.MakeGenericMethod(type);
            return (IEntityResource) method.Invoke(this, new object[] {fullName, attribute, null, null, null, null, null, null, null});
        }

        private IEntityResource _InsertWrapperResource(Type wrapperType, Type wrappedType, string fullName = null, RESTarAttribute attribute = null)
        {
            var method = InsertResourceWrappedMethod.MakeGenericMethod(wrapperType, wrappedType);
            return (IEntityResource) method.Invoke(this, new object[] {fullName, attribute, null, null, null, null, null, null, null});
        }

        private IEntityResource<TResource> _InsertResource<TResource>(
            string fullName = null,
            RESTarAttribute attribute = null,
            Selector<TResource> selector = null,
            Inserter<TResource> inserter = null,
            Updater<TResource> updater = null,
            Deleter<TResource> deleter = null,
            Counter<TResource> counter = null,
            Authenticator<TResource> authenticator = null,
            Validator<TResource> validator = null
        ) where TResource : class, TBase => new Meta.Internal.EntityResource<TResource>
        (
            fullName: fullName ?? typeof(TResource).GetRESTarTypeName(),
            attribute: attribute ?? typeof(TResource).GetCustomAttribute<RESTarAttribute>(),
            selector: selector ?? GetDelegate<Selector<TResource>>(typeof(TResource)) ?? DefaultSelect,
            inserter: inserter ?? GetDelegate<Inserter<TResource>>(typeof(TResource)) ?? DefaultInsert,
            updater: updater ?? GetDelegate<Updater<TResource>>(typeof(TResource)) ?? DefaultUpdate,
            deleter: deleter ?? GetDelegate<Deleter<TResource>>(typeof(TResource)) ?? DefaultDelete,
            counter: counter ?? GetDelegate<Counter<TResource>>(typeof(TResource)) ?? DefaultCount,
            authenticator: authenticator ?? GetDelegate<Authenticator<TResource>>(typeof(TResource)),
            validator: validator ?? GetDelegate<Validator<TResource>>(typeof(TResource)),
            views: GetViews<TResource>(),
            provider: this
        );

        private IEntityResource<TWrapped> _InsertWrapperResource<TWrapper, TWrapped>(
            string fullName = null,
            RESTarAttribute attribute = null,
            Selector<TWrapped> selector = null,
            Inserter<TWrapped> inserter = null,
            Updater<TWrapped> updater = null,
            Deleter<TWrapped> deleter = null,
            Counter<TWrapped> counter = null,
            Authenticator<TWrapped> authenticator = null,
            Validator<TWrapped> validator = null
        )
            where TWrapper : ResourceWrapper<TWrapped>
            where TWrapped : class, TBase => new Meta.Internal.EntityResource<TWrapped>
        (
            fullName: fullName ?? typeof(TWrapper).GetRESTarTypeName(),
            attribute: attribute ?? typeof(TWrapper).GetCustomAttribute<RESTarAttribute>(),
            selector: selector ?? GetDelegate<Selector<TWrapped>>(typeof(TWrapper)) ?? DefaultSelect,
            inserter: inserter ?? GetDelegate<Inserter<TWrapped>>(typeof(TWrapper)) ?? DefaultInsert,
            updater: updater ?? GetDelegate<Updater<TWrapped>>(typeof(TWrapper)) ?? DefaultUpdate,
            deleter: deleter ?? GetDelegate<Deleter<TWrapped>>(typeof(TWrapper)) ?? DefaultDelete,
            counter: counter ?? GetDelegate<Counter<TWrapped>>(typeof(TWrapper)) ?? DefaultCount,
            authenticator: authenticator ?? GetDelegate<Authenticator<TWrapped>>(typeof(TWrapper)),
            validator: validator ?? GetDelegate<Validator<TWrapped>>(typeof(TWrapper)),
            views: GetWrappedViews<TWrapper, TWrapped>(),
            provider: this
        );


        private static View<TResource>[] GetViews<TResource>() where TResource : class, TBase => typeof(TResource)
            .GetNestedTypes()
            .Where(nested => nested.HasAttribute<RESTarViewAttribute>())
            .Select(view => new View<TResource>(view))
            .ToArray();

        private static View<TWrapped>[] GetWrappedViews<TWrapper, TWrapped>() where TWrapper : ResourceWrapper<TWrapped>
            where TWrapped : class, TBase => typeof(TWrapper)
            .GetNestedTypes()
            .Where(nested => nested.HasAttribute<RESTarViewAttribute>())
            .Select(view => new View<TWrapped>(view))
            .ToArray();

        #endregion
    }
}