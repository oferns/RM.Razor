using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RM.Mvc.Middleware;
using RM.Razor;

namespace RM.Mvc {
    public class Startup {
        private readonly IHostEnvironment environment;

        public Startup(IConfiguration configuration, IHostEnvironment environment) {
            Configuration = configuration;
            this.environment = environment;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {

            services.AddHttpContextAccessor();

            services.Configure<MultiTenantRazorViewEngineOptions>(Configuration.GetSection("MultiTenantRazorViewEngineOptions"));

            services.AddMultiTenantViewEgine();
#if DEBUG
            services.AddMultiTenantRuntimeCompilation(environment);
#endif
            services.AddControllersWithViews();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/Home/Error");
            }

            // app.UseStaticFiles();  < instead of this
            app.UseMultiTenantStaticFiles(); // <- use this

            app.UseRouting();

            app.UseAuthorization();                                                   

            app.UseMiddleware<HostBasedViewLibrarySelectorMiddleware>();

#if DEBUG
            app.UseMiddleware<CookieBasedViewLibrarySelectorMiddleware>();
#endif

            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
