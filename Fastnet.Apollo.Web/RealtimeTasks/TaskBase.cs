using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
            try
            {
                await RunTask();
            }
            catch (Exception xe)
            {
                log.Error(xe, $"[TI-{ taskId}]");
                SetTaskFailed();
                //throw;
            }
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
        protected async Task ExecuteTaskItemWithRetryAsync(Func<MusicDb, Task> methodAsync)
        {
            using (var db = new MusicDb(connectionString))
            {
                //RT? r = default;
                var strategy = db.Database.CreateExecutionStrategy() as RetryStrategy;
                if (strategy != null)
                {
                    await strategy.ExecuteAsync(async () =>
                    {
                        try
                        {
                            if (strategy.RetryNumber > 0)
                            {
                                await Task.Delay(TimeSpan.FromSeconds(10));
                                log.Information($"[TI-{taskId}] execution restarted, retry {strategy.RetryNumber}");
                            }

                            using (var db2 = new MusicDb(connectionString))
                            {
                                using (var tran = db2.Database.BeginTransaction())
                                {
                                    await methodAsync(db2);
                                    tran.Commit();
                                    log.Debug($"[TI-{taskId}] execution strategy: transaction committed");
                                }
                            }
                        }
                        catch (DbUpdateException de)
                        {
                            if (de.InnerException != null)
                            {
                                log.Error(de.InnerException, $"[TI-{taskId}]");
                            }
                            else
                            {
                                log.Error(de, $"[TI-{taskId}]");
                            }
                            throw;
                        }
                        //catch (Exception xe)
                        //{
                        //    log.Error(xe);
                        //    throw;
                        //}
                    });
                }
                return;
            }
        }
        protected async Task<RT?> ExecuteTaskItemWithRetryAsync<RT>(Func<MusicDb, Task<RT>> methodAsync) where RT : class
        {
            using (var db = new MusicDb(connectionString))
            {
                RT? r = default;
                var strategy = db.Database.CreateExecutionStrategy() as RetryStrategy;
                if (strategy != null)
                {
                    try
                    {
                        await strategy.ExecuteAsync(async () =>
                            {
                                //try
                                //{
                                if (strategy.RetryNumber > 0)
                                {
                                    await Task.Delay(TimeSpan.FromSeconds(10));
                                    log.Information($"[TI-{taskId}] execution restarted, retry {strategy.RetryNumber}");
                                }

                                using (var db2 = new MusicDb(connectionString))
                                {
                                    using (var tran = db2.Database.BeginTransaction())
                                    {
                                        r = await methodAsync(db2);
                                        tran.Commit();
                                        log.Debug($"[TI-{taskId}] execution strategy: transaction committed");
                                    }
                                }
                                //}
                                //catch (Exception xe)
                                //{
                                //    log.Error(xe, $"[TI-{taskId}]");
                                //    throw;
                                //}
                            });
                    }
                    catch (Exception xe)
                    {
                        log.Error(xe, $"[TI-{taskId}]");
                        throw;
                    }
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
        private void SetTaskFailed()
        {
            using (var db = new MusicDb(connectionString))
            {
                var t = db.TaskItems.Find(taskId);
                t.Status = Music.Core.TaskStatus.Failed;
                t.FinishedAt = DateTimeOffset.Now;
                db.SaveChanges();
                log.Warning($"{t} {t.TaskString} abandoned");
            }
        }
    }
#nullable restore
}

