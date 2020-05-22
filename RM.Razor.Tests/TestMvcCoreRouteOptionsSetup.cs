namespace RM.Razor.Tests {

    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Options;
    using System;

    public class PublicMvcCoreRouteOptionsSetup : IConfigureOptions<RouteOptions> {
        /// <summary>
        /// Configures the <see cref="RouteOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="RouteOptions"/>.</param>
        public void Configure(RouteOptions options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            options.ConstraintMap.Add("exists", typeof(KnownRouteValueConstraint));
        }
    }
}