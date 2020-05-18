namespace RM.Razor.Runtime {
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.ApplicationParts;
    using Microsoft.AspNetCore.Mvc.Razor.Compilation;
    using Microsoft.AspNetCore.Razor.Language;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class MultiTenantRuntimeViewCompilerProvider : IViewCompilerProvider {

        private readonly IHttpContextAccessor contextAccessor;
        private readonly ILogger<MultiTenantRuntimeViewCompiler> logger;
        private readonly MultiTenantRazorViewEngineOptions options;

        private readonly IDictionary<string, IViewCompiler> compilers = new Dictionary<string, IViewCompiler>();

        public MultiTenantRuntimeViewCompilerProvider(IHttpContextAccessor contextAccessor,
                                                    ApplicationPartManager applicationPartManager,
                                                    IOptions<MultiTenantRazorViewEngineOptions> optionsAccessor,
                                                    IDictionary<string, RazorProjectEngine> razorProjectEngines,
                                                    CSharpCompiler csharpCompiler,
                                                    ILoggerFactory loggerFactory) {

            this.contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            this.options = optionsAccessor?.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));
            this.logger = loggerFactory.CreateLogger<MultiTenantRuntimeViewCompiler>();

            var feature = new ViewsFeature();
            applicationPartManager.PopulateFeature(feature);

            var defaultViews = new List<CompiledViewDescriptor>();

            foreach (var descriptor in feature.ViewDescriptors.Where(f => f.Item.Type.Assembly.GetName().Name.Equals($"{options.DefaultViewLibrary.AssemblyName}.Views", StringComparison.Ordinal))) {
                if (!defaultViews.Exists(v => v.RelativePath.Equals(descriptor.RelativePath, StringComparison.OrdinalIgnoreCase))) {
                    defaultViews.Add(descriptor);
                }
            }

            compilers.Add("default", new MultiTenantViewCompiler(defaultViews));

            // A cache list of libraries and their compiled views 
            var libraryViewList = new Dictionary<string, List<CompiledViewDescriptor>>();

            foreach (var option in options.ViewLibraryConfig) {

                if (compilers.ContainsKey(option.Key)) {
                    continue;
                }

                // A list of descriptors for this option                
                var viewDescriptors = new List<CompiledViewDescriptor>();

                // Loop the requested libraries
                foreach (var library in option.Value) {
                    if (!libraryViewList.TryGetValue(library, out var liblist)) {
                        liblist = feature.ViewDescriptors.Where(d => d.Item.Type.Assembly.GetName().Name.Equals($"{library}.Views")).ToList();
                    }

                    foreach (var descriptor in liblist) {
                        if (viewDescriptors.Exists(v => v.RelativePath.Equals(descriptor.RelativePath, StringComparison.OrdinalIgnoreCase))) {
                            continue;
                        }
                        viewDescriptors.Add(descriptor);
                    }
                }

                // Add any missing views from the default library
                foreach (var descriptor in defaultViews) {
                    if (viewDescriptors.Exists(v => v.RelativePath.Equals(descriptor.RelativePath, StringComparison.OrdinalIgnoreCase))) {
                        continue;
                    }
                    viewDescriptors.Add(descriptor);
                }
                compilers.Add(option.Key, new MultiTenantRuntimeViewCompiler(razorProjectEngines, csharpCompiler, viewDescriptors, logger));
            }

        }

        public IViewCompiler GetCompiler() {
            if (contextAccessor.HttpContext.Items.TryGetValue(this.options.HttpContextItemsKey, out var key) && !string.IsNullOrEmpty(key?.ToString())) {
                var hostname = key.ToString();
                if (compilers.ContainsKey(hostname)) {
                    return compilers[hostname];
                }
            }
            return compilers["default"];
        }
    }
}