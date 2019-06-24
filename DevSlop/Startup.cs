using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights;
using DevSlop.Slop.Repositories;
using DevSlop.Slop.Data;

namespace DevSlop
{
    public class Startup
    {
        public IConfiguration Configuration { get; }


        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            var config = builder.Build();

            builder.AddAzureKeyVault(
                $"https://{config["azureKeyVault:vault"]}.vault.azure.net/",
                config["azureKeyVault:clientId"],
                config["azureKeyVault:clientSecret"]
            );

            this.Configuration = builder.Build();

            //example of how to read value
            var connectionStringKey = "appSettings:connectionStrings:DefaultConnection";
            var connectionString = Configuration[connectionStringKey];
            var aiClient = new TelemetryClient();
            aiClient.TrackTrace("connectionStringKey: " + connectionStringKey);
            aiClient.TrackTrace("connection string from Startup(): " + connectionString, Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information);
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // setup Dependency Injection for Schedule REpository
            services.AddTransient<IScheduleRepository, ScheduleRepository>();

            // set up DB context and default identity
            var connectionString = this.Configuration["appSettings:connectionStrings:DefaultConnection"];
            services.AddDbContext<DevSlopContext>(options => options.UseSqlServer(connectionString));
            services.AddDefaultIdentity<IdentityUser>()
                .AddEntityFrameworkStores<DevSlopContext>();

            // add application insight
            var aiClient = new TelemetryClient();
            aiClient.TrackTrace("connection string from ConfigureServices(): " + connectionString, Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information);


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, DevSlopContext devSlopContext)
        {
            // generate ui for identity
            app.UseStaticFiles();
            app.UseAuthentication();


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts(hsts => hsts.MaxAge(365).IncludeSubdomains());
            }

            //Security headers make me happy
            app.UseXContentTypeOptions();
            app.UseReferrerPolicy(opts => opts.NoReferrer());
            app.UseXXssProtection(options => options.EnabledWithBlockMode());
            app.UseXfo(options => options.Deny());
            /*app.UseCsp(opts => opts
             .BlockAllMixedContent()
             .StyleSources(s => s.Self())
             .StyleSources(s => s.UnsafeInline())
             .FontSources(s => s.Self())
             .FormActions(s => s.Self())
             .FrameAncestors(s => s.Self())
             .ImageSources(s => s.Self())
             .ScriptSources(s => s.Self())
             );*/
            //End Security Headers



            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            //devSlopContext.Database.Migrate();
        }
    }
}