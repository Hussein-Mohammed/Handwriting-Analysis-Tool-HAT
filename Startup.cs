using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Coravel;
using LongRunningSignalr;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using HAT3p5.MyLibrary;

namespace HAT3p5
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddRazorPages();
            services.AddRazorPages().AddNewtonsoftJson();
            services.AddRazorPages().AddRazorRuntimeCompilation();

            services.AddSingleton(new TempVariables_Unknown_Singleton { });
            services.AddSingleton(new TempVariables_Known_Singleton { });
            services.AddSingleton(new GlobalVariables { });

            services.AddQueue();
            services.AddSignalR();

            services.Configure<FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
                // Set the limit to 256 MB
                //options.MultipartBodyLengthLimit = 2199023985135;
            });

            services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = int.MaxValue;
            });

            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = int.MaxValue; // if don't set default value is: 30 MB
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseSignalR(routes => { routes.MapHub<JobProgressHub>("/jobprogress"); });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });

            appLifetime.ApplicationStarted.Register(() => OpenBrowser(app.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First()));
        }

        private static void OpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}")
                {
                    CreateNoWindow = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                Console.WriteLine("No pre-defined OS has been detected!");
            }
        }
    }
}
