namespace RM.Razor.Mvc {

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using RM.Razor.Mvc.Middleware;
    using RM.Razor.RuntimeCompilation;

    public class Startup {
        private readonly IWebHostEnvironment environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment) {
            Configuration = configuration;
            this.environment = environment;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {

            services.AddHttpContextAccessor();

            services.Configure<RazorMultiViewEngineOptions>(Configuration.GetSection("MultiTenantRazorViewEngineOptions"));

            services.AddMultiTenantViewEgine();
#if DEBUG
            services.AddMultiTenantRuntimeCompilation(environment);
#endif
            services.AddControllersWithViews();
            
            services.AddRazorPages();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {

            app.UseMiddleware<CookieBasedViewLibrarySelectorMiddleware>();
            app.UseMiddleware<HostBasedViewLibrarySelectorMiddleware>();

            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();  
                          
            app.UseRouting();

            app.UseAuthorization();                                                   


            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapRazorPages();
            });
        }
    }
}
