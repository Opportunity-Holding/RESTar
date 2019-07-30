using System.Collections.Generic;
using RESTar.ProtocolProviders;

namespace RESTar.Requests
{
    /// <summary>
    /// Describes the components of a RESTar URI
    /// </summary>
    public interface IUriComponents
    {
        /// <summary>
        /// Specifies the resource for the request
        /// </summary>
        string ResourceSpecifier { get; }

        /// <summary>
        /// Specifies the view for the request
        /// </summary>
        string ViewName { get; }

        /// <summary>
        /// Specifies the conditions for the request
        /// </summary>
        IReadOnlyCollection<IUriCondition> Conditions { get; }

        /// <summary>
        /// Specifies the meta-conditions for the request
        /// </summary>
        IReadOnlyCollection<IUriCondition> MetaConditions { get; }

        /// <summary>
        /// The macro, if any, belonging to these uri components
        /// </summary>
        IMacro Macro { get; }

        /// <summary>
        /// The protocol provider specified in the uri string
        /// </summary>
        IProtocolProvider ProtocolProvider { get; }
    }

    /// <inheritdoc cref="IUriComponents" />
    /// <summary>
    /// Defines the operations of a RESTar macro
    /// </summary>
    public interface IMacro : IUriComponents
    {
        /// <summary>
        /// The name of the macro
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Does this macro contain a body?
        /// </summary>
        bool HasBody { get; }

        /// <summary>
        /// The body of the macro, as byte array
        /// </summary>
        byte[] Body { get; }

        /// <summary>
        /// The content type of the body of the macro
        /// </summary>
        ContentType ContentType { get; }

        /// <summary>
        /// The headers of the macro
        /// </summary>
        IHeaders Headers { get; }

        /// <summary>
        /// Should the macro overwrite the body of the calling request?
        /// </summary>
        bool OverwriteBody { get; }

        /// <summary>
        /// Should the macro overwrite matching headers in the calling request?
        /// </summary>
        bool OverwriteHeaders { get; }
    }
}