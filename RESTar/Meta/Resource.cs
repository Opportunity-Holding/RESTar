using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Resources;
using RESTar.Results;

namespace RESTar.Meta
{
    /// <summary>
    /// A static class for finding registered RESTar resources
    /// </summary>
    public static class Resource
    {
        #region Find and get resources

        /// <summary>
        /// Gets all registered RESTar resources
        /// </summary>
        public static IEnumerable<IResource> All => RESTarConfig.Resources;

        /// <summary>
        /// Gets all registered RESTar resources that were claimed by the given 
        /// ResourceProvider type.
        /// </summary>
        public static IEnumerable<IEntityResource> ClaimedBy<T>() where T : IEntityResourceProvider => All
            .OfType<IEntityResource>()
            .Where(r => r.ClaimedBy<T>());

        /// <summary>
        /// Gets all registered RESTar resources that were claimed by the given 
        /// ResourceProvider type.
        /// </summary>
        public static ICollection<IEntityResource> ClaimedBy(IEntityResourceProvider provider) => All
            .OfType<IEntityResource>()
            .Where(r => r.Provider == provider.Id)
            .ToList();

        /// <summary>
        /// Finds a resource by a search string. The string can be a partial resource name. If no resource 
        /// is found, throws an UnknownResource exception. If more than one resource is found, throws
        /// an AmbiguousResource exception.
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// <param name="kind">The resource kind to filter results against</param>
        /// </summary>
        public static IResource Find(string searchString, ResourceKind kind = ResourceKind.All)
        {
            if (searchString == null) throw new UnknownResource("null");
            if (!RESTarConfig.ResourceFinder.TryGetValue(searchString, out var resource))
                throw new UnknownResource(searchString);
            if (resource == null)
                throw new AmbiguousResource(searchString);
            if (!kind.HasFlag(resource.ResourceKind))
                throw new WrongResourceKind(searchString, kind, resource.ResourceKind);
            return resource;
        }

        /// <summary>
        /// Finds a resource by a search string. The string can be a partial resource name.
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// <param name="resource">The found resource (if any)</param>
        /// <param name="error">Describes the error that occured when locating the resource (if any)</param>
        /// <param name="kind">The resource kind to filter results against</param>
        /// </summary>
        public static bool TryFind(string searchString, out IResource resource, out Error error, ResourceKind kind = ResourceKind.All)
        {
            searchString = searchString.ToLower();
            error = null;
            if (!RESTarConfig.ResourceFinder.TryGetValue(searchString, out resource))
            {
                error = new UnknownResource(searchString);
                return false;
            }
            if (resource == null)
            {
                error = new AmbiguousResource(searchString);
                return false;
            }
            if (!kind.HasFlag(resource.ResourceKind))
            {
                error = new WrongResourceKind(searchString, kind, resource.ResourceKind);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Finds a resource by a search string. The string can be a partial resource name. If no resource 
        /// is found, throws an UnknownResource exception. If more than one resource is found, throws
        /// an AmbiguousResource exception.
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// </summary>
        public static T Find<T>(string searchString) where T : IResource
        {
            return (T) Find(searchString, typeof(T).GetResourceKind());
        }

        /// <summary>
        /// Finds a resource by a search string. The string can be a partial resource name.
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// <param name="resource">The found resource (if any)</param>
        /// <param name="error">Describes the error that occured when locating the resource (if any)</param>
        /// </summary>
        public static bool TryFind<T>(string searchString, out T resource, out Error error) where T : IResource
        {
            var kind = typeof(T).GetResourceKind();
            if (!TryFind(searchString, out var _resource, out error, kind))
            {
                resource = default;
                return false;
            }
            resource = (T) _resource;
            return true;
        }

        /// <summary>
        /// Finds a number of resources based on a search string. To include more than one resource in 
        /// the search, use the wildcard character (asterisk '*'). For example: To find all resources in a namespace
        /// 'MyApplication.Utilities', use the search string "myapplication.utilities.*" or any case 
        /// variant of it.
        /// </summary>
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// <returns></returns>
        public static IResource[] SafeFindMany(string searchString)
        {
            switch (searchString.Count(i => i == '*'))
            {
                case 0:
                    if (TryFind(searchString, out var resource, out _))
                        return new[] {resource};
                    return new IResource[0];
                case 1 when searchString.Last() != '*':
                    throw new Exception("Invalid resource string syntax. The asterisk must be the last character");
                case 1:
                    var commonPart = searchString.TrimEnd('*');
                    var commonPartDots = commonPart.Count(c => c == '.');
                    return RESTarConfig.ResourceByName
                        .Where(pair => pair.Key.StartsWith(commonPart, StringComparison.OrdinalIgnoreCase) &&
                                       pair.Key.Count(c => c == '.') == commonPartDots)
                        .Select(pair => pair.Value)
                        .ToArray();
                default: throw new Exception("Invalid resource string syntax. Can only include one asterisk (*)");
            }
        }

        /// <summary>
        /// Gets resource for a given type, based on the type's name
        /// </summary>
        public static IResource ByTypeName(string typeName)
        {
            return All.FirstOrDefault(r => string.Equals(r.Type.GetRESTarTypeName(), typeName, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Gets the resource for a given type, or throws an UnknownResource exception if there is no such resource
        /// </summary>
        public static IResource Get(Type type) => RESTarConfig.ResourceByType.SafeGet(type) ?? throw new UnknownResource(type.GetRESTarTypeName());

        /// <summary>
        /// Gets the resource for a given type or returns null if there is no such resource
        /// </summary>
        public static IResource SafeGet(Type type) => RESTarConfig.ResourceByType.SafeGet(type);

        /// <summary>
        /// Finds a resource by name (case insensitive) and throws an UnknownResource exception
        /// if no resource is found.
        /// </summary>
        public static IResource Get(string name) => RESTarConfig.ResourceByName.SafeGet(name) ?? throw new UnknownResource(name);

        /// <summary>
        /// Finds a resource by name (case insensitive) and returns null
        /// if no resource is found
        /// </summary>
        public static IResource SafeGet(string name) => RESTarConfig.ResourceByName.SafeGet(name);

        #endregion
    }

    /// <summary>
    /// A static generic class for manually getting RESTar resources by type
    /// </summary>
    public static class Resource<T> where T : class
    {
        /// <summary>
        /// Gets the terminal resource for a given type, and throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public static IResource<T> Get => RESTarConfig.ResourceByType.SafeGet(typeof(T)) as IResource<T>
                                          ?? throw new UnknownResource(typeof(T).GetRESTarTypeName());


        /// <summary>
        /// Gets the terminal resource for a given type or null if there is no such resource
        /// </summary>
        public static IResource<T> SafeGet => RESTarConfig.ResourceByType.SafeGet(typeof(T)) as IResource<T>;

        /// <summary>
        /// Gets the resource specifier for a given resource
        /// </summary>
        public static string ResourceSpecifier => Get.Name;
    }
}