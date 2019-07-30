using System;

namespace RESTar.Resources
{
    /// <summary>
    /// Describes a dynamic entity resource
    /// </summary>
    public interface IProceduralEntityResource
    {
        /// <summary>
        /// The name, including namespace, of this resource
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The description to use for the resource
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The methods to enable for the resource
        /// </summary>
        Method[] Methods { get; }

        /// <summary>
        /// The type to bind to this resource. Must be unique for this resource.
        /// </summary>
        Type Type { get; }
    }
}