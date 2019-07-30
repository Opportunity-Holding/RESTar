using System;
using System.IO;

namespace RESTar.Results
    {
        /// <inheritdoc cref="IResult" />
        /// <inheritdoc cref="IDisposable" />
        /// <summary>
        /// Represents a result that is ready to be sent back to the client, for example 
        /// in an HTTP response or a WebSocket message.
        /// </summary>
        public interface ISerializedResult : IResult
        {
            /// <summary>
            /// The serialized body contained in the result. Can be seekable or non-seekable.
            /// </summary>
            Stream Body { get; set; }
        }
    
        /// <inheritdoc cref="IResult{T}" />
        /// <inheritdoc cref="ISerializedResult" />
        /// <summary>
        /// Represents a result that is ready to be sent back to the client, for example 
        /// in an HTTP response or a WebSocket message.
        /// </summary>
        public interface ISerializedResult<T> : IResult<T>, ISerializedResult where T : class { }
    }