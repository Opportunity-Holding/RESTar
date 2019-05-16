using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Meta.Internal;
using RESTar.Requests;
using RESTar.Resources.Operations;
using Starcounter;

namespace RESTar.Resources
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="IInserter{T}" />
    /// <inheritdoc cref="IUpdater{T}" />
    /// <inheritdoc cref="IDeleter{T}" />
    /// <inheritdoc cref="ResourceController{TController,TProvider}" />
    /// <summary>
    /// Resource controllers attach to entity resource providers that support procedural resources,
    /// and enable insertion of resources during runtime.
    /// </summary>
    /// <typeparam name="TProvider"></typeparam>
    /// <typeparam name="TController"></typeparam>
    public abstract class ResourceController<TController, TProvider> : Resource, ISelector<TController>, IInserter<TController>,
        IUpdater<TController>, IDeleter<TController>
        where TController : ResourceController<TController, TProvider>, new()
        where TProvider : IEntityResourceProvider, IProceduralEntityResourceProvider
    {
        internal static string BaseNamespace { private get; set; }
        internal static TProvider ResourceProvider { private get; set; }
        private static IEntityResourceProviderInternal ResourceProviderInternal => (IEntityResourceProviderInternal) ResourceProvider;

        private static void ResolveDynamicResourceName(ref string name)
        {
            switch (name)
            {
                case var _ when !Regex.IsMatch(name, RegEx.DynamicResourceName):
                    throw new Exception($"Resource name '{name}' contains invalid characters. Letters, nu" +
                                        "mbers and underscores are valid in resource names. Dots can be used " +
                                        "to organize resources into namespaces. No other characters can be used.");
                case var _ when name.StartsWith(".") || name.Contains("..") || name.EndsWith("."):
                    throw new Exception($"'{name}' is not a valid resource name. Invalid namespace syntax");
            }
            if (!name.StartsWith($"{BaseNamespace}."))
            {
                if (name.StartsWith($"{BaseNamespace}.", StringComparison.OrdinalIgnoreCase))
                {
                    var nrOfDots = name.Count(c => c == '.') + 2;
                    name = $"{BaseNamespace}.{name.Split(new[] {'.'}, nrOfDots).Last()}";
                }
                else name = $"{BaseNamespace}.{name}";
            }
            if (RESTarConfig.ResourceByName.ContainsKey(name))
                throw new Exception($"Invalid resource name '{name}'. Name already in use.");
        }

        /// <summary>
        /// Additional data associated with this resource (as defined by the resource provider)
        /// </summary>
        [RESTarMember(ignore: true)] protected virtual dynamic Data { get; } = null;

        /// <summary>
        /// Selects the instances that have been inserted by this controller
        /// </summary>
        protected static IEnumerable<TController> Select() => ResourceProviderInternal
            .SelectProceduralResources()
            .OrderBy(r => r.Name)
            .Select(r => Make<TController>(Meta.Resource.SafeGet(r.Name)));

        /// <summary>
        /// Inserts the current instance as a new procedural resource
        /// </summary>
        protected void Insert()
        {
            var name = Name;
            var methods = EnabledMethods;
            var description = Description;
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Missing or invalid name for new resource");
            ResolveDynamicResourceName(ref name);
            var methodsArray = methods.ResolveMethodRestrictions().ToArray();

            var inserted = ResourceProviderInternal.InsertProceduralResource(name, description, methodsArray, (object) Data);
            if (inserted != null)
                ResourceProviderInternal.InsertProcedural(inserted);

            var resource = (IResourceInternal) Meta.Resource.SafeGet(name);
            resource.SetAlias(Alias);
        }

        /// <summary>
        /// Updates the state of the current instance to the corresponding procedural resource
        /// </summary>
        protected void Update()
        {
            var procedural = ResourceProviderInternal.SelectProceduralResources()?.FirstOrDefault(item => item.Name == Name) ??
                             throw new InvalidOperationException($"Cannot update resource '{Name}'. Resource has not been inserted.");
            var resource = (IResourceInternal) Meta.Resource.SafeGet(procedural.Type) ??
                           throw new InvalidOperationException($"Cannot update resource '{Name}'. Resource has not been inserted.");
            resource.SetAlias(Alias);
            ResourceProviderInternal.SetProceduralResourceDescription(procedural, Description);
            resource.Description = Description;
            var methods = EnabledMethods.ResolveMethodRestrictions().ToArray();
            ResourceProviderInternal.SetProceduralResourceMethods(procedural, methods);
            resource.AvailableMethods = methods;
        }

        /// <summary>
        /// Deletes the corresponding procedural resource
        /// </summary>
        protected void Delete()
        {
            var procedural = ResourceProviderInternal.SelectProceduralResources()?.FirstOrDefault(item => item.Name == Name);
            if (procedural == null) return;
            var type = procedural.Type;
            Db.TransactAsync(() => ResourceAlias.GetByResource(procedural.Name)?.Delete());
            if (ResourceProviderInternal.DeleteProceduralResource(procedural))
                ResourceProviderInternal.RemoveProceduralResource(type);
        }

        #region RESTar

        /// <inheritdoc />
        public virtual IEnumerable<TController> Select(IRequest<TController> request)
        {
            return Select().Where(request.Conditions);
        }

        /// <inheritdoc />
        public virtual int Insert(IRequest<TController> request)
        {
            var i = 0;
            foreach (var resource in request.GetInputEntities())
            {
                resource.Insert();
                i += 1;
            }
            return i;
        }

        /// <inheritdoc />
        public virtual int Update(IRequest<TController> request)
        {
            var i = 0;
            foreach (var resource in request.GetInputEntities())
            {
                resource.Update();
                i += 1;
            }
            return i;
        }

        /// <inheritdoc />
        public virtual int Delete(IRequest<TController> request)
        {
            var i = 0;
            foreach (var resource in request.GetInputEntities())
            {
                resource.Delete();
                i += 1;
            }
            return i;
        }

        #endregion
    }
}