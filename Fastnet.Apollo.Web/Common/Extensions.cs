using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
// temp:: using Fastnet.Music.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public static partial class Extensions
    {
        public static void Touch(this string fileName)
        {
            var fi = new System.IO.FileInfo(fileName);
            fi.Touch();
        }
        public static void Touch(this System.IO.FileInfo fi)
        {
            fi.LastWriteTimeUtc = DateTime.UtcNow;
        }
        public static IServiceCollection AddMusicLibraryTasks(this IServiceCollection services, IConfiguration configuration)
        {
            var so = new SchedulerOptions();
            configuration.GetSection("SchedulerOptions").Bind(so);
            services.AddScheduler(configuration);

            services.AddSingleton<FileSystemMonitorFactory>();
            services.AddSingleton<Messenger>();
            services.AddService<MusicFolderChangeMonitor>();
            services.AddService<TaskRunner>();
            services.AddService<TaskPublisher>();
            services.AddService<PlayManager>();
            services.AddService<Resampler>();
            if (!so.SuspendScheduling)
            {
                foreach (var s in so.Schedules)
                {
                    if (s.Enabled)
                    {
                        switch (s.Name)
                        {
                            case "MusicScanner":
                                // temp:: services.AddSingleton<ScheduledTask, MusicScanner>();
                                break;
                        }
                    }
                }
            }
            return services;
        }
    }
}
