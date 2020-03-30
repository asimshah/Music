using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Fastnet.Apollo.Web
{
    // test chnage
    public class Startup
    {
        private IWebHostEnvironment environment;
        private ILogger log;
        public Startup(IConfiguration configuration, ILogger<Startup> logger, IWebHostEnvironment env)
        {
            Configuration = configuration;
            this.log = logger;
            this.environment = env;
            var packageVersion = GetPackageVersion();
            var assemblyVersion = GetAssemblyVersion();
            var version = typeof(Startup).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var name = Process.GetCurrentProcess().ProcessName;
            log.Information($"Music {packageVersion} [{assemblyVersion}] site started ({name})");
            var versions = GetVersions();
            log.Information($"using versions:");
            foreach (var item in versions.OrderBy(x => x))
            {
                log.Information($"   {item}");
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR((x) => x.EnableDetailedErrors = true);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddOptions();
            services.Configure<MessengerOptions>(Configuration.GetSection("MessengerOptions"));
            services.Configure<MusicServerOptions>(Configuration.GetSection("MusicServerOptions"));
            services.Configure<SchedulerOptions>(Configuration.GetSection("SchedulerOptions"));
            services.Configure<MusicOptions>(Configuration.GetSection("MusicOptions"));
            services.Configure<FileSystemMonitorOptions>(Configuration.GetSection("FileSystemMonitorOptions"));
            services.AddSingleton<BrowserMonitor>();
            var cs = environment.LocaliseConnectionString(Configuration.GetConnectionString("MusicDb"));
            services.AddDbContext<MusicDb>(options =>
            {
                try
                {
                    options.UseSqlServer(cs, sqlServerOptions =>
                    {
                        //sqlServerOptions.EnableRetryOnFailure(8, TimeSpan.FromSeconds(15), null);
                        sqlServerOptions.EnableRetryOnFailure();
                        if (environment.IsDevelopment())
                        {
                            sqlServerOptions.CommandTimeout(60 * 3);
                        }
                    })

                    .EnableDetailedErrors()
                    .EnableSensitiveDataLogging()
                    .UseLazyLoadingProxies();
                }
                catch (Exception xe)
                {
                    log.Error(xe);
                    throw;
                }

            });
            services.AddMusicLibraryTasks(Configuration);


            // .net core 3.0's built-in json (System.Text.Json) is missing features as of Nov 2019:
            // 1. reference loop handling
            // 2. deserializing anonymous types
            // so I revert back to NewtonsoftJson here ....
            services.AddControllersWithViews()
                .AddNewtonsoftJson();
            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            CheckAddBinPath();// for lame mp3 stuff
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();
            if (!env.IsDevelopment())
            {
                app.UseSpaStaticFiles();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
                endpoints.MapHub<PlayHub>("/playhub");
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.Options.StartupTimeout = TimeSpan.FromSeconds(120);
                    spa.UseAngularCliServer(npmScript: "start");
                }
            });
            using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var options = scope.ServiceProvider.GetService<IOptions<MusicOptions>>().Value;
                try
                {
                    //var t = scope.ServiceProvider.GetService<IOptions<test>>();
                    var musicDb = scope.ServiceProvider.GetService<MusicDb>();
                    MusicDbInitialiser.Initialise(musicDb, options);
                }
                catch (System.Exception xe)
                {
                    log.Error(xe, $"Error initialising MusicDb");
                }
                
                foreach (var source in new MusicSources(options, true))
                {
                    if (!Directory.Exists(source.DiskRoot))
                    {
                        Directory.CreateDirectory(source.DiskRoot);
                        log.Information($"{source.DiskRoot} created");
                    }
                    foreach (var style in options.Styles.Where(x => x.Enabled))
                    {
                        foreach (var setting in style.Settings)
                        {
                            var path = Path.Combine(source.DiskRoot, setting.Path);
                            if (!Directory.Exists(path))
                            {
                                Directory.CreateDirectory(path);
                                log.Information($"{path} created");
                            }
                        }
                    }
                }
            }
            lifetime.ApplicationStarted.Register(OnStarted);
            lifetime.ApplicationStopping.Register(OnStopping);
            lifetime.ApplicationStopped.Register(OnStopped);
        }
        public void CheckAddBinPath()
        {
            // find path to 'bin' folder
            //var binPath = Path.Combine(new string[] { AppDomain.CurrentDomain.BaseDirectory, "bin" });
            var binPath = AppDomain.CurrentDomain.BaseDirectory;// Path.Combine(new string[] { AppDomain.CurrentDomain.BaseDirectory, "bin" });
            // get current search path from environment
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";

            // add 'bin' folder to search path if not already present
            if (!path.Split(Path.PathSeparator).Contains(binPath, StringComparer.CurrentCultureIgnoreCase))
            {
                path = string.Join(Path.PathSeparator.ToString(), new string[] { path, binPath });
                Environment.SetEnvironmentVariable("PATH", path);
            }
        }
        private void OnStarted()
        {
            log.Information("OnStarted()");
        }
        private void OnStopping()
        {
            log.Information("OnStopping()");
        }
        private void OnStopped()
        {
            log.Information("OnStopped()");
        }
        // move this to Fastnet.Core
        private string GetPackageVersion()
        {
            //var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            //return System.Diagnostics.FileVersionInfo.GetVersionInfo(assemblyLocation).ProductVersion;
            return System.Reflection.Assembly.GetExecutingAssembly().GetPackageVersion();
        }
        // move this to Fastnet.Core
        private string GetAssemblyVersion()
        {
            //return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            return System.Reflection.Assembly.GetExecutingAssembly().GetAssemblyVersion();
        }
        private IEnumerable<string> GetVersions()
        {
            var list = new List<string>();
            foreach(var assemblyName in System.Reflection.Assembly.GetExecutingAssembly().GetReferencedAssemblies())
            {
                if (assemblyName.Name.StartsWith("fastnet", System.Globalization.CompareOptions.IgnoreCase))
                {
                    list.Add($"{assemblyName.Name}, {assemblyName.Version}");
                }
            }
            return list;
        }

    }
}
