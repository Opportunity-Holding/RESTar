using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RESTar.Admin;
using RESTar.Meta;
using RESTar.Requests;
using Starcounter;

namespace RESTar.Resources.Operations
{
    internal static class DelegateMaker
    {
        private static Type MatchingInterface<TDelegate>() where TDelegate : Delegate
        {
            var t = typeof(TDelegate).GetGenericArguments().ElementAtOrDefault(0);
            switch (typeof(TDelegate))
            {
                case var d when d == typeof(Selector<>).MakeGenericType(t): return typeof(ISelector<>).MakeGenericType(t);
                case var d when d == typeof(Inserter<>).MakeGenericType(t): return typeof(IInserter<>).MakeGenericType(t);
                case var d when d == typeof(Updater<>).MakeGenericType(t): return typeof(IUpdater<>).MakeGenericType(t);
                case var d when d == typeof(Deleter<>).MakeGenericType(t): return typeof(IDeleter<>).MakeGenericType(t);
                case var d when d == typeof(Counter<>).MakeGenericType(t): return typeof(ICounter<>).MakeGenericType(t);
                case var d when d == typeof(Profiler<>).MakeGenericType(t): return typeof(IProfiler<>).MakeGenericType(t);
                case var d when d == typeof(Authenticator<>).MakeGenericType(t): return typeof(IAuthenticatable<>).MakeGenericType(t);
                case var d when d == typeof(BinarySelector<>).MakeGenericType(t): return typeof(IBinary<>).MakeGenericType(t);
                case var d when d == typeof(ViewSelector<>).MakeGenericType(t): return typeof(ISelector<>).MakeGenericType(t);
                case var d when d == typeof(Validator<>).MakeGenericType(t): return typeof(IValidator<>).MakeGenericType(t);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        internal static Type MatchingInterface(RESTarOperations operation)
        {
            switch (operation)
            {
                case RESTarOperations.Select: return typeof(ISelector<>);
                case RESTarOperations.Insert: return typeof(IInserter<>);
                case RESTarOperations.Update: return typeof(IUpdater<>);
                case RESTarOperations.Delete: return typeof(IDeleter<>);
                default: throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
            }
        }

        /// <summary>
        /// Gets the given operations delegate from a given resource type definition
        /// </summary>
        internal static TDelegate GetDelegate<TDelegate>(Type target) where TDelegate : Delegate
        {
            var i = MatchingInterface<TDelegate>();
            if (!i.IsAssignableFrom(target)) return default;
            var method = target.GetInterfaceMap(i).TargetMethods.First();
            if (method.DeclaringType != target)
            {
                if (target.HasAttribute<DatabaseAttribute>())
                    throw new InvalidResourceDeclarationException($"Invalid resource declaration for type '{target}'. Starcounter " +
                                                                  "resource types cannot inherit operation implementations from " +
                                                                  "generic base types.");
                if (target.GetConstructor(Type.EmptyTypes) == null)
                    throw new InvalidResourceDeclarationException($"Invalid resource declaration for type '{target}'. Any resource " +
                                                                  "type inheriting operation implementations from a generic base " +
                                                                  "class must define a public parameterless constructor, due to " +
                                                                  "limitations in how CLR can bind delegates to these types.");
                var instance = Activator.CreateInstance(target);
                return (TDelegate) Delegate.CreateDelegate(typeof(TDelegate), instance, method);
            }
            return (TDelegate) Delegate.CreateDelegate(typeof(TDelegate), null, method);
        }
    }

    /// <summary>
    /// Specifies the Select operation used in GET from a view. Select gets a set 
    /// of entities from a resource that satisfy certain conditions provided in the request, 
    /// and returns them.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    internal delegate IEnumerable<T> ViewSelector<T>(IRequest<T> request) where T : class;

    /// <summary>
    /// Specifies the Select operation used in GET, PATCH, PUT and DELETE. Select gets a set 
    /// of entities from a resource that satisfy certain conditions provided in the request, 
    /// and returns them.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    internal delegate IEnumerable<T> Selector<T>(IRequest<T> request) where T : class;

    /// <summary>
    /// Specifies the Insert operation used in POST and PUT. Takes a set of entities and inserts 
    /// them into the resource, and returns the number of entities successfully inserted.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    internal delegate int Inserter<T>(IRequest<T> request) where T : class;

    /// <summary>
    /// Specifies the Update operation used in PATCH and PUT. Takes a set of entities and updates 
    /// their corresponding entities in the resource (often by deleting the old ones and inserting 
    /// the new), and returns the number of entities successfully updated.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    internal delegate int Updater<T>(IRequest<T> request) where T : class;

    /// <summary>
    /// Specifies the Delete operation used in DELETE. Takes a set of entities and deletes them from 
    /// the resource, and returns the number of entities successfully deleted.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    internal delegate int Deleter<T>(IRequest<T> request) where T : class;

    /// <summary>
    /// Counts the entities that satisfy certain conditions provided in the request
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    internal delegate long Counter<T>(IRequest<T> request) where T : class;

    /// <summary>
    /// Generates a profile for a given resource
    /// </summary>
    internal delegate ResourceProfile Profiler<T>(IEntityResource<T> resource) where T : class;

    /// <summary>
    /// Authenticates a request
    /// </summary>
    internal delegate AuthResults Authenticator<T>(IRequest<T> request) where T : class;

    /// <summary>
    /// Selects a stream and content type for a binary resource
    /// </summary>
    internal delegate (Stream stream, ContentType contentType) BinarySelector<T>(IRequest<T> request) where T : class;

    /// <summary>
    /// Defines an operation that processes an <see cref="IEnumerable{T}"/> and returns a new
    /// updated enumeration.
    /// </summary>
    public delegate IEnumerable<T> EntityProcessor<T>(IEnumerable<T> entities);

    /// <summary>
    /// Defines the operation of validating an entity resource entity
    /// </summary>
    public delegate bool Validator<in T>(T entity, out string invalidReason) where T : class;
}