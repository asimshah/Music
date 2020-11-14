using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public class DeletePath : TaskBase
    {
        private readonly EntityHelper entityHelper;
        private readonly IOptionsMonitor<MusicOptions> optionsMonitor;
        public DeletePath(
            IOptionsMonitor<MusicOptions> optionsMonitor,
            EntityHelper entityHelper,
            ILogger<DeletePath> logger,
            IConfiguration cfg,
            IWebHostEnvironment environment) : base(logger, cfg, environment)
        {
            this.optionsMonitor = optionsMonitor;
            this.entityHelper = entityHelper;
        }
        protected override async Task RunTask()
        {
            await this.ExecuteTaskItemWithRetryAsync(async (db) =>
            {
                await DeleteAsync(db);
            });
        }
        private async Task DeleteAsync(MusicDb db)
        {
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            var taskItem = await db.TaskItems.FindAsync(taskId);
            var mpa = MusicRoot.AnalysePath(optionsMonitor.CurrentValue, taskItem.TaskString);
            var folder = mpa.GetFolder();
            this.entityHelper.Enable(db, taskItem);
            log.Debug($"{taskItem} starting {folder.ToString()}");
            var musicFileList = folder.GetFilesInDb(this.entityHelper, mpa.GetPath());
            if (musicFileList.Count() == 0)
            {
                log.Warning($"{taskItem} {folder.ToString()} no music files found");
            }
            else
            {
                log.Information($"{taskItem}  {folder.ToString()} deleting {musicFileList.Count()} music files");
                foreach (var mf in musicFileList)
                {
                    this.entityHelper.Delete(mf);
                }
            }
            taskItem.Status = Music.Core.TaskStatus.Finished;
            taskItem.FinishedAt = DateTimeOffset.Now;
            await db.SaveChangesAsync();
        }
    }
}

