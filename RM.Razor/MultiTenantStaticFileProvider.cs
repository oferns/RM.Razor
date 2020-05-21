namespace RM.Razor {

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class MultiTenantStaticFileProvider : IFileProvider {
        private readonly IHttpContextAccessor contextAccessor;
        private readonly IDictionary<string, EmbeddedFileProvider> embeddedProviders;
        private readonly StaticFileOptions originalOptions;
        private readonly MultiTenantRazorViewEngineOptions viewOptions;

        public MultiTenantStaticFileProvider(IHttpContextAccessor contextAccessor, 
                                            IDictionary<string, EmbeddedFileProvider> embeddedProviders,
                                            IOptions<MultiTenantRazorViewEngineOptions> viewOptions, 
                                            IOptions<StaticFileOptions> originalOptions) {
            this.contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            this.embeddedProviders = embeddedProviders ?? throw new ArgumentNullException(nameof(embeddedProviders));
            this.originalOptions = originalOptions?.Value ?? throw new ArgumentNullException(nameof(originalOptions));
            this.viewOptions = viewOptions?.Value ?? throw new ArgumentNullException(nameof(viewOptions));
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
            return this.originalOptions.FileProvider.GetDirectoryContents(subpath);
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
            return this.originalOptions.FileProvider.GetFileInfo(subpath);
        }

        public IChangeToken Watch(string filter) {
            return this.originalOptions.FileProvider.Watch(filter);
        }
    }
}
