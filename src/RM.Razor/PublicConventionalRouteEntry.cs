namespace RM.Razor {

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.AspNetCore.Routing.Patterns;

    public readonly struct PublicConventionalRouteEntry {
        public readonly RoutePattern Pattern;
        public readonly string RouteName;
        public readonly RouteValueDictionary DataTokens;
        public readonly int Order;
        public readonly IReadOnlyList<Action<EndpointBuilder>> Conventions;

        public PublicConventionalRouteEntry(
            string routeName,
            string pattern,
            RouteValueDictionary defaults,
            IDictionary<string, object> constraints,
            RouteValueDictionary dataTokens,
            int order,
            List<Action<EndpointBuilder>> conventions) {
            RouteName = routeName;
            DataTokens = dataTokens;
            Order = order;
            Conventions = conventions;

            try {
                // Data we parse from the pattern will be used to fill in the rest of the constraints or
                // defaults. The parser will throw for invalid routes.
                Pattern = RoutePatternFactory.Parse(pattern, defaults, constraints);
            } catch (Exception exception) {
                throw new RouteCreationException(string.Format(
                    CultureInfo.CurrentCulture,
                    "An error occurred while creating the route with name '{0}' and pattern '{1}'.",
                    routeName,
                    pattern), exception);
            }
        }
    }
}