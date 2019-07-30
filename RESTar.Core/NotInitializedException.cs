using System;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters a call to an operation that cannot be executed
    /// before RESTarConfig.Init() has been called.
    /// </summary>
    public class NotInitializedException : RESTarException
    {
        internal NotInitializedException(string message, Exception ie = null) : base(ErrorCodes.NotInitialized, message, ie) { }
    }
}