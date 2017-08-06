﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;
using static System.Reflection.BindingFlags;
using static RESTar.RESTarConfig;
using static RESTar.RESTarMethods;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    /// <summary>
    /// A resource that lists all available resources in a RESTar instance
    /// </summary>
    [RESTar, OpenResource(GET)]
    public sealed class Resource : ISelector<Resource>, IInserter<Resource>, IUpdater<Resource>, IDeleter<Resource>
    {
        /// <summary>
        /// The name of the resource
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The alias of this resource, if any
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// The methods that have been enabled for this resource
        /// </summary>
        public RESTarMethods[] AvailableMethods { get; set; }

        /// <summary>
        /// Is this resource editable?
        /// </summary>
        public bool Editable { get; private set; }

        /// <summary>
        /// Is this resource internal?
        /// </summary>
        public bool IsInternal { get; set; }

        /// <summary>
        /// The type targeted by this resource.
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// The IResource of this resource
        /// </summary>
        [IgnoreDataMember]
        public IResource IResource { get; private set; }

        /// <summary>
        /// The resource type
        /// </summary>
        public RESTarResourceType ResourceType { get; private set; }

        #region RESTar operations

        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        public IEnumerable<Resource> Select(IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var accessRights = AuthTokens[request.AuthToken];
            return Resources
                .Where(r => r.IsGlobal)
                .OrderBy(r => r.Name)
                .Select(resource => new Resource
                {
                    Name = resource.Name,
                    Alias = resource.Alias,
                    AvailableMethods = accessRights.SafeGet(resource)?.Intersect(resource.AvailableMethods).ToArray()
                                       ?? new RESTarMethods[0],
                    Editable = resource.Editable,
                    IsInternal = resource.IsInternal,
                    Type = resource.Type.FullName,
                    IResource = resource,
                    ResourceType = resource.ResourceType
                })
                .Where(request.Conditions);
        }

        /// <summary>
        /// RESTar inserter (don't use)
        /// </summary>
        public int Insert(IEnumerable<Resource> resources, IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var entity in resources)
            {
                if (string.IsNullOrEmpty(entity.Alias))
                    throw new Exception("No Alias for new resource");
                if (Resources.Any(r => r.Name.EqualsNoCase(entity.Alias)))
                    throw new AliasEqualToResourceNameException(entity.Alias);
                if (ResourceAlias.Exists(entity.Alias, out var alias))
                    throw new AliasAlreadyInUseException(alias);
                entity.AvailableMethods = Methods;
                DynamicResource.MakeTable(entity);
                count += 1;
            }
            return count;
        }

        /// <summary>
        /// RESTar updater (don't use)
        /// </summary>
        public int Update(IEnumerable<Resource> entities, IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var resource in entities)
            {
                var updated = false;
                var iresource = resource.IResource;
                if (!string.IsNullOrWhiteSpace(resource.Alias) && resource.Alias != iresource.Alias)
                {
                    iresource.Alias = resource.Alias;
                    updated = true;
                }
                if (iresource.Editable)
                {
                    var methods = resource.AvailableMethods?.Distinct().ToList();
                    methods?.Sort(MethodComparer.Instance);
                    if (methods != null && !iresource.AvailableMethods.SequenceEqual(methods))
                    {
                        iresource.AvailableMethods = methods;
                        var dynamicResource = resource.GetDynamicResource();
                        if (dynamicResource != null)
                            dynamicResource.AvailableMethods = methods;
                        updated = true;
                    }
                }
                if (updated) count += 1;
            }
            return count;
        }

        /// <summary>
        /// RESTar deleter (don't use)
        /// </summary>
        public int Delete(IEnumerable<Resource> entities, IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var resource in entities)
            {
                DynamicResource.DeleteTable(resource);
                count += 1;
            }
            return count;
        }

        #endregion

        #region Register resources

        /// <summary>
        /// Registers a class as a RESTar resource. If no methods are provided in the 
        /// methods list, all methods will be enabled for this resource.
        /// </summary>
        public static void Register<T>(params RESTarMethods[] methods) where T : class
        {
            if (!methods.Any()) methods = Methods;
            Register<T>(methods.OrderBy(i => i, MethodComparer.Instance).ToArray(), null);
        }

        /// <summary>
        /// Registers a class as a RESTar resource. If no methods are provided in the 
        /// methods list, all methods will be enabled for this resource.
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        /// <param name="methods">The methods to make available for this resource</param>
        /// <param name="selector">The selector to use for this resource</param>
        /// <param name="inserter">The inserter to use for this resource</param>
        /// <param name="updater">The updater to use for this resource</param>
        /// <param name="deleter">The deleter to use for this resource</param>
        public static void Register<T>
        (
            ICollection<RESTarMethods> methods,
            Selector<T> selector = null,
            Inserter<T> inserter = null,
            Updater<T> updater = null,
            Deleter<T> deleter = null
        ) where T : class
        {
            if (typeof(T).HasAttribute<RESTarAttribute>())
                throw new InvalidOperationException("Cannot manually register resources that have a RESTar " +
                                                    "attribute. Resources decorated with a RESTar attribute " +
                                                    "are registered automatically");
            var attribute = new RESTarAttribute(methods.ToArray());
            Resource<T>.Make(attribute, selector, inserter, updater, deleter);
        }

        #endregion

        #region Find and get resources

        /// <summary>
        /// Finds a resource by a search string. The string can be a partial resource name. If no resource 
        /// is found, throws an UnknownResourceException. If more than one resource is found, throws
        /// an AmbiguousResourceException.
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// </summary>
        public static IResource Find(string searchString)
        {
            searchString = searchString.ToLower();
            var resource = ResourceAlias.ByAlias(searchString)?.IResource;
            if (resource == null)
                ResourceByName.TryGetValue(searchString, out resource);
            if (resource != null)
                return resource;
            var matches = ResourceByName
                .Where(pair => pair.Value.IsGlobal && pair.Key.EndsWith($".{searchString}"))
                .Select(pair => pair.Value)
                .ToList();
            switch (matches.Count)
            {
                case 0: throw new UnknownResourceException(searchString);
                case 1: return matches[0];
                default: throw new AmbiguousResourceException(searchString, matches.Select(c => c.Name).ToList());
            }
        }

        /// <summary>
        /// Finds a resource by a search string. The string can be a partial resource name. If no resource 
        /// is found, returns null. If more than one resource is found, throws an AmbiguousResourceException.
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// </summary>
        public static IResource SafeFind(string searchString)
        {
            searchString = searchString.ToLower();
            var resource = ResourceAlias.ByAlias(searchString)?.IResource;
            if (resource == null)
                ResourceByName.TryGetValue(searchString, out resource);
            if (resource != null)
                return resource;
            var matches = ResourceByName
                .Where(pair => pair.Value.IsGlobal && pair.Key.EndsWith($".{searchString}"))
                .Select(pair => pair.Value)
                .ToList();
            switch (matches.Count)
            {
                case 0: return null;
                case 1: return matches[0];
                default: throw new AmbiguousResourceException(searchString, matches.Select(c => c.Name).ToList());
            }
        }

        /// <summary>
        /// Finds a number of resources based on a search string. To include more than one resource in 
        /// the search, use the wildcard character (asterisk '*'). To find all resources in a namespace
        /// 'MyApplication.Utilities', use the search string "myapplication.utilities.*" or any case 
        /// variant of it.
        /// </summary>
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// <returns></returns>
        public static IResource[] SafeFindMany(string searchString)
        {
            searchString = searchString.ToLower();
            switch (searchString.Count(i => i == '*'))
            {
                case 0:
                    var found = SafeFind(searchString);
                    if (found == null) return new IResource[0];
                    return new[] {found};
                case 1 when searchString.Last() != '*':
                    throw new Exception("Invalid resource string syntax. The asterisk must be the last character");
                case 1:
                    var commonPart = searchString.TrimEnd('*');
                    var commonPartDots = commonPart.Count(c => c == '.');
                    var matches = ResourceByName
                        .Where(pair => pair.Key.StartsWith(commonPart) &&
                                       pair.Key.Count(c => c == '.') == commonPartDots)
                        .Select(pair => pair.Value)
                        .ToArray();
                    return matches;
                default: throw new Exception("Invalid resource string syntax. Can only include one asterisk (*)");
            }
        }

        /// <summary>
        /// Finds a resource by name (case sensitive) and throws an UnknownResourceException
        /// if no resource is found.
        /// </summary>
        public static IResource Get(string name) => ResourceByName.SafeGet(name)
                                                    ?? throw new UnknownResourceException(name);

        /// <summary>
        /// Finds a resource by name (case insensitive) and returns null
        /// if no resource is found
        /// </summary>
        public static IResource SafeGet(string name) => ResourceByName.SafeGetNoCase(name);

        /// <summary>
        /// Finds a resource by target type and throws an UnknownResourceException
        /// if no resource is found.
        /// </summary>
        public static IResource Get(Type type) => ResourceByType.SafeGet(type)
                                                  ?? throw new UnknownResourceException(type.FullName);

        /// <summary>
        /// Finds a resource by target type and throws an UnknownResourceException
        /// if no resource is found.
        /// </summary>
        public static IResource SafeGet(Type type) => ResourceByType.SafeGet(type);

        /// <summary>
        /// Finds a resource by target type and throws an UnknownResourceException
        /// if no resource is found.
        /// </summary>
        public static IResource<T> Get<T>() where T : class => Resource<T>.Get;

        /// <summary>
        /// Finds a resource by target type, and returns null if no
        /// resource is found.
        /// </summary>
        public static IResource<T> SafeGet<T>() where T : class => Resource<T>.SafeGet;

        #endregion

        #region Helpers

        private static readonly MethodInfo AUTO_MAKER;
        private static readonly MethodInfo DYNAMIC_AUTO_MAKER;
        private const string DynamicResourceSQL = "SELECT t FROM RESTar.Internal.DynamicResource t WHERE t.Name =?";
        internal DynamicResource GetDynamicResource() => Db.SQL<DynamicResource>(DynamicResourceSQL, Name).First;

        static Resource()
        {
            DYNAMIC_AUTO_MAKER = typeof(Resource).GetMethod(nameof(DYNAMIC_AUTO_MAKE), NonPublic | Static);
            AUTO_MAKER = typeof(Resource).GetMethod(nameof(AUTO_MAKE), NonPublic | Static);
        }

        internal static void AutoMakeDynamicResource(DynamicResource resource)
        {
            DYNAMIC_AUTO_MAKER.MakeGenericMethod(resource.Table).Invoke(null, new object[] {resource.Attribute});
        }

        internal static void AutoMakeResource(Type type)
        {
            AUTO_MAKER.MakeGenericMethod(type).Invoke(null, null);
        }

        private static void AUTO_MAKE<T>() where T : class
        {
            Resource<T>.Make(typeof(T).GetAttribute<RESTarAttribute>());
        }

        private static void DYNAMIC_AUTO_MAKE<T>(RESTarAttribute attribute) where T : class
        {
            Resource<T>.Make(attribute);
        }

        #endregion
    }
}