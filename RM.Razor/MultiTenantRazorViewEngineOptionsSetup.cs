namespace RM.Razor {

    using Microsoft.Extensions.Options;
    using System;

    public class MultiTenantRazorViewEngineOptionsSetup : IConfigureOptions<MultiTenantRazorViewEngineOptions> {
        public void Configure(MultiTenantRazorViewEngineOptions options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            options.ViewLocationFormats.Add("/Views/{1}/{0}" + MultiTenantRazorViewEngine.ViewExtension);
            options.ViewLocationFormats.Add("/Views/Shared/{0}" + MultiTenantRazorViewEngine.ViewExtension);

            options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}" + MultiTenantRazorViewEngine.ViewExtension);
            options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}" + MultiTenantRazorViewEngine.ViewExtension);
            options.AreaViewLocationFormats.Add("/Views/Shared/{0}" + MultiTenantRazorViewEngine.ViewExtension);
        }
    }
}
