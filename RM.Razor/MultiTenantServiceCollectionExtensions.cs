namespace RM.Razor {
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.Razor.Compilation;
    using Microsoft.AspNetCore.Mvc.Razor.Extensions;
    using Microsoft.AspNetCore.Razor.Language;
    using Microsoft.CodeAnalysis.Razor;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using RM.Razor.Runtime;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class MultiTenantServiceCollectionExtensions {

        // Swaps out the Razor View engine and compiler for ours
        public static IServiceCollection AddMultiTenantViewEgine(this IServiceCollection services) {

            services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<MultiTenantRazorViewEngineOptions>, MultiTenantRazorViewEngineOptionsSetup>());
            return services
                .AddSingleton<IRazorViewEngine, MultiTenantRazorViewEngine>()
                .AddSingleton<IViewCompilerProvider, MultiTenantViewCompilerProvider>();
        }

        // Adds Runtime Compilation support
        public static IServiceCollection AddMultiTenantRuntimeCompilation(this IServiceCollection services, IHostEnvironment environment) {

            services.AddTransient<IRazorPageFactoryProvider, MultiTenantRuntimePageFactoryProvider>();
            services.AddSingleton<IViewCompilerProvider, MultiTenantRuntimeViewCompilerProvider>();
            services.AddSingleton<IRazorViewEngine, MultiTenantRazorViewEngine>();
            services.AddSingleton<RazorReferenceManager>();
            services.AddSingleton<CSharpCompiler>();

            services.AddSingleton<IDictionary<string, RazorProjectEngine>>(s => {

                var csharpCompiler = s.GetRequiredService<CSharpCompiler>();
                var referenceManager = s.GetRequiredService<RazorReferenceManager>();
                var dictionary = new Dictionary<string, RazorProjectEngine>();

                var options = s.GetService<IOptions<MultiTenantRazorViewEngineOptions>>()?.Value;

                if (options is null) {
                    // Log a warning that there are no options and dont add any engines.
                    return dictionary;
                }

                if (!string.IsNullOrEmpty(options.DefaultViewLibrary.PathRelativeToContentRoot)) {
                    var path = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.DefaultViewLibrary.PathRelativeToContentRoot));
                    var engine = GetEngine(csharpCompiler, referenceManager, new FileProviderRazorProjectFileSystem(path), options.DefaultViewLibrary.AssemblyName);
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
                        var engine = GetEngine(csharpCompiler, referenceManager, new FileProviderRazorProjectFileSystem(path), viewLibrary.AssemblyName);
                        dictionary.Add($"{viewLibrary.AssemblyName}.Views", engine);
                    }
                }                                        
                return dictionary;
            });

            return services;
        }



        private static RazorProjectEngine GetEngine(CSharpCompiler csharpCompiler,
                                                    RazorReferenceManager referenceManager,
                                                    FileProviderRazorProjectFileSystem projectFileSystem,
                                                    string assemblyName) {
                        
            var engineConfig = RazorConfiguration.Create(RazorLanguageVersion.Latest, assemblyName, Array.Empty<RazorExtension>());

            return RazorProjectEngine.Create(engineConfig, projectFileSystem, builder => {
                RazorExtensions.Register(builder);

                // Roslyn + TagHelpers infrastructure                            
                builder.Features.Add(new LazyMetadataReferenceFeature(referenceManager));
                builder.Features.Add(new CompilationTagHelperFeature());

                // TagHelperDescriptorProviders (actually do tag helper discovery)
                builder.Features.Add(new DefaultTagHelperDescriptorProvider());
                builder.Features.Add(new ViewComponentTagHelperDescriptorProvider());
                builder.SetCSharpLanguageVersion(csharpCompiler.ParseOptions.LanguageVersion);
            });

        }
    }
}