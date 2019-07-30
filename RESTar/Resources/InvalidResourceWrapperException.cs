using System;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid resource wrapper declaration
    /// </summary>
    public class InvalidResourceWrapperException : RESTarException
    {
        internal InvalidResourceWrapperException((Type wrapper, Type wrapped) types, string message) : base(ErrorCodes.ResourceWrapperError,
            $"Invalid resource wrapper declaration '{types.wrapper.RESTarTypeName()}' for wrapped type '{types.wrapped.RESTarTypeName()}'. Resource wrappers {message}") { }
    }
}