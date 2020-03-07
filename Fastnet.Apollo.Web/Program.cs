using Fastnet.Core.Logging;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Fastnet.Apollo.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //CreateWebHostBuilder(args).Build().Run();
            var wh = CreateWebHostBuilder(args).Build();
            ApplicationLoggerFactory.Init(wh.Services);
            wh.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureLogging(lb => lb.AddRollingFile())
            .UseStartup<Startup>();
    }
}
