using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public class TaskRunner : HostedService
    {
        private int maxConsumerThreads;// = 1;//3;// 5;
        private List<TaskHost> consumerTasks = new List<TaskHost>();
        private BlockingCollection<TaskQueueItem> TaskQueue;
        private List<long> itemList = new List<long>();
        private CancellationToken cancellationToken;
        private readonly IOptionsMonitor<MusicOptions> options;
        private readonly IServiceProvider serviceProvider;
        private readonly string connectionString;
        private readonly IOptionsMonitor<IndianClassicalInformation> monitoredIndianClassicalInformation;
        private readonly EntityObserver entityObserver;
        //private readonly IHubContext<MessageHub, IHubMessage> messageHub;
        public TaskRunner(IServiceProvider sp, IOptionsMonitor<MusicOptions> options, /*IOptions<IndianClassicalInformation> iciOptions,*/
            IOptionsMonitor<IndianClassicalInformation> monitoredIci,
            IConfiguration cfg, IWebHostEnvironment environment,
            EntityObserver entityObserver,
            IHubContext<MessageHub, IHubMessage> messageHub,
            ILogger<TaskRunner> logger) : base(logger)
        {
            this.serviceProvider = sp;
            this.options = options;//.Value;
            monitoredIndianClassicalInformation = monitoredIci;
            maxConsumerThreads = Math.Max(1, this.options.CurrentValue.MaxTaskThreads);
            connectionString = environment.LocaliseConnectionString(cfg.GetConnectionString("MusicDb"));
            this.entityObserver = entityObserver;
            //this.messageHub = messageHub;
            this.entityObserver.EntityChanged += EntityChanged;
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            log.Information($"started");
            TaskQueue = new BlockingCollection<TaskQueueItem>();
            this.cancellationToken = cancellationToken;
            StartThreads();
            return Task.CompletedTask;
        }
        public void QueueTask(TaskItem item)
        {
            var tqi = new TaskQueueItem { TaskItemId = item.Id, Type = item.Type};
            TaskQueue.Add(tqi);
            log.Debug($"{item.ToDescription()} queued");
        }

        private void StartThreads()
        {
            for(int i = 0; i < maxConsumerThreads;++i)
            {
                var th = new TaskHost(this.serviceProvider, options, TaskQueue, monitoredIndianClassicalInformation, connectionString, cancellationToken);
                var t = Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await th.Execute();
                        log.Information("task host finished - restarting");
                    }
                });
                consumerTasks.Add(th);
            }
        }
        private void EntityChanged(object sender, EntityChangedEventArgs e)
        {
            switch(e.Type)
            {
                case EntityChangeType.Delete:
                    break;
            }
            log.Information(e.LogMessage);
        }
        #region hub-messages
        //public async Task SendArtistDeleted(long id)
        //{
        //    try
        //    {
        //        await this.messageHub.Clients.All.SendArtistDeleted(id);
        //    }
        //    catch (Exception xe)
        //    {
        //        log.Error(xe);
        //    }

        //}
        //public async Task SendArtistNewOrModified(long id)
        //{
        //    try
        //    {
        //        await this.messageHub.Clients.All.SendArtistNewOrModified(id);
        //    }
        //    catch (Exception xe)
        //    {
        //        log.Error(xe);
        //    }

        //}
        #endregion hub-messages
    }
}
