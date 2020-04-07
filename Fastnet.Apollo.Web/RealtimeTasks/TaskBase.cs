using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
#nullable enable

    public abstract class TaskBase
    {
        protected MusicStyles musicStyle;
        protected string taskData = string.Empty;
        protected bool forceChanges;
        protected bool forSingles;
        protected readonly MusicOptions musicOptions;
        protected readonly long taskId;
        protected readonly string connectionString;
        protected readonly ILogger log;
        private readonly BlockingCollection<TaskQueueItem>? taskQueue;
        public TaskBase(MusicOptions options, long taskId, string connectionString, BlockingCollection<TaskQueueItem> taskQueue)
        {
            musicOptions = options;
            this.taskId = taskId;
            this.connectionString = connectionString;
            this.taskQueue = taskQueue;
            log = ApplicationLoggerFactory.CreateLogger(GetType());

        }
        public async Task RunAsync()
        {
            (musicStyle, taskData, forSingles, forceChanges) = await GetTaskAsync();
            await RunTask();
        }
        protected abstract Task RunTask();

        private async Task<(MusicStyles ms, string dataString, bool forSingles, bool force)> GetTaskAsync()
        {
            using (var db = new MusicDb(connectionString))
            {
                var taskItem = await db.TaskItems.FindAsync(taskId);
                return (taskItem.MusicStyle, taskItem.TaskString, taskItem.ForSingles, taskItem.Force);
            }
        }
        protected async Task ExecuteTaskItemAsync(Func<MusicDb, Task> methodAsync)
        {
            using (var db = new MusicDb(connectionString))
            {
                try
                {
                    log.Debug($"[TI-{taskId}] execution started");
                    await methodAsync(db);
                }
                catch (Exception xe)
                {
                    log.Error($"[TI-{taskId}] Error {xe.GetType().Name} thrown within execution");
                    throw;
                }
                finally
                {
                    log.Debug($"[TI-{taskId}] execution finished");
                }
            }

        }
        protected async Task<RT> ExecuteTaskItemAsync<RT>(Func<MusicDb, Task<RT>> methodAsync)
        {
            using (var db = new MusicDb(connectionString))
            {
                try
                {
                    log.Debug($"[TI-{taskId}] execution started");
                    return await methodAsync(db);
                }
                catch (Exception xe)
                {
                    log.Error($"[TI-{taskId}] Error {xe.GetType().Name} thrown within execution");
                    throw;
                }
                finally
                {
                    log.Debug($"[TI-{taskId}] execution finished");
                }
            }

        }
        protected async Task<RT?> ExecuteTaskItemWithRetryAsync<RT>(Func<MusicDb, Task<RT>> methodAsync) where RT : class
        {
            using (var db = new MusicDb(connectionString))
            {
                RT? r = default;

                //SqlServerRetryingExecutionStrategy? strategy = db.Database.CreateExecutionStrategy() as SqlServerRetryingExecutionStrategy;
                var strategy = db.Database.CreateExecutionStrategy() as RetryStrategy;
                if (strategy != null)
                {
                    strategy.SetIdentifier($"[TI-{taskId}]");
                    await strategy.ExecuteAsync(async () =>
                    {
                        
                        if(strategy.RetryNumber > 0)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(10));
                        }
                        log.Information($"[TI-{taskId}] execution started, retry {strategy.RetryNumber + 1}");
                        try
                        {
                            //using (var tran = db.Database.BeginTransaction(System.Data.IsolationLevel.Snapshot)) // default is read committed
                            using (var db2 = new MusicDb(connectionString))
                            {
                                using (var tran = db2.Database.BeginTransaction())
                                {
                                    r = await methodAsync(db2);
                                    tran.Commit();
                                    log.Debug($"[TI-{taskId}] execution strategy: transaction committed");
                                } 
                            }
                        }
                        catch (Exception xe)
                        {
                            log.Error($"[TI-{taskId}] Error {xe.GetType().Name} thrown within execution strategy");
                            throw;
                        }
                        finally
                        {
                            log.Debug($"[TI-{taskId}] execution strategy finished");
                        }
                    });
                }
                return r;
            }
        }
        protected void QueueTask(TaskItem item)
        {
            var tqi = new TaskQueueItem { TaskItemId = item.Id, Type = item.Type/*, ProcessingId = Guid.NewGuid()*/};
            if (taskQueue != null)
            {
                taskQueue.Add(tqi);
                log.Information($"{item.ToDescription()} queued");
            }
            else
            {
                log.Information($"{item.ToDescription()} - not task queue available");
            }
        }
    }
#nullable restore
}

