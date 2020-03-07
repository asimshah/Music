using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Fastnet.Apollo.Web
{

    // possibly rename this as it now handles player and library messages
    public partial class PlayManager : HostedService // RealtimeTask
    {
        private readonly IDictionary<string, DeviceRuntime> devices;
        private ServerInformationMulticast sim;
        private KeepAgentsAlive keepAlive;
        private readonly IHubContext<PlayHub, IHubMessage> playHub;
        private readonly Messenger messenger;
        private readonly ILoggerFactory lf;
        private readonly List<Task> taskList;
        private readonly IServiceProvider serviceProvider;
        private readonly MessengerOptions messengerOptions;
        private readonly MusicServerOptions musicServerOptions;
        private readonly IWebHostEnvironment environment;
        public PlayManager(IHubContext<PlayHub, IHubMessage> playHub, IServiceProvider serviceProvider,
            IWebHostEnvironment env,
            IOptions<MusicServerOptions> serverOptions, Messenger messenger, IOptions<MessengerOptions> messengerOptions,
            ILogger<PlayManager> log, ILoggerFactory loggerFactory) : base(log)
        {
            this.messengerOptions = messengerOptions.Value;
            this.playHub = playHub;
            this.serviceProvider = serviceProvider;
            this.musicServerOptions = serverOptions.Value;
            this.environment = env;

            this.messenger = messenger;
            this.messenger.EnableMulticastSend();
            this.lf = loggerFactory;
            this.taskList = new List<Task>();
            this.devices = new Dictionary<string, DeviceRuntime>();
        }
        protected /*public*/ override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            messenger.AddMulticastSubscription<DeviceStatus>(async (m) => { await OnDeviceStatus(m); });
            this.taskList.Add(Task.Run(() =>
            {
                this.sim = new ServerInformationMulticast(environment, messengerOptions, musicServerOptions, messenger, cancellationToken, lf);
                sim.Start().ContinueWith(sim.OnException, TaskContinuationOptions.OnlyOnFaulted);
                this.keepAlive = new KeepAgentsAlive(musicServerOptions, cancellationToken, lf);
                this.keepAlive.Start(this.GetPlayerUrls(), async (url) =>
                {
                    await PlayerDead(url);
                }).ContinueWith(keepAlive.OnException, TaskContinuationOptions.OnlyOnFaulted);
            }, cancellationToken));
            await Task.WhenAll(taskList);
        }
    }

}
