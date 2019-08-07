using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;
using RESTar.Requests;
using RESTar.Resources.Operations;
using RESTar.Linq;
using RESTar.Resources;

namespace RESTar.AspNetCore
{
    [RESTar(Method.GET)]
    public class Route : ISelector<Route>
    {
        public string Name { get; set; }
        public string Template { get; set; }
        public string[] MethodRestrictions { get; set; }

        public IEnumerable<Route> Select(IRequest<Route> request) => GetRoutes().Where(request.Conditions);

        private static IEnumerable<Route> GetRoutes()
        {
            var routeCollection = Application.Services.GetService<IHttpContextAccessor>()
                .HttpContext.GetRouteData()
                .Routers.OfType<RouteCollection>()
                .First();
            for (var i = 0; i < routeCollection.Count; i += 1)
            {
                var aspNetCoreRoute = (Microsoft.AspNetCore.Routing.Route) routeCollection[i];
                var methodConstraint = (HttpMethodRouteConstraint) aspNetCoreRoute.Constraints.SafeGet("httpMethod");
                yield return new Route
                {
                    Name = aspNetCoreRoute.Name,
                    Template = aspNetCoreRoute.RouteTemplate,
                    MethodRestrictions = methodConstraint?.AllowedMethods.ToArray()
                };
            }
        }
    }
}