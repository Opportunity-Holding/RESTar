using System;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error with permissions for binding 
    /// against, for example, properties of resources.
    /// </summary>
    internal class BinderPermissions : Internal
    {
        /// <inheritdoc />
        public BinderPermissions(Exception e) : base(ErrorCodes.FailedBinding, 
            "RESTar failed to resolve a reference to a property or type.", e) { }
    }
}