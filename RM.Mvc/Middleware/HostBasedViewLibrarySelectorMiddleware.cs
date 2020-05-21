using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RM.Razor;
using System;
using System.Threading.Tasks;

namespace RM.Mvc.Middleware {

    public class HostBasedViewLibrarySelectorMiddleware {

        private readonly RequestDelegate _next;
        private readonly MultiTenantRazorViewEngineOptions _options;

        public HostBasedViewLibrarySelectorMiddleware(RequestDelegate next, IOptions<MultiTenantRazorViewEngineOptions> optionsAccessor) {
            _next = next;
            _options = optionsAccessor?.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));
        }

        public Task InvokeAsync(HttpContext context) {
            if (!context.Items.ContainsKey(_options.HttpContextItemsKey)) {
                context.Items.Add(_options.HttpContextItemsKey, context.Request.Host.Host);
            }
            return this._next(context);
        }
    }
}