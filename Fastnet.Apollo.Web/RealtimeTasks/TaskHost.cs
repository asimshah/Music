using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;

namespace Fastnet.Apollo.Web
{
    public class TaskHost
    {
        private static int hostIdentifier = 0;
        private readonly int hostIdentity;
        private readonly CancellationToken cancellationToken;
        private readonly ILogger log;
        private readonly string connectionString;
        private readonly BlockingCollection<TaskQueueItem> taskQueue;
        private readonly IOptionsMonitor<MusicOptions> options;
        private readonly IServiceProvider serviceProvider;
        private readonly IOptionsMonitor<IndianClassicalInformation> monitoredIndianClassicalInformation;
        public TaskHost(IServiceProvider sp, IOptionsMonitor<MusicOptions> options, BlockingCollection<TaskQueueItem> taskQueue,
            IOptionsMonitor<IndianClassicalInformation> monitoredIndianClassicalInformation,
            string connectionString, CancellationToken cancellationToken)
        {
            this.serviceProvider = sp;
            this.options = options;
            this.cancellationToken = cancellationToken;
            this.hostIdentity = ++hostIdentifier;
            this.log = ApplicationLoggerFactory.CreateLogger($"Fastnet.Apollo.Web.TaskHost{hostIdentity}");
            this.connectionString = connectionString;
            this.taskQueue = taskQueue;
            this.monitoredIndianClassicalInformation = monitoredIndianClassicalInformation;
        }
        public async Task Execute()
        {
            int count = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                count++;
                try
                {
                    log.Debug($"started count = {count}");
                    await ProcessTaskQueue();
                    log.Information($"finished");
                }
                catch (CatalogueFailed cfe)
                {
                    log.Information($"[TI-{cfe.TaskId}] CatalogueFailed, requeue suspended");
                    //log.Information($"[TI-{cfe.TaskId}] CatalogueFailed, attempting requeue");
                    //TryAddTaskAgain(cfe.TaskId);
                }
                catch (Exception xe)
                {
                    if (xe.InnerException != null)
                    {
                        log.Error($"recycling due to {xe.GetType().Name}, inner exception {xe.InnerException.GetType().Name} {xe.InnerException.Message}");
                    }
                    else
                    {
                        log.Error($"recycling due to {xe.GetType().Name}, {xe.Message}");
                    }
                }
            }
            log.Information($"cancellationToken.IsCancellationRequested = {cancellationToken.IsCancellationRequested}");
        }
        private async Task ProcessTaskQueue()
        {
            foreach (var item in taskQueue.GetConsumingEnumerable(this.cancellationToken))
            {
                if (SetTaskItemStatus(item.TaskItemId, Music.Core.TaskStatus.InProgress, out TaskType _))
                {
                    TaskBaseOld tbo = null;
                    TaskBase tb = null;
                    switch (item.Type)
                    {
                        case TaskType.DiskPath:
                            //tbo = new CataloguePathOld(options.CurrentValue, item.TaskItemId, connectionString, monitoredIndianClassicalInformation.CurrentValue, taskQueue,
                            //    this.serviceProvider.GetService<IOptions<MusicServerOptions>>(), this.serviceProvider.GetService<IHubContext<MessageHub, IHubMessage>>(),
                            //    this.serviceProvider.GetService<ILoggerFactory>());
                            tb = this.serviceProvider.GetRequiredService<CataloguePath>();
                            break;
                        case TaskType.Portraits:
                            tb = this.serviceProvider.GetRequiredService<UpdatePortraits>();
                            //tbo = new UpdatePortraits(options.CurrentValue, item.TaskItemId, connectionString);
                            break;
                        case TaskType.ArtistFolder:
                        //case TaskType.ArtistName:
                        case TaskType.MusicStyle:
                            tb = this.serviceProvider.GetRequiredService<ExpandTask>();
                            //tbo = new ExpandTask(options.CurrentValue, item.TaskItemId, connectionString, taskQueue);
                            break;
                        case TaskType.DeletedPath:
                            tb = this.serviceProvider.GetRequiredService<DeletePath>();

                            //tb = new DeletePathOld(options.CurrentValue, item.TaskItemId, connectionString,
                            //    this.serviceProvider.GetService<IOptions<MusicServerOptions>>(), this.serviceProvider.GetService<IHubContext<MessageHub, IHubMessage>>(),
                            //    this.serviceProvider.GetService<ILoggerFactory>());
                            break;
                    }
                    if(tb != null)
                    {
                        await tb.RunAsync(item.TaskItemId);
                    }
                    else if (tbo != null)
                    {
                        log.Debug($"host is executing an instance of {tbo?.GetType().Name}");
                        await tbo.RunAsync();
                    }
                }
                else
                {
                    log.Warning($"[TI-{item.TaskItemId}] unable to set as InProgress");
                }
            }
        }
        private void TryAddTaskAgain(long itemId)
        {
            using (var db = new MusicDb(connectionString))
            {
                var taskItem = db.TaskItems.Find(itemId);
                var maxRetries = options.CurrentValue.MaxTaskRetries;
                bool requeue = false;
                if (taskItem.RetryCount < maxRetries)
                {
                    taskItem.Status = Music.Core.TaskStatus.Pending;
                    taskItem.ScheduledAt = DateTimeOffset.Now;
                    taskItem.RetryCount++;
                    requeue = true;

                    log.Information($"{taskItem} requeued, retry {taskItem.RetryCount} of {maxRetries}");
                }
                else
                {
                    taskItem.Status = Music.Core.TaskStatus.Failed;
                    taskItem.FinishedAt = DateTimeOffset.Now;
                    log.Information($"{taskItem} failed - retries exhausted");
                }
                db.SaveChanges();
                if(requeue)
                {
                    QueueTask(taskItem);
                }
            }
        }
        private bool SetTaskItemStatus(long itemId, Music.Core.TaskStatus status, out TaskType type)
        {
            bool result = false;
            type = TaskType.DiskPath;
            using (var db = new MusicDb(connectionString))
            {
                var taskItem = db.TaskItems.Find(itemId);
                try
                {
                    var oldStatus = taskItem.Status;
                    taskItem.Status = status;// Music.Core.TaskStatus.InProgress;
                    type = taskItem.Type;
                    db.SaveChanges();
                    result = true;
                    log.Debug($"{taskItem} status changed from {oldStatus} to {taskItem.Status}");
                }
                catch (Exception xe)
                {
                    if (xe.InnerException != null)
                    {
                        log.Information($"{taskItem} error: {xe.GetType().Name}, inner exception {xe.InnerException.GetType().Name} {xe.InnerException.Message}");
                    }
                    else
                    {
                        log.Information($"{taskItem} error: {xe.GetType().Name}, {xe.Message}");
                    }
                }
            }
            return result;
        }
        private void QueueTask(TaskItem item)
        {
            var tqi = new TaskQueueItem { TaskItemId = item.Id, Type = item.Type/*, ProcessingId = Guid.NewGuid()*/};
            taskQueue.Add(tqi);
            log.Information($"{item.ToDescription()} queued");
        }
    }

}
