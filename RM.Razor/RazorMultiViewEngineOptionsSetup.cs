namespace RM.Razor {
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.ApplicationParts;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    public class RazorMultiViewEngineOptionsSetup : IConfigureOptions<RazorMultiViewEngineOptions> {
        private readonly IHostEnvironment environment;

        public RazorMultiViewEngineOptionsSetup(IHostEnvironment environment) {
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }


        public void Configure(RazorMultiViewEngineOptions options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            // Make sure the referenced view libs are loaded.
            if (options.ViewLibraries is object) {
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

            options.ViewLocationFormats.Add("/Views/{1}/{0}" + RazorMultiViewEngine.ViewExtension);
            options.ViewLocationFormats.Add("/Views/Shared/{0}" + RazorMultiViewEngine.ViewExtension);

            options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}" + RazorMultiViewEngine.ViewExtension);
            options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}" + RazorMultiViewEngine.ViewExtension);
            options.AreaViewLocationFormats.Add("/Views/Shared/{0}" + RazorMultiViewEngine.ViewExtension);


            //var rootDirectory = environment.ContentRootPath;

            //var defaultPageSearchPath = CombinePath(rootDirectory, "{1}/{0}" + RazorMultiViewEngine.ViewExtension);
            //options.PageViewLocationFormats.Add(defaultPageSearchPath);

            //// /Pages/Shared/{0}.cshtml
            //var pagesSharedDirectory = CombinePath(rootDirectory, "Shared/{0}" + RazorMultiViewEngine.ViewExtension);
            //options.PageViewLocationFormats.Add(pagesSharedDirectory);

            //options.PageViewLocationFormats.Add("/Views/Shared/{0}" + RazorMultiViewEngine.ViewExtension);

            //var areaDirectory = CombinePath("/Areas/", "{2}");
            //// Areas/{2}/Pages/
            //var areaPagesDirectory = CombinePath(areaDirectory, "/Pages/");

            //// Areas/{2}/Pages/{1}/{0}.cshtml
            //// Areas/{2}/Pages/Shared/{0}.cshtml
            //// Areas/{2}/Views/Shared/{0}.cshtml
            //// Pages/Shared/{0}.cshtml
            //// Views/Shared/{0}.cshtml
            //var areaSearchPath = CombinePath(areaPagesDirectory, "{1}/{0}" + RazorMultiViewEngine.ViewExtension);
            //options.AreaPageViewLocationFormats.Add(areaSearchPath);

            //var areaPagesSharedSearchPath = CombinePath(areaPagesDirectory, "Shared/{0}" + RazorMultiViewEngine.ViewExtension);
            //options.AreaPageViewLocationFormats.Add(areaPagesSharedSearchPath);

            //var areaViewsSharedSearchPath = CombinePath(areaDirectory, "Views/Shared/{0}" + RazorMultiViewEngine.ViewExtension);
            //options.AreaPageViewLocationFormats.Add(areaViewsSharedSearchPath);

            //options.AreaPageViewLocationFormats.Add(pagesSharedDirectory);
            //options.AreaPageViewLocationFormats.Add("/Views/Shared/{0}" + RazorMultiViewEngine.ViewExtension);

            //options.ViewLocationFormats.Add(pagesSharedDirectory);
            //options.AreaViewLocationFormats.Add(pagesSharedDirectory);

            //options.ViewLocationExpanders.Add(new PageViewLocationExpander());
        }

        private static string CombinePath(string path1, string path2) {
            if (path1.EndsWith("/", StringComparison.Ordinal) || path2.StartsWith("/", StringComparison.Ordinal)) {
                return path1 + path2;
            } else if (path1.EndsWith("/", StringComparison.Ordinal) && path2.StartsWith("/", StringComparison.Ordinal)) {
                return path1 + path2.Substring(1);
            }

            return path1 + "/" + path2;
        }
    }
}
