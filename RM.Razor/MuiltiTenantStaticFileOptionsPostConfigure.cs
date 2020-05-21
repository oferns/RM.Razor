﻿
namespace RM.Razor {

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using System;

    public class MuiltiTenantStaticFileOptionsPostConfigure : IPostConfigureOptions<StaticFileOptions> {
        private readonly IOptions<MultiTenantRazorViewEngineOptions> optionsAccessor;
        private readonly IHttpContextAccessor contextAccessor;
        private readonly IWebHostEnvironment environment;

        public MuiltiTenantStaticFileOptionsPostConfigure(IOptions<MultiTenantRazorViewEngineOptions> optionsAccessor,
                                                            IHttpContextAccessor contextAccessor,
                                                            IWebHostEnvironment environment) {
            this.optionsAccessor = optionsAccessor ?? throw new ArgumentNullException(nameof(optionsAccessor));
            this.contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }



        public void PostConfigure(string name, StaticFileOptions options) {
            options.FileProvider = new MultiTenantStaticFileProvider(contextAccessor, this.optionsAccessor, environment.WebRootFileProvider);
        }
    }
}