using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RM.Razor;
using System;
using System.Threading.Tasks;

namespace RM.Mvc.Middleware {
    public class CookieBasedViewLibrarySelectorMiddleware {

        public const string CookieKey = "vl";
        private readonly RequestDelegate _next;
        private readonly MultiTenantRazorViewEngineOptions _options;

        public CookieBasedViewLibrarySelectorMiddleware(RequestDelegate next, IOptions<MultiTenantRazorViewEngineOptions> optionsAccessor) {
            _next = next;
            _options = optionsAccessor?.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));
        }

        public Task InvokeAsync(HttpContext context) {

            if (context.Request.Cookies.TryGetValue(CookieKey, out string viewLibrary)) {
                context.Items.Add(_options.HttpContextItemsKey, viewLibrary);
            }

            return this._next(context);

        }
    }
}
