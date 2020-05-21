namespace RM.Razor {
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.Collections.Generic;

    public class MultiTenantStaticFileProvider : IFileProvider {
        private readonly IHttpContextAccessor contextAccessor;
        private readonly IDictionary<string, ManifestEmbeddedFileProvider> embeddedProviders;
        private readonly IFileProvider originalProvider;
        private readonly MultiTenantRazorViewEngineOptions viewOptions;

        public MultiTenantStaticFileProvider(IHttpContextAccessor contextAccessor, 
                                            IOptions<MultiTenantRazorViewEngineOptions> viewOptions, 
                                            IFileProvider originalProvider) {
            this.contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));            
            this.originalProvider = originalProvider ?? throw new ArgumentNullException(nameof(originalProvider));
            this.viewOptions = viewOptions?.Value ?? throw new ArgumentNullException(nameof(viewOptions));

            this.embeddedProviders = new Dictionary<string, ManifestEmbeddedFileProvider>();
            
            foreach (var library in this.viewOptions.ViewLibraries) {
                if (!string.IsNullOrEmpty(library.EmbeddedStaticFilePath)) {
                    var assembly = AppDomain.CurrentDomain.EnsureAssembly(library.AssemblyName);
                    embeddedProviders.Add(library.AssemblyName, new ManifestEmbeddedFileProvider(assembly, library.EmbeddedStaticFilePath));
                }            
            }
        }

        public IDirectoryContents GetDirectoryContents(string subpath) {

            if (contextAccessor.HttpContext.Items.TryGetValue(this.viewOptions.HttpContextItemsKey, out var key)) {
                if (this.viewOptions.ViewLibraryConfig.TryGetValue(key.ToString(), out var libraries)) {
                    foreach (var lib in libraries) {
                        if (this.embeddedProviders.TryGetValue(lib, out var provider)) {
                            return provider.GetDirectoryContents(subpath);                        
                        }                    
                    }                    
                }
            }
            return this.originalProvider.GetDirectoryContents(subpath);
        }

        public IFileInfo GetFileInfo(string subpath) {
            if (contextAccessor.HttpContext.Items.TryGetValue(this.viewOptions.HttpContextItemsKey, out var key)) {
                if (this.viewOptions.ViewLibraryConfig.TryGetValue(key.ToString(), out var libraries)) {
                    foreach (var lib in libraries) {
                        if (this.embeddedProviders.TryGetValue(lib, out var provider)) {
                            var info = provider.GetFileInfo(subpath);
                            if (info.Exists) {
                                return info;
                            }
                        }
                    }
                }
            }
            return this.originalProvider.GetFileInfo(subpath);
        }

        public IChangeToken Watch(string filter) {
            return this.originalProvider.Watch(filter);
        }
    }
}
