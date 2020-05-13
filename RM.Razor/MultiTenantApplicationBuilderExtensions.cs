namespace RM.Razor {

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using RM.Razor.StaticFiles;
    using System;


    /// <summary>
    /// Extension methods for the StaticFileMiddleware
    /// </summary>
    public static class MultiTenantApplicationBuilderExtensions {
        /// <summary>
        /// Enables static file serving for the current request path
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseMultiTenantStaticFiles(this IApplicationBuilder app) {
            if (app == null) {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<MultiTenantStaticFilesMiddleware>();
        }

        /// <summary>
        /// Enables static file serving for the given request path
        /// </summary>
        /// <param name="app"></param>
        /// <param name="requestPath">The relative request path.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseMultiTenantStaticFiles(this IApplicationBuilder app, string requestPath) {
            if (app == null) {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseStaticFiles(new StaticFileOptions {
                RequestPath = new PathString(requestPath)
            });
        }

        /// <summary>
        /// Enables static file serving with the given options
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseMultiTenantStaticFiles(this IApplicationBuilder app, StaticFileOptions options) {
            if (app == null) {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<MultiTenantStaticFilesMiddleware>(Options.Create(options));
        }





    }
}