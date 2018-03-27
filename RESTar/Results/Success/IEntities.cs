﻿using System.Collections.Generic;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    /// <inheritdoc cref="IEnumerable{T}" />
    /// <inheritdoc cref="IResult" />
    /// <inheritdoc cref="ISerializedResult" />
    public interface IEntities<out T> : IEnumerable<T>, IResult, ISerializedResult where T : class
    {
        /// <summary>
        /// The number of entitites in the collection. Should be set by the serializer, since it is unknown
        /// until the collection is iterated.
        /// </summary>
        ulong EntityCount { get; set; }

        /// <summary>
        /// Is this result paged?
        /// </summary>
        bool IsPaged { get; }

        /// <summary>
        /// Gets a link to the next set of entities, with a given number of entities to include
        /// </summary>
        IUriComponents GetNextPageLink(int count);

        /// <summary>
        /// Gets a link to the next set of entities, with the same amount of entities as in the last one
        /// </summary>
        IUriComponents GetNextPageLink();

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
    }
}