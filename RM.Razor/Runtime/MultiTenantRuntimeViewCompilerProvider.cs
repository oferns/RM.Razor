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
        private readonly string defaultViewLibrary;
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

            foreach (var descriptor in feature.ViewDescriptors) {
                if (!defaultViews.Exists(v => v.RelativePath.Equals(descriptor.RelativePath, StringComparison.OrdinalIgnoreCase))) {
                    defaultViews.Add(descriptor);
                }
            }

            // TODO: Check the options.DefaultViewLibrary exists and is a Razor Class Library
            this.defaultViewLibrary = string.IsNullOrEmpty(this.options.DefaultViewLibrary) ? $"{Assembly.GetEntryAssembly().GetName().Name}.Views" : this.options.DefaultViewLibrary;

            compilers.Add(this.defaultViewLibrary, new MultiTenantRuntimeViewCompiler(
                razorProjectEngines,
                csharpCompiler,
                defaultViews,
                logger));

            foreach (var host in razorProjectEngines) {
                if (!compilers.ContainsKey(host.Key)) {
                    logger.LogWarning($"{host.Key} has already been added to the View Library collection and will not be readded");
                    continue;
                }

                var viewDescriptors = new List<CompiledViewDescriptor>();
                var hostSpecificViews = feature.ViewDescriptors.Where(d => d.Item.Type.Assembly.GetName().Name.Equals(host.Key));

                foreach (var view in hostSpecificViews) {
                    if (!viewDescriptors.Any(v => v.RelativePath.Equals(view.RelativePath, StringComparison.OrdinalIgnoreCase))) {
                        viewDescriptors.Add(view);
                    }
                }

                foreach (var view in defaultViews) {
                    if (!viewDescriptors.Any(v => v.RelativePath.Equals(view.RelativePath))) {
                        viewDescriptors.Add(view);
                    }
                }

                compilers.Add(host.Key, new MultiTenantRuntimeViewCompiler(
                          razorProjectEngines,
                          csharpCompiler,
                          viewDescriptors.ToList(),
                          logger));
            }


        }

        public IViewCompiler GetCompiler() {
            if (contextAccessor.HttpContext.Items.TryGetValue(this.options.HttpContextItemsKey, out var host) && !string.IsNullOrEmpty(host?.ToString())) {
                var hostname = host.ToString();
                if (compilers.ContainsKey(hostname)) {
                    return compilers[hostname];
                }
            }
            return compilers[this.defaultViewLibrary];
        }
    }
}