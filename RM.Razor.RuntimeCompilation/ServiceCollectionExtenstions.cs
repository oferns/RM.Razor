namespace RM.Razor.RuntimeCompilation {

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.Razor.Compilation;
    using Microsoft.AspNetCore.Mvc.Razor.Extensions;
    using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
    using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
    using Microsoft.AspNetCore.Razor.Language;
    using Microsoft.CodeAnalysis.Razor;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.IO;

    public static class ServiceCollectionExtenstions {

        // Adds Runtime Compilation support
        public static IServiceCollection AddMultiTenantRuntimeCompilation(this IServiceCollection services, IWebHostEnvironment environment) {

            services.AddSingleton<PublicActionEndpointFactory>();
            services.AddSingleton<PageLoader, RuntimeRazorPageLoader>();

            // services.AddTransient<IRazorPageFactoryProvider, MultiTenantRuntimePageFactoryProvider>();
            services.AddSingleton<IViewCompilerProvider, RuntimeMultiViewCompilerProvider>();
            services.AddSingleton<IRazorViewEngine, RazorMultiViewEngine>();
            services.AddSingleton<PublicRazorReferenceManager>();
            services.AddSingleton<PublicCSharpCompiler>();

            services.AddSingleton<IDictionary<string, RazorProjectEngine>>(s => {

                var csharpCompiler = s.GetRequiredService<PublicCSharpCompiler>();
                var referenceManager = s.GetRequiredService<PublicRazorReferenceManager>();
                var dictionary = new Dictionary<string, RazorProjectEngine>();

                var options = s.GetService<IOptions<RazorMultiViewEngineOptions>>()?.Value;
                var compileOptions = s.GetService<IOptions<MvcRazorRuntimeCompilationOptions>>();

                if (options is null) {
                    // Log a warning that there are no options and dont add any engines.
                    return dictionary;
                }

                if (!string.IsNullOrEmpty(options.DefaultViewLibrary.PathRelativeToContentRoot)) {
                    var path = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.DefaultViewLibrary.PathRelativeToContentRoot));
                    var engine = GetEngine(csharpCompiler, referenceManager, new PublicFileProviderRazorProjectFileSystem(
                        new PublicRuntimeCompilationFileProvider(compileOptions), environment), options.DefaultViewLibrary.AssemblyName);
                    dictionary.Add($"{options.DefaultViewLibrary.AssemblyName}.Views", engine);
                }

                // Assumes they are all Loaded at this point
                // and that the paths have been checked and are valid
                foreach (var viewLibrary in options.ViewLibraries) {
                    if (dictionary.ContainsKey(viewLibrary.AssemblyName)) {
                        // TODO: Invalid config should have been picked up here
                        continue;
                    }

                    if (!string.IsNullOrEmpty(viewLibrary.PathRelativeToContentRoot)) {
                        var path = Path.GetFullPath(Path.Combine(environment.ContentRootPath, viewLibrary.PathRelativeToContentRoot));
                        var engine = GetEngine(csharpCompiler, referenceManager, new PublicFileProviderRazorProjectFileSystem(
                        new PublicRuntimeCompilationFileProvider(compileOptions), environment), viewLibrary.AssemblyName);
                        dictionary.Add($"{viewLibrary.AssemblyName}.Views", engine);
                    }
                }
                return dictionary;
            });

            return services;
        }

        private static RazorProjectEngine GetEngine(PublicCSharpCompiler csharpCompiler,
                                                    PublicRazorReferenceManager referenceManager,
                                                    PublicFileProviderRazorProjectFileSystem projectFileSystem,
                                                    string assemblyName) {

            var engineConfig = RazorConfiguration.Create(RazorLanguageVersion.Latest, assemblyName, Array.Empty<RazorExtension>());

            return RazorProjectEngine.Create(engineConfig, projectFileSystem, builder => {
                RazorExtensions.Register(builder);

                // Roslyn + TagHelpers infrastructure                            
                builder.Features.Add(new PublicLazyMetadataReferenceFeature(referenceManager));
                builder.Features.Add(new CompilationTagHelperFeature());

                // TagHelperDescriptorProviders (actually do tag helper discovery)
                builder.Features.Add(new DefaultTagHelperDescriptorProvider());
                builder.Features.Add(new ViewComponentTagHelperDescriptorProvider());
                builder.SetCSharpLanguageVersion(csharpCompiler.ParseOptions.LanguageVersion);
            });

        }
    }
}