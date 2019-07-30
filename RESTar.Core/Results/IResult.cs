using System;
using System.Net;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// A generic RESTar result
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public interface IResult<T> : IResult where T : class
    {
        /// <summary>
        /// The entities contained in the result
        /// </summary>
        IEntities<T> Entities { get; }

        /// <summary>
        /// Serializes the result and prepares output streams and content types.
        /// Optionally, provide a content type to serialize the result with.
        /// If null, the content type specified in the request will be used.
        /// If no content type is specified in the request, the default content 
        /// type for the protocol is used.
        /// </summary>
        new ISerializedResult<T> Serialize(ContentType? contentType = null);
    }

    /// <inheritdoc cref="ILogable" />
    /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// A RESTar result
    /// </summary>
    public interface IResult : ILogable, IDisposable
    {
        /// <summary>
        /// The status code of the result
        /// </summary>
        HttpStatusCode StatusCode { get; }

        /// <summary>
        /// The description of the status
        /// </summary>
        string StatusDescription { get; }

        /// <summary>
        /// The cookies to set in the response
        /// </summary>
        Cookies Cookies { get; }

        /// <summary>
        /// Is this a successful result?
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// Is this an error result?
        /// </summary>
        bool IsError { get; }

        /// <summary>
        /// If the result is non-successful, invoking this method will throw an 
        /// exception containing the erroneous result.
        /// </summary>
        void ThrowIfError();

        /// <summary>
        /// Serializes the result and prepares output streams and content types.
        /// Optionally, provide a content type to serialize the result with.
        /// If null, the content type specified in the request will be used.
        /// If no content type is specified in the request, the default content 
        /// type for the protocol is used.
        /// </summary>
        ISerializedResult Serialize(ContentType? contentType = null);

        /// <summary>
        /// Tries to convert the result to an IEnumerable instance, or throws an 
        /// Exception if the result is non-successful or cannot be cast to the given type.
        /// </summary>
        IEntities<T> ToEntities<T>() where T : class;

        /// <summary>
        /// The time it took for RESTar to generate the response.
        /// </summary>
        TimeSpan TimeElapsed { get; }

        /// <summary>
        /// Has this result been serialized?
        /// </summary>
        bool IsSerialized { get; }

        /// <summary>
        /// The metadata for this result, for use in the RESTar-metadata header in remote requests.
        /// </summary>
        string Metadata { get; }
    }
}