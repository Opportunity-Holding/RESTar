using System;
using RESTar.Requests;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// Defines the operations of something that can be logged
    /// </summary>
    public interface ILogable : ITraceable
    {
        /// <summary>
        /// The log event type
        /// </summary>
        MessageType MessageType { get; }

        /// <summary>
        /// The message to log
        /// </summary>
        string LogMessage { get; }

        /// <summary>
        /// The content to log
        /// </summary>
        string LogContent { get; }

        /// <summary>
        /// The headers of the logable entity
        /// </summary>
        Headers Headers { get; }

        /// <summary>
        /// A string cache of the headers
        /// </summary>
        string HeadersStringCache { get; set; }

        /// <summary>
        /// Should headers be excluded?
        /// </summary>
        bool ExcludeHeaders { get; }

        /// <summary>
        /// The date and time of this logable instance
        /// </summary>
        DateTime LogTime { get; }
    }
}