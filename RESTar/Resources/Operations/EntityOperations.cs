using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Results;

namespace RESTar.Resources.Operations
{
    internal static class EntityOperationExtensions
    {
        internal static IEnumerable<T> Validate<T>(this IEnumerable<T> e, IEntityResource<T> resource) where T : class => resource.Validate(e);
        internal static IEnumerable<T> InvokePostInsert<T>(this IEnumerable<T> e) where T : class => Events.EntityResource<T>.OnPostInsert(e);
        internal static IEnumerable<T> InvokePostUpdate<T>(this IEnumerable<T> e) where T : class => Events.EntityResource<T>.OnPostUpdate(e);
        internal static IEnumerable<T> InvokePreDelete<T>(this IEnumerable<T> e) where T : class => Events.EntityResource<T>.OnPreDelete(e);
    }

    internal static class EntityOperations<T> where T : class
    {
        private static IEnumerable<T> SelectFilter(IRequest<T> request) => request.Target
            .Select(request)?
            .Filter(request.MetaConditions.Distinct)
            .Filter(request.MetaConditions.Search)
            .Filter(request.MetaConditions.OrderBy)
            .Filter(request.MetaConditions.Offset)
            .Filter(request.MetaConditions.Limit);

        private static IEnumerable<object> SelectProcessFilter(IRequest<T> request) => request.Target
            .Select(request)?
            .Process(request.MetaConditions.Processors)
            .Filter(request.MetaConditions.Distinct)
            .Filter(request.MetaConditions.Search)
            .Filter(request.MetaConditions.OrderBy)
            .Filter(request.MetaConditions.Offset)
            .Filter(request.MetaConditions.Limit);

        private static IEnumerable<T> TrySelectFilter(IEntityRequest<T> request)
        {
            var producer = request.EntitiesProducer;
            try
            {
                request.EntitiesProducer = () => new T[0];
                return SelectFilter(request);
            }
            catch (InfiniteLoop)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedSelect, e);
            }
            finally
            {
                request.EntitiesProducer = producer;
            }
        }

        private static IEnumerable<object> TrySelectProcessFilter(IEntityRequest<T> request)
        {
            var producer = request.EntitiesProducer;
            try
            {
                request.EntitiesProducer = () => new T[0];
                if (!request.MetaConditions.HasProcessors)
                    return SelectFilter(request);
                return SelectProcessFilter(request);
            }
            catch (InfiniteLoop)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedSelect, e);
            }
            finally
            {
                request.EntitiesProducer = producer;
            }
        }

        private static long TryCount(IEntityRequest<T> request)
        {
            try
            {
                request.EntitiesProducer = () => new T[0];
                if (request.EntityResource.CanCount &&
                    request.MetaConditions.CanUseExternalCounter)
                    return request.EntityResource.Count(request);
                if (!request.MetaConditions.HasProcessors)
                    return SelectFilter(request)?.Count() ?? 0L;
                return SelectProcessFilter(request)?.Count() ?? 0L;
            }
            catch (InfiniteLoop)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedReport, e);
            }
        }

        private static int Insert(IEntityRequest<T> request, bool limit = false)
        {
            try
            {
                request.EntitiesProducer = () =>
                    (request.GetSelector() ?? (() => request.GetBody().Deserialize<T>()))()?
                    .InputLimit(limit)?
                    .Validate(request.EntityResource)?
                    .InvokePostInsert() ?? throw new MissingDataSource(request);
                return request.EntityResource.Insert(request);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedInsert, e);
            }
        }

        private static int Update(IEntityRequest<T> request)
        {
            try
            {
                request.EntitiesProducer = () =>
                    (request.GetUpdater() ?? (e => request.GetBody().PopulateTo(e)))(
                        (request.GetSelector() ?? (() => TrySelectFilter(request) ?? new T[0]))
                        .Invoke()?
                        .UnsafeLimit(!request.MetaConditions.Unsafe))?
                    .Validate(request.EntityResource)?
                    .InvokePostUpdate() ?? throw new MissingDataSource(request);
                return request.EntityResource.Update(request);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedUpdate, e);
            }
        }

        private static int SafePostUpdate(IEntityRequest<T> request, ICollection<(JObject json, T source)> items)
        {
            try
            {
                request.EntitiesProducer = () => items.Select(item =>
                    {
                        if (item.json == null || item.source == null)
                            return item.source;
                        using (var sr = item.json.CreateReader())
                            JsonProvider.Serializer.Populate(sr, item.source);
                        return item.source;
                    })
                    .Validate(request.EntityResource)?
                    .InvokePostUpdate();
                return request.EntityResource.Update(request);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedUpdate, e);
            }
        }

        private static int Delete(IEntityRequest<T> request)
        {
            try
            {
                request.EntitiesProducer = () => (request.GetSelector() ?? (() => TrySelectFilter(request) ?? new T[0]))()?
                                                 .UnsafeLimit(!request.MetaConditions.Unsafe)?
                                                 .InvokePreDelete() ?? new T[0];
                return request.EntityResource.Delete(request);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedDelete, e);
            }
        }

        #region Operation resolvers

        internal static Func<IEntityRequest<T>, RequestSuccess> GetEvaluator(Method method)
        {
            switch (method)
            {
                case Method.GET:
                    return request =>
                    {
                        var entities = TrySelectProcessFilter(request);
                        if (entities == null)
                            return MakeEntities(request, default(IEnumerable<T>));
                        return MakeEntities(request, (dynamic) entities);
                    };
                case Method.POST:
                    return request => request.MetaConditions.SafePost == null
                        ? new InsertedEntities(request, Insert(request))
                        : SafePOST(request);
                case Method.PUT:
                    return request =>
                    {
                        var source = TrySelectFilter(request)?.InputLimit()?.ToList();
                        switch (source?.Count)
                        {
                            case null:
                            case 0: return new InsertedEntities(request, Insert(request));
                            case 1 when request.GetUpdater() == null && !request.GetBody().HasContent:
                                return new UpdatedEntities(request, 0);
                            default:
                                request.Selector = () => source;
                                return new UpdatedEntities(request, Update(request));
                        }
                    };
                case Method.HEAD:
                    return request =>
                    {
                        var count = TryCount(request);
                        if (count > 0) return new Head(request, count);
                        return new NoContent(request);
                    };
                case Method.PATCH: return request => new UpdatedEntities(request, Update(request));
                case Method.DELETE: return request => new DeletedEntities(request, Delete(request));
                case Method.REPORT: return request => new Report(request, TryCount(request));
                default: return request => new ImATeapot(request);
            }
        }

        /// <summary>
        /// Needed since some <see cref="Entities{T}"/> instances are created using dynamic binding, which requires
        /// a separate static method in a non-generic class.
        /// </summary>
        private static Entities<T1> MakeEntities<T1>(IRequest r, IEnumerable<T1> e) where T1 : class => new Entities<T1>(r, e);

        #endregion

        #region SafePost

        private static RequestSuccess SafePOST(IEntityRequest<T> request)
        {
            try
            {
                var innerRequest = (IEntityRequest<T>) request.Context.CreateRequest<T>();
                var (toInsert, toUpdate) = GetSafePostTasks(request, innerRequest);
                var (updatedCount, insertedCount) = (0, 0);
                if (toUpdate.Any())
                    updatedCount = SafePostUpdate(innerRequest, toUpdate);
                if (toInsert.Any())
                {
                    innerRequest.Selector = () => toInsert.Select(item => item.ToObject<T>(JsonProvider.Serializer));
                    insertedCount = Insert(innerRequest);
                }
                return new SafePostedEntities(request, updatedCount, insertedCount);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedInsert, e, e.Message);
            }
        }

        private static (List<JObject> ToInsert, IList<(JObject json, T source)> ToUpdate) GetSafePostTasks(IEntityRequest<T> request,
            IEntityRequest<T> innerRequest)
        {
            var toInsert = new List<JObject>();
            var toUpdate = new List<(JObject json, T source)>();
            try
            {
                var body = request.GetBody();
                if (!body.HasContent) return (toInsert, toUpdate);
                var conditions = request.MetaConditions.SafePost
                    .Split(',')
                    .Select(s => new Condition<T>(s, Operators.EQUALS, null))
                    .ToList();
                foreach (var entity in body.Deserialize<JObject>())
                {
                    conditions.ForEach(cond => cond.Value = entity.SafeSelect(cond.Term.Evaluate));
                    innerRequest.Conditions = conditions;
                    var results = innerRequest.Evaluate().ToEntities<T>().ToList();
                    switch (results.Count)
                    {
                        case 0:
                            toInsert.Add(entity);
                            break;
                        case 1:
                            toUpdate.Add((entity, results[0]));
                            break;
                        case var multiple:
                            throw new SafePostAmbiguousMatch
                            (
                                count: multiple,
                                uri: innerRequest.UriComponents.ToUriString()
                            );
                    }
                }
                return (toInsert, toUpdate);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedInsert, e, e.Message);
            }
        }

        #endregion
    }
}