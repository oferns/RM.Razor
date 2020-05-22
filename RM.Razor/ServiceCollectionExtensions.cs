namespace RM.Razor {

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.Razor.Compilation;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Options;

    public static class ServiceCollectionExtensions {

        // Swaps out the Razor View engine and compiler for ours
        // and adds config to the StaticFiles options to use files specific
        // to the loaded view library, if configured.
        public static IServiceCollection AddMultiViewEngine(this IServiceCollection services) {

            services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<RazorMultiViewEngineOptions>, RazorMultiViewEngineOptionsSetup>());
            services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<StaticFileOptions>, StaticFileOptionsPostConfigure>());
            
            return services
                .AddSingleton<IRazorViewEngine, RazorMultiViewEngine>()
                .AddSingleton<IViewCompilerProvider, RazorMultiViewCompilerProvider>();
        }

    }
}