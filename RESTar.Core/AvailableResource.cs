using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Meta.Internal;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using static RESTar.Method;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// A resource that generates a list of the available resources for the current user
    /// </summary>
    [RESTar(GET, Description = description, GETAvailableToAll = true)]
    public sealed class AvailableResource : ISelector<AvailableResource>
    {
        private const string description = "The AvailableResource resource contains all resources " +
                                           "available for the current user, as defined by the access " +
                                           "rights assigned to its API key. It is the default resource " +
                                           "used when no resource is specified in the request URI.";

        /// <summary>
        /// The name of the resource
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Resource descriptions are visible in the AvailableMethods resource
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The methods that have been enabled for this resource
        /// </summary>
        public Method[] Methods { get; set; }

        /// <summary>
        /// The resource type, entity resource or terminal resource
        /// </summary>
        public ResourceKind Kind { get; set; }

        /// <summary>
        /// The views for this resource
        /// </summary>
        [RESTarMember(hideIfNull: true)] public ViewInfo[] Views { get; private set; }

        /// <summary>
        /// Inner resources for this resource
        /// </summary>
        [RESTarMember(hideIfNull: true)] public AvailableResource[] InnerResources { get; private set; }

        /// <inheritdoc />
        public IEnumerable<AvailableResource> Select(IRequest<AvailableResource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return request.Context.Client.AccessRights.Keys?
                .Where(r => r.IsGlobal && !r.IsInnerResource)
                .OrderBy(r => r.Name)
                .Select(r => Make(r, request))
                .Where(request.Conditions);
        }

        /// <inheritdoc />
        /// <summary>
        /// Returns all the resources that are declared within a given namespace
        /// </summary>
        [RESTarView]
        public class InNamespace : ISelector<AvailableResource>
        {
            /// <summary>
            /// The namespace to match against
            /// </summary>
            public string Namespace { get; set; }

            /// <inheritdoc />
            public IEnumerable<AvailableResource> Select(IRequest<AvailableResource> request)
            {
                var @namespace = request.Conditions.Get(nameof(Namespace), Operators.EQUALS).Value as string;
                if (@namespace != null)
                    if (!@namespace.EndsWith("."))
                        @namespace = @namespace + ".";
                if (@namespace == null)
                    return request.Context.Client.AccessRights.Keys?
                        .Where(r => r.IsGlobal && !r.IsInnerResource)
                        .OrderBy(r => r.Name)
                        .Select(r => Make(r, request))
                        .Where(request.Conditions);
                return request.Context.Client.AccessRights.Keys?
                    .Where(r => r.IsGlobal && !r.IsInnerResource)
                    .Where(r => r.Name.StartsWith(@namespace, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(r => r.Name)
                    .Select(r => Make(r, request))
                    .Where(request.Conditions);
            }
        }

        internal static AvailableResource Make(IResource iresource, ITraceable trace) => new AvailableResource
        {
            Name = iresource.Name,
            Description = iresource.Description ?? "No description",
            Methods = trace.Context.Client.AccessRights.SafeGet(iresource)?
                          .Intersect(iresource.AvailableMethods)
                          .ToArray() ?? new Method[0],
            Kind = iresource.ResourceKind,
            Views = iresource is IEntityResource er
                ? er.Views?.Select(v => new ViewInfo(v.Name, v.Description ?? "No description")).ToArray()
                  ?? new ViewInfo[0]
                : null,
            InnerResources = ((IResourceInternal) iresource).InnerResources?.Select(r => Make(r, trace)).ToArray()
        };
    }
}