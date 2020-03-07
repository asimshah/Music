using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using Microsoft.AspNetCore.Hosting;
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
        private readonly MusicOptions options;
        private readonly IServiceProvider serviceProvider;
        private readonly string connectionString;
        public TaskRunner(IServiceProvider sp, IOptions<MusicOptions> options,
            IConfiguration cfg, IWebHostEnvironment environment,
            ILogger<TaskRunner> logger) : base(logger)
        {
            this.serviceProvider = sp;
            this.options = options.Value;
            maxConsumerThreads = Math.Max(1, this.options.MaxTaskThreads);
            connectionString = environment.LocaliseConnectionString(cfg.GetConnectionString("MusicDb"));
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
            log.Information($"{item.ToDescription()} queued");
        }
        private void StartThreads()
        {
            for(int i = 0; i < maxConsumerThreads;++i)
            {
                var th = new TaskHost(this.serviceProvider, options, TaskQueue, connectionString, cancellationToken);
                var t = Task.Run(async () =>
                {
                    await th.Execute();
                    log.Information("host has died .........................");
                });
                consumerTasks.Add(th);
            }
        }
    }
}
