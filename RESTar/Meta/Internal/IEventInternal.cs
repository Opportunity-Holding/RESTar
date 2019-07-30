using System;
using RESTar.Requests;

namespace RESTar.Meta.Internal
{
    internal interface IEventInternal<out T> : IEvent, IDisposable where T : class
    {
        string Name { get; }
        T Payload { get; }
        ContentType? NativeContentType { get; }
        bool HasBinaryPayload { get; }
    }
}