using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Messages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public class ServerInformationMulticast
    {
        private long counter = 0L;
        private MusicServerInformation si;
        //private readonly MusicConfiguration musicConfiguration;
        private readonly Messenger messenger;
        private MessengerOptions messengerOptions;
        private readonly ILogger log;
        private readonly ILoggerFactory lf;
        private readonly CancellationToken cancellationToken;
        private readonly MusicServerOptions musicServerOptions;
        private readonly IWebHostEnvironment environment;
        public ServerInformationMulticast(IWebHostEnvironment env, MessengerOptions messengerOptions, MusicServerOptions serverOptions, Messenger messenger, CancellationToken cancellationToken, ILoggerFactory loggerFactory)
        {
            Debug.Assert(messengerOptions != null);
            Debug.Assert(serverOptions != null);
            Debug.Assert(messenger != null);
            this.environment = env;
            this.messengerOptions = messengerOptions;
            this.musicServerOptions = serverOptions;
            this.lf = loggerFactory;
            this.messenger = messenger;
            this.log = lf.CreateLogger<ServerInformationMulticast>();// log;
            this.cancellationToken = cancellationToken;
            InitialiseServerInformation();
        }

        public async Task Start()
        {
            log.Debug($"started");
            while (true)
            {
                await SendServerInformation();
                await Task.Delay(GetCurrentInterval());
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        internal void OnException(Task task, object arg2)
        {
            if (task.Exception != null)
            {
                log.Error(task.Exception);
            }
            else
            {
                log.Warning($"did not expect to be here!!!!!");
            }
        }
        private async Task SendServerInformation()
        {
            await this.messenger.SendMulticastAsync(si);
            if((counter % musicServerOptions.MulticastReportInterval) == 0)
            {
                log.Debug($"sent MusicServerInformation [{counter.ToString()}]");
            }
            ++counter;
        }
        private int GetCurrentInterval()
        {
            Debug.Assert(musicServerOptions.Intervals.ServerInformationBroadcastInterval > 0);
            return musicServerOptions.Intervals.ServerInformationBroadcastInterval;
        }
        private void InitialiseServerInformation()
        {
            Debug.Assert(messengerOptions.LocalCIDR != null);
            try
            {
                var list = NetInfo.GetMatchingIPV4Addresses(messengerOptions.LocalCIDR);
                if (list.Count() > 1)
                {
                    log.Warning($"Multiple local ipaddresses: {(string.Join(", ", list.Select(l => l.ToString()).ToArray()))}, cidr is {messengerOptions.LocalCIDR}, config error?");
                }
                var ipAddress = list.First();
                si = new MusicServerInformation
                {
                    MachineName = Environment.MachineName.ToLower(),
                    ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id,
                    //Url = $"http://{ipAddress.ToString()}:{musicServerOptions.Port}"
                };
                if(environment.IsDevelopment())
                {
                    // in development, I use IISExpress which by default only listens to "localhost"
                    si.Url = $"http://localhost:{musicServerOptions.Port}";
                }
                else
                {
                    si.Url = $"http://{ipAddress.ToString()}:{musicServerOptions.Port}";
                }
                log.Information($"music server url is {si.Url}");
            }
            catch (Exception xe)
            {
                //Debugger.Break();
                log.Error(xe);
            }
        }
    }
}
