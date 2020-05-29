namespace RM.Razor {
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class KeyedStaticFileProvider : IFileProvider {
        private readonly IHttpContextAccessor contextAccessor;
        private readonly IDictionary<string, IFileProvider> embeddedProviders;
        private readonly IFileProvider originalProvider;
        private readonly RazorMultiViewEngineOptions viewOptions;

        public KeyedStaticFileProvider(IHttpContextAccessor contextAccessor, 
                                            IOptions<RazorMultiViewEngineOptions> viewOptions,
                                            IWebHostEnvironment environment) {
            
            this.contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));            
            this.originalProvider = environment?.WebRootFileProvider ?? throw new ArgumentNullException(nameof(originalProvider));
            this.viewOptions = viewOptions?.Value ?? throw new ArgumentNullException(nameof(viewOptions));

            this.embeddedProviders = new Dictionary<string, IFileProvider>();
            
            foreach (var library in this.viewOptions.ViewLibraries) {
                if (!string.IsNullOrEmpty(library.StaticFilePath)) {
                    var assembly = AppDomain.CurrentDomain.EnsureAssembly(library.AssemblyName);
                    if (string.IsNullOrEmpty(library.PathRelativeToContentRoot)) {
                        embeddedProviders.Add(library.AssemblyName, new ManifestEmbeddedFileProvider(assembly, library.StaticFilePath));
                    } else {
                        var path = Path.GetFullPath(Path.Combine(environment.ContentRootPath, library.PathRelativeToContentRoot, library.StaticFilePath));
                        embeddedProviders.Add(library.AssemblyName, new PhysicalFileProvider(path));
                    }
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
