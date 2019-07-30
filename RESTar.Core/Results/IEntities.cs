using System;
using System.Collections;
using System.Collections.Generic;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc cref="IEnumerable" />
    /// <inheritdoc cref="IResult" />
    /// <inheritdoc cref="ISerializedResult" />
    /// <summary>
    /// A non-generic interface for a collection of result entities from a RESTar request
    /// </summary>
    public interface IEntities : IEnumerable, IResult, ISerializedResult
    {
        /// <summary>
        /// The number of entities in the collection. Should be set by the serializer, since it is unknown
        /// until the collection is iterated.
        /// </summary>
        ulong EntityCount { get; set; }

        /// <summary>
        /// Is this result paged?
        /// </summary>
        bool IsPaged { get; }

        /// <summary>
        /// Helper method for setting the Content-Disposition headers of the result to an appropriate file
        /// attachment. 
        /// </summary>
        /// <param name="extension">The file extension to use, for example .xlsx</param>
        void SetContentDisposition(string extension);

        /// <summary>
        /// The request that generated this result
        /// </summary>
        IRequest Request { get; }

        /// <summary>
        /// The type of entities in the entity collection
        /// </summary>
        Type EntityType { get; }
    }

    /// <inheritdoc cref="IEntities" />
    /// <inheritdoc cref="IEnumerable{T}" />
    /// <summary>
    /// A generic interface for a collection of result entities from a RESTar request
    /// </summary>
    /// <typeparam name="T">The entity type contained in the entity collection</typeparam>
    public interface IEntities<out T> : IEntities, IEnumerable<T> where T : class { }
}