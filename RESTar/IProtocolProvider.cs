﻿using System.Collections.Generic;
using RESTar.Operations;
using RESTar.Requests;

namespace RESTar
{
    /// <summary>
    /// Interface for RESTar protocol providers. Protocol providers provide the logic for 
    /// parsing requests according to some protocol.
    /// </summary>
    public interface IProtocolProvider
    {
        /// <summary>
        /// The identifier is used in request URIs to indicate the protocol to use. If the ProtocolIdentifer 
        /// is 'OData', for example, and RESTar runs locally, on port 8282 and with root URI "/rest" requests 
        /// can trigger the OData protocol by "127.0.0.1:8282/rest-odata",
        /// </summary>
        string ProtocolIdentifier { get; }

        /// <summary>
        /// Should the protocol provider allow external content type providers, or only the ones specified in the 
        /// GetContentTypeProviders method?
        /// </summary>
        bool AllowExternalContentProviders { get; }

        /// <summary>
        /// Gets the content type providers associated with this protocol provider. If this is the exclusive list 
        /// of content type providers to use with this protocol, set the AllowExternalContentProviders to false.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IContentTypeProvider> GetContentTypeProviders();

        /// <summary>
        /// Reads a query string, which is everyting after the root URI in the full request URI, parses 
        /// its content according to some protocol and populates the URI object.
        /// </summary>
        void ParseQuery(string query, URI uri);

        /// <summary>
        /// If headers are used to check protocol versions, for example, this method allows the 
        /// protocolprovider to throw an exception and abort a request if the request is not 
        /// in compliance with the protocol.
        /// </summary>
        void CheckCompliance(Context context);

        /// <summary>
        /// The protocol needs to be able to generate a relative URI string from an IUriParameters instance. 
        /// Note that only components added to a URI in ParseQuery can be present in the IUriParameters instance.
        /// </summary>
        string MakeRelativeUri(IUriParameters parameters);

        /// <summary>
        /// Takes a result and generates an IFinalizedResult entity from it, that can be returned 
        /// to the network component.
        /// </summary>
        IFinalizedResult FinalizeResult(IResult result, ContentType accept, IContentTypeProvider contentTypeProvider);
    }
}