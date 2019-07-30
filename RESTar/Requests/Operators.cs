using System;

#pragma warning disable 1591

namespace RESTar.Requests
{
    /// <summary>
    /// An enumeration of available condition operators
    /// </summary>
    [Flags]
    public enum Operators
    {
        None = 0,
        EQUALS = 1 << 0,
        NOT_EQUALS = 1 << 1,
        LESS_THAN = 1 << 2,
        GREATER_THAN = 1 << 3,
        LESS_THAN_OR_EQUALS = 1 << 4,
        GREATER_THAN_OR_EQUALS = 1 << 5,
        All = ~None
    }
}