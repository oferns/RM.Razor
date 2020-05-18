namespace RM.Razor {
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.ApplicationParts;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    public class MultiTenantRazorViewEngineOptionsSetup : IConfigureOptions<MultiTenantRazorViewEngineOptions> {
        private readonly IHostEnvironment environment;

        public MultiTenantRazorViewEngineOptionsSetup(IHostEnvironment environment) {
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }


        public void Configure(MultiTenantRazorViewEngineOptions options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            // Make sure the referenced view libs are loaded.
            foreach (var viewLibrary in options.ViewLibraries) {
                // This will throw if the Assembly cannot be loaded
                var assembly = AppDomain.CurrentDomain.EnsureAssembly(viewLibrary.AssemblyName);
                var razorAssemblies = assembly.EnsureRazorAssemblies();

                if (!string.IsNullOrEmpty(viewLibrary.PathRelativeToContentRoot)) {
                    // Check the path exists 
                    var path = Path.GetFullPath(Path.Combine(this.environment.ContentRootPath, viewLibrary.PathRelativeToContentRoot));

                    if (!Directory.Exists(path)) {
                        throw new ApplicationException($"The path for view library {viewLibrary.AssemblyName} does not exist at {path}");
                    }
                }
            }

            // Set the default engine to the entry assembly if its not specified
            if (options.DefaultViewLibrary is null) {                
                options.DefaultViewLibrary = new ViewLibraryInfo {
                    AssemblyName = $"{Assembly.GetEntryAssembly().GetName().Name}",
                    PathRelativeToContentRoot = "./"
                };
            } else {
                // if it is specified make sure it has been loaded
                var assembly = AppDomain.CurrentDomain.EnsureAssembly(options.DefaultViewLibrary.AssemblyName);
                _ = assembly.EnsureRazorAssemblies();
            }

            options.ViewLocationFormats.Add("/Views/{1}/{0}" + MultiTenantRazorViewEngine.ViewExtension);
            options.ViewLocationFormats.Add("/Views/Shared/{0}" + MultiTenantRazorViewEngine.ViewExtension);

            options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}" + MultiTenantRazorViewEngine.ViewExtension);
            options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}" + MultiTenantRazorViewEngine.ViewExtension);
            options.AreaViewLocationFormats.Add("/Views/Shared/{0}" + MultiTenantRazorViewEngine.ViewExtension);
        }
    }
}
