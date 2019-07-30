using System;

namespace RESTar.Meta
{
    /// <inheritdoc />
    /// <summary>
    /// A common interface describing a event resources
    /// </summary>
    public interface IEventResource : IResource
    {
        /// <summary>
        /// The type of the event payload
        /// </summary>
        Type PayloadType { get; }
    }

    /// <inheritdoc cref="IResource" />
    /// <inheritdoc cref="IEventResource" />
    /// <summary>
    /// A common generic interface describing event resources
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <typeparam name="TPayload"></typeparam>
    public interface IEventResource<TEvent, TPayload> : IResource<TEvent>, IEventResource where TEvent : class where TPayload : class
    {
        /// <summary>
        /// An ITarget describing the payload type
        /// </summary>
        ITarget<TPayload> PayloadTarget { get; }
    }
}