namespace RM.Razor.Mvc.Middleware {

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using System;
    using System.Threading.Tasks;

    public class HostBasedViewLibrarySelectorMiddleware {

        private readonly RequestDelegate _next;
        private readonly RazorMultiViewEngineOptions _options;

        public HostBasedViewLibrarySelectorMiddleware(RequestDelegate next, IOptions<RazorMultiViewEngineOptions> optionsAccessor) {
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