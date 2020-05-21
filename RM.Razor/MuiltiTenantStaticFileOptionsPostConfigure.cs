
namespace RM.Razor {

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using System;

    public class MuiltiTenantStaticFileOptionsPostConfigure : IPostConfigureOptions<StaticFileOptions> {
        private readonly IOptions<MultiTenantRazorViewEngineOptions> optionsAccessor;

        public MuiltiTenantStaticFileOptionsPostConfigure(IOptions<MultiTenantRazorViewEngineOptions> optionsAccessor, IHttpContextAccessor contextAccessor) {
            this.optionsAccessor = optionsAccessor ?? throw new ArgumentNullException(nameof(optionsAccessor));
        }



        public void PostConfigure(string name, StaticFileOptions options) {

            var originalFileProvider = options.FileProvider;

            



        }
    }
}
