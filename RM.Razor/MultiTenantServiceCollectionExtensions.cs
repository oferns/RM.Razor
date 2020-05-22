namespace RM.Razor {
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.Razor.Compilation;
    using Microsoft.AspNetCore.Mvc.Razor.Extensions;
    using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
    using Microsoft.AspNetCore.Razor.Language;
    using Microsoft.CodeAnalysis.Razor;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;    
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class MultiTenantServiceCollectionExtensions {

        // Swaps out the Razor View engine and compiler for ours
        public static IServiceCollection AddMultiTenantViewEgine(this IServiceCollection services) {

            services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<RazorMultiViewEngineOptions>, RazorMultiViewEngineOptionsSetup>());
            services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<StaticFileOptions>, StaticFileOptionsPostConfigure>());
            
            return services
                .AddSingleton<IRazorViewEngine, RazorMultiViewEngine>()
                .AddSingleton<IViewCompilerProvider, MultiTenantViewCompilerProvider>();
        }

    }
}