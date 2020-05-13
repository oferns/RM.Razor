namespace RM.Razor.StaticFiles {

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class MultiTenantFileProvider : IFileProvider {
        
        private readonly IHttpContextAccessor contextAccessor;
        private readonly IDictionary<string, IFileProvider> providers;
        private readonly MultiTenantRazorViewEngineOptions options;

        public MultiTenantFileProvider(IHttpContextAccessor contextAccessor, IDictionary<string, IFileProvider> providers, IOptions<MultiTenantRazorViewEngineOptions> optionsAccessor) {
            this.contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            this.providers = providers ?? throw new ArgumentNullException(nameof(providers));
            this.options = optionsAccessor?.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));
        }


        public IDirectoryContents GetDirectoryContents(string subpath) {
            var libraryKey = this.contextAccessor.HttpContext.Items[options.HttpContextItemsKey];
            if (libraryKey is object && providers.TryGetValue(libraryKey.ToString(), out var provider)) {
                return provider.GetDirectoryContents(subpath);                                                                                                                
            }
            return providers.First().Value.GetDirectoryContents(subpath);
        }

        public IFileInfo GetFileInfo(string subpath) {
            var fileInfo = default(IFileInfo);
            var libraryKey = this.contextAccessor.HttpContext.Items[options.HttpContextItemsKey];
            if (libraryKey is object && providers.TryGetValue(libraryKey.ToString(), out var provider)) {
                fileInfo  = provider.GetFileInfo(subpath);
            }

            if (fileInfo is null || !fileInfo.Exists) {
                fileInfo = providers.First().Value.GetFileInfo(subpath);
            }
            return fileInfo;
        }

        public IChangeToken Watch(string filter) {
            var libraryKey = this.contextAccessor.HttpContext.Items[options.HttpContextItemsKey];
            if (libraryKey is object && providers.TryGetValue(libraryKey.ToString(), out var provider)) {
                return provider.Watch(filter);
            }
            return providers.First().Value.Watch(filter);

        }
    }
}
