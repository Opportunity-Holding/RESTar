﻿using System;
using RESTar.Resources;
using RESTar.Results;

namespace RESTar.Meta
{
    /// <summary>
    /// A static generic class for manually getting RESTar terminal resources by type
    /// </summary>
    public static class TerminalResource<T> where T : class, ITerminal
    {
        /// <summary>
        /// Gets the terminal resource for a given type, and throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public static ITerminalResource<T> Get => RESTarConfig.ResourceByType.SafeGet(typeof(T)) as ITerminalResource<T>
                                                  ?? throw new UnknownResource(typeof(T).RESTarTypeName());


        /// <summary>
        /// Gets the terminal resource for a given type or null if there is no such resource
        /// </summary>
        public static ITerminalResource<T> SafeGet => RESTarConfig.ResourceByType.SafeGet(typeof(T)) as ITerminalResource<T>;

        /// <summary>
        /// Gets the resource specifier for a given terminal resource
        /// </summary>
        public static string ResourceSpecifier => Get.Name;
    }

    /// <summary>
    /// A static generic class for manually getting RESTar event resources by type
    /// </summary>
    public static class EventResource<TEvent, TPayload> where TEvent : Event<TPayload> where TPayload : class
    {
        /// <summary>
        /// Gets the terminal resource for a given type, and throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public static IEventResource<TEvent, TPayload> Get => RESTarConfig.ResourceByType.SafeGet(typeof(TEvent)) as IEventResource<TEvent, TPayload>
                                                              ?? throw new UnknownResource(typeof(TEvent).RESTarTypeName());

        /// <summary>
        /// Gets the terminal resource for a given type or null if there is no such resource
        /// </summary>
        public static IEventResource<TEvent, TPayload> SafeGet =>
            RESTarConfig.ResourceByType.SafeGet(typeof(TEvent)) as IEventResource<TEvent, TPayload>;

        /// <summary>
        /// Gets the resource specifier for a given terminal resource
        /// </summary>
        public static string ResourceSpecifier => Get.Name;
    }

    /// <summary>
    /// A static generic class for manually getting RESTar event resources by type
    /// </summary>
    public static class EventResource
    {
        /// <summary>
        /// Gets the terminal resource for a given type, and throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public static IEventResource Get(Type type) => RESTarConfig.ResourceByType.SafeGet(type) as IEventResource
                                                       ?? throw new UnknownResource(type.RESTarTypeName());

        /// <summary>
        /// Gets the terminal resource for a given type or null if there is no such resource
        /// </summary>
        public static IEventResource SafeGet(Type type) => RESTarConfig.ResourceByType.SafeGet(type) as IEventResource;
    }
}