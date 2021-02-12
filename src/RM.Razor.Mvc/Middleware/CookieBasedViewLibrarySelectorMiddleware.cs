using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace RM.Razor.Mvc.Middleware {
    public class CookieBasedViewLibrarySelectorMiddleware {

        public const string CookieKey = "vl";
        private readonly RequestDelegate _next;
        private readonly RazorMultiViewEngineOptions _options;

        public CookieBasedViewLibrarySelectorMiddleware(RequestDelegate next, IOptions<RazorMultiViewEngineOptions> optionsAccessor) {
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
