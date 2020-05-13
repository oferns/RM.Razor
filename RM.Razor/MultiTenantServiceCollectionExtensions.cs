namespace RM.Razor {
   
    using Microsoft.AspNetCore.Mvc.ApplicationParts;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.Razor.Compilation;
    using Microsoft.AspNetCore.Mvc.Razor.Extensions;
    using Microsoft.AspNetCore.Razor.Hosting;
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
    using System.Reflection;

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

                var options = s.GetService<IOptions<MultiTenantRazorViewEngineOptions>>().Value;


                var entryAssembly = Assembly.GetEntryAssembly();
                var setBase = false;

                // Add Razor projects
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.CustomAttributes.Any(c => c.AttributeType.Equals(typeof(RelatedAssemblyAttribute))))) {

                    if (assembly.Equals(entryAssembly)) {
                        setBase = true;
                        continue; // Add this as the base library
                    }

                    var viewLibraryInfos = options.ViewLibraries.Where(h => h.Key.Equals(assembly.GetName().Name, StringComparison.OrdinalIgnoreCase)).Select(h => h.Value);
                    
                    if (viewLibraryInfos.Any()) { 
                                            
                    
                    
                    
                    }

                    
                    var relativePath = viewLibraryInfo?.Value.  ?? $"../{assembly.GetName().Name}";
                    var path = Path.GetFullPath(Path.Combine(environment.ContentRootPath, relativePath));
                    var projectFileSystem = new FileProviderRazorProjectFileSystem(path);

                    var engines = GetEnginesForAssembly(assembly, csharpCompiler, projectFileSystem, referenceManager);

                    foreach (var engine in engines) {
                        if (!dictionary.ContainsKey(engine.Key)) {
                            dictionary.Add(engine.Key, engine.Value);
                        }
                    }
                }

                if (setBase) {
                    var engines = GetEnginesForAssembly(entryAssembly, csharpCompiler, new FileProviderRazorProjectFileSystem(environment.ContentRootPath), referenceManager);
                    foreach (var engine in engines) {
                        if (!dictionary.ContainsKey(engine.Key)) {
                            dictionary.Add(engine.Key, engine.Value);
                        }
                    }
                }

                return dictionary;
            });

            return services;        
        }

        private static IDictionary<string, RazorProjectEngine> GetEnginesForAssembly(Assembly rootAssembly, CSharpCompiler csharpCompiler, RazorProjectFileSystem projectFileSystem, RazorReferenceManager referenceManager) {

            var dictionary = new Dictionary<string, RazorProjectEngine>();
            var relatedAttribute = rootAssembly.GetCustomAttributes<RelatedAssemblyAttribute>();

            var relatedViewAssemblies = new List<Assembly>();
            foreach (var att in relatedAttribute) {
                var relatedAssembly = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name.Equals(att.AssemblyFileName, StringComparison.OrdinalIgnoreCase)).SingleOrDefault();
                if (relatedAssembly is object) {
                    if (relatedAssembly.CustomAttributes.Any(c => c.AttributeType.Equals(typeof(RazorCompiledItemAttribute)))) {

                        var engineConfig = RazorConfiguration.Create(RazorLanguageVersion.Latest, att.AssemblyFileName, Array.Empty<RazorExtension>());
                        var engine = RazorProjectEngine.Create(engineConfig, projectFileSystem, builder => {
                            RazorExtensions.Register(builder);

                            // Roslyn + TagHelpers infrastructure                            
                            builder.Features.Add(new LazyMetadataReferenceFeature(referenceManager));
                            builder.Features.Add(new CompilationTagHelperFeature());

                            // TagHelperDescriptorProviders (actually do tag helper discovery)
                            builder.Features.Add(new DefaultTagHelperDescriptorProvider());
                            builder.Features.Add(new ViewComponentTagHelperDescriptorProvider());
                            builder.SetCSharpLanguageVersion(csharpCompiler.ParseOptions.LanguageVersion);
                        });

                        dictionary.Add(att.AssemblyFileName, engine);
                    }
                }
            }

            return dictionary;

        }
    }
}