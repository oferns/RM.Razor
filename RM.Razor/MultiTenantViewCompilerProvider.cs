namespace RM.Razor {

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.ApplicationParts;
    using Microsoft.AspNetCore.Mvc.Razor.Compilation;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class MultiTenantViewCompilerProvider : IViewCompilerProvider {
        
        private readonly IHttpContextAccessor contextAccessor;
        private readonly MultiTenantRazorViewEngineOptions options;
        private readonly IDictionary<string, IViewCompiler> compilers = new Dictionary<string, IViewCompiler>();
        private readonly string defaultViewLibrary;

        public MultiTenantViewCompilerProvider(ApplicationPartManager applicationPartManager, 
                                                IHttpContextAccessor contextAccessor,
                                                IOptions<MultiTenantRazorViewEngineOptions> optionsAccessor) {

            if (applicationPartManager is null) {
                throw new ArgumentNullException(nameof(applicationPartManager));
            }


            this.contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            this.options = optionsAccessor?.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));

            // TODO: Check the options.DefaultViewLibrary exists and is a Razor Class Library
            this.defaultViewLibrary = string.IsNullOrEmpty(this.options.DefaultViewLibrary) ? $"{Assembly.GetEntryAssembly().GetName().Name}.Views" : this.options.DefaultViewLibrary;

            var feature = new ViewsFeature();
            applicationPartManager.PopulateFeature(feature);
                       
            var defaultViews = new List<CompiledViewDescriptor>();

            foreach (var descriptor in feature.ViewDescriptors.Where(d => d.Item.Type.Assembly.GetName().Name.Equals(this.defaultViewLibrary))) {
                if (!defaultViews.Any(v => v.RelativePath.Equals(descriptor.RelativePath))) {
                    defaultViews.Add(descriptor);
                }
            }

            compilers.Add(this.defaultViewLibrary, new MultiTenantViewCompiler(defaultViews));

            foreach (var host in options.ViewLibraries) {

                var viewDescriptors = new List<CompiledViewDescriptor>();

                var hostSpecificViews = feature.ViewDescriptors.Where(d => d.Item.Type.Assembly.GetName().Name.Equals($"{host.Value}.Views"));

                foreach (var view in hostSpecificViews) {
                    if (!viewDescriptors.Any(v => v.RelativePath.Equals(view.RelativePath))) {
                        viewDescriptors.Add(view);                                                                                                  
                    }

                }

                foreach (var view in defaultViews) {
                    if (!viewDescriptors.Any(v => v.RelativePath.Equals(view.RelativePath))) {
                        viewDescriptors.Add(view);
                    }
                }

                compilers.Add(host.Key, new MultiTenantViewCompiler(viewDescriptors));
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