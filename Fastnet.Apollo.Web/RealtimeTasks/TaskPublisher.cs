using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public class TaskPublisher : HostedService // RealtimeTask
    {
        private CancellationToken cancellationToken;

        private readonly MusicOptions musicOptions;
        private readonly IServiceProvider serviceProvider;
        private readonly TaskRunner taskRunner;
        private readonly string connectionString;
        public TaskPublisher(TaskRunner runner, IServiceProvider sp,
            IConfiguration cfg, IWebHostEnvironment environment,
            IOptions<MusicOptions> mso, ILogger<TaskPublisher> logger) : base(logger)
        {
            this.taskRunner = runner;
            this.serviceProvider = sp;
            this.musicOptions = mso.Value;
            connectionString = environment.LocaliseConnectionString(cfg.GetConnectionString("MusicDb"));
            //this.BeforeTaskStartsAsync = OnStartup;
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            log.Information($"started");
            await LoadOutstandingTasks();
            this.cancellationToken = cancellationToken;
            return;
        }
        public async Task AddPortraitsTask(MusicStyles style, bool force = false)
        {
            await AddTask(style, TaskType.Portraits, style.ToString(),  force);
        }
        public async Task AddTask(MusicStyles style, bool force = false)
        {
            await AddTask(style, TaskType.MusicStyle, style.ToString(),  force);
        }
        //public async Task AddTask(MusicStyles style, string artistName, bool force = false)
        //{
        //    await AddTask(style, TaskType.ArtistName, artistName,  force);
        //}

        public async Task<TaskItem> AddTask(MusicStyles style, TaskType type, string taskString, /*bool queueTask = true,*/ bool force = false)
        {
            using (var db = new MusicDb(connectionString))
            {
                return await AddTask(db, style, type, taskString,  force);
                //var existing = db.TaskItems
                //    .Where(t => t.Type == type && t.MusicStyle == style
                //        && t.TaskString.ToLower() == taskString.ToLower()
                //        && (t.Status == Music.Core.TaskStatus.Pending || t.Status == Music.Core.TaskStatus.InProgress)
                //        && t.Force == force);
                //if (existing.Count() == 0)
                //{
                //    var now = DateTimeOffset.Now;
                //    var ti = new TaskItem
                //    {
                //        Type = type,
                //        CreatedAt = now,
                //        ScheduledAt = now,
                //        Status = Music.Core.TaskStatus.Pending,
                //        MusicStyle = style,
                //        TaskString = taskString,
                //        Force = force
                //    };
                //    await db.TaskItems.AddAsync(ti);
                //    await db.SaveChangesAsync();
                //    if (queueTask)
                //    {
                //        taskRunner.QueueTask(ti);
                //    }
                //    return ti;
                //}
                //else
                //{
                //    log.Debug($"Task type {type} for target {taskString} skipped as alrerady present");
                //}
                //return null;
            }
        }
        private async Task<TaskItem> AddTask(MusicDb db, MusicStyles style, TaskType type, string taskString,/* bool queueTask = true,*/ bool force = false)
        {
            var existing = db.TaskItems
                .Where(t => t.Type == type && t.MusicStyle == style
                    && t.TaskString.ToLower() == taskString.ToLower()
                    && (t.Status == Music.Core.TaskStatus.Pending || t.Status == Music.Core.TaskStatus.InProgress)
                    && t.Force == force);
            if (existing.Count() == 0)
            {
                var now = DateTimeOffset.Now;
                var ti = new TaskItem
                {
                    Type = type,
                    CreatedAt = now,
                    ScheduledAt = now,
                    Status = Music.Core.TaskStatus.Pending,
                    MusicStyle = style,
                    TaskString = taskString,
                    Force = force
                };
                await db.TaskItems.AddAsync(ti);
                await db.SaveChangesAsync();
                taskRunner.QueueTask(ti);
                return ti;
            }
            else
            {
                log.Debug($"Task type {type} for target {taskString} skipped as alrerady present");
            }
            return null;
        }
        private async Task LoadOutstandingTasks()
        {
            var now = DateTimeOffset.Now;
            var list = new List<TaskItem>();
            using (var db = new MusicDb(connectionString))
            {
                foreach(var ti in db.TaskItems.Where(x =>
                    x.Type != TaskType.ResampleWork && x.Status != Music.Core.TaskStatus.Finished && x.Status != Music.Core.TaskStatus.Failed))
                {
                    ti.Status = Music.Core.TaskStatus.Pending;
                    ti.ScheduledAt = now;
                    ti.FinishedAt = default;
                    list.Add(ti);
                }
                await db.SaveChangesAsync();
            }
            foreach(var item in list)
            {
                //taskRunner.TaskAdded(item.Id, item.Type);
                taskRunner.QueueTask(item);
            }
        }
    }
}
