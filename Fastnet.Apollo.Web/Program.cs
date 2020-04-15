using Fastnet.Core.Logging;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Fastnet.Apollo.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var wh = CreateWebHostBuilder(args)
                .Build();
            ApplicationLoggerFactory.Init(wh.Services);
            wh.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("raganames.json", optional: false);
            })
            .ConfigureLogging(lb => lb.AddRollingFile())
            .UseStartup<Startup>();
    }
}
