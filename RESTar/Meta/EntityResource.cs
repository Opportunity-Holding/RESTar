using System;
using RESTar.Results;

namespace RESTar.Meta
{
    /// <summary>
    /// A static class for manually getting RESTar entity resources by type
    /// </summary>
    public static class EntityResource
    {
        /// <summary>
        /// Gets the entity resource for a given type, or throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public static IEntityResource Get(Type type) => RESTarConfig.ResourceByType.SafeGet(type) as IEntityResource
                                                        ?? throw new UnknownResource(type.RESTarTypeName());

        /// <summary>
        /// Gets the entity resource for a given type or returns null if there is no such resource
        /// </summary>
        public static IEntityResource SafeGet(Type type) => RESTarConfig.ResourceByType.SafeGet(type) as IEntityResource;

        /// <summary>
        /// Gets the entity resource with the given name, or throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public static IEntityResource Get(string name) => RESTarConfig.ResourceByName.SafeGet(name) as IEntityResource
                                                          ?? throw new UnknownResource(name);

        /// <summary>
        /// Gets the entity resource with the given name or returns null if there is no such resource
        /// </summary>
        public static IEntityResource SafeGet(string name) => RESTarConfig.ResourceByName.SafeGet(name) as IEntityResource;
    }

    /// <summary>
    /// A static generic class for manually getting RESTar entity resources by type
    /// </summary>
    /// <typeparam name="T">The type to register</typeparam>
    public static class EntityResource<T> where T : class
    {
        /// <summary>
        /// Gets the entity resource for a given type, or throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public static IEntityResource<T> Get => RESTarConfig.ResourceByType.SafeGet(typeof(T)) as IEntityResource<T>
                                                ?? throw new UnknownResource(typeof(T).RESTarTypeName());

        /// <summary>
        /// Gets the entity resource for a given type or null if there is no such resource
        /// </summary>
        public static IEntityResource<T> SafeGet => RESTarConfig.ResourceByType.SafeGet(typeof(T)) as IEntityResource<T>;

        /// <summary>
        /// Gets the resource specifier for a given EntityResource
        /// </summary>
        public static string ResourceSpecifier => Get.Name;
    }
}