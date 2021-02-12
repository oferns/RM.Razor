namespace RM.Razor.RuntimeCompilation {

    using System;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
    using Microsoft.Extensions.Options;

    public class PublicMvcRazorRuntimeCompilationOptionsSetup : IConfigureOptions<MvcRazorRuntimeCompilationOptions> {
        private readonly IWebHostEnvironment _hostingEnvironment;

        public PublicMvcRazorRuntimeCompilationOptionsSetup(IWebHostEnvironment hostingEnvironment) {
            _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
        }

        public void Configure(MvcRazorRuntimeCompilationOptions options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            options.FileProviders.Add(_hostingEnvironment.ContentRootFileProvider);
        }
    }
}
