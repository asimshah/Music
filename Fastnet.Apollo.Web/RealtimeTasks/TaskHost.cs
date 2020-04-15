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
        private readonly MusicOptions options;
        private readonly IServiceProvider serviceProvider;
        private readonly IndianClassicalInformation indianClassicalInformation;
        public TaskHost(IServiceProvider sp, MusicOptions options, BlockingCollection<TaskQueueItem> taskQueue,
            IndianClassicalInformation ici,
            string connectionString, CancellationToken cancellationToken)
        {
            this.serviceProvider = sp;
            this.options = options;
            this.cancellationToken = cancellationToken;
            this.hostIdentity = ++hostIdentifier;
            this.log = ApplicationLoggerFactory.CreateLogger($"Fastnet.Apollo.Web.TaskHost{hostIdentity}");
            this.connectionString = connectionString;
            this.taskQueue = taskQueue;
            indianClassicalInformation = ici;
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
                    TaskBase tb = null;
                    switch (item.Type)
                    {
                        case TaskType.DiskPath:
                            tb = new CataloguePath(options, item.TaskItemId, connectionString, indianClassicalInformation, taskQueue, this.serviceProvider.GetService<PlayManager>());
                            break;
                        case TaskType.Portraits:
                            tb = new UpdatePortraits(options, item.TaskItemId, connectionString);
                            break;
                        case TaskType.ArtistFolder:
                        case TaskType.ArtistName:
                        case TaskType.MusicStyle:
                            tb = new ExpandTask(options, item.TaskItemId, connectionString, taskQueue);
                            break;
                        case TaskType.DeletedPath:
                            tb = new DeletePath(options, item.TaskItemId, connectionString, this.serviceProvider.GetService<PlayManager>());
                            break;
                        // resampling moved to a single polling background service
                        //case TaskType.ResampleWork:
                        //    tb = new ResampleTask(options, item.TaskItemId, connectionString);
                        //    break;
                    }
                    log.Debug($"host is executing an instance of {tb.GetType().Name}");
                    await tb?.RunAsync();
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
                var maxRetries = options.MaxTaskRetries;
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
                    taskItem.Status = status;// Music.Core.TaskStatus.InProgress;
                    type = taskItem.Type;
                    db.SaveChanges();
                    result = true;
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
