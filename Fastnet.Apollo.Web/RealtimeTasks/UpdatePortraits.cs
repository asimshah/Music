using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fastnet.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Fastnet.Apollo.Web
{
    public class UpdatePortraits : TaskBase
    {
        private readonly IOptionsMonitor<MusicOptions> optionsMonitor;
        public UpdatePortraits(IOptionsMonitor<MusicOptions> optionsMonitor,
            ILogger<UpdatePortraits> log, IConfiguration cfg, IWebHostEnvironment environment) : base(log, cfg, environment)
        {
            this.optionsMonitor = optionsMonitor;
        }
        protected async override Task RunTask()
        {
            await this.ExecuteTaskItemAsync(async (db) =>
            {
                await UpdateAsync(db);
            });
        }
        private async Task UpdateAsync(MusicDb db/*, IEnumerable<string> paths*/)
        {
            var ti = await db.TaskItems.FindAsync(taskId);
            var artists = await db.ArtistStyles.Where(x => x.StyleId == ti.MusicStyle && x.Artist.Type != ArtistType.Various)
                .Select(x => x.Artist)
                .ToArrayAsync();
            foreach (var artist in artists)
            {
                var portraitFile = artist.GetPortraitFile(optionsMonitor.CurrentValue);
                if (portraitFile == null)
                {
                    if (artist.Portrait != null)
                    {
                        artist.Portrait = null;
                        log.Information($"{ti} artist {artist.Name} portrait removed");
                    }
                }
                else if (artist.Portrait.HasChanged(portraitFile))
                {
                    artist.Portrait = await portraitFile.GetImage();
                    log.Information($"{ti} artist {artist.Name} portrait updated using {portraitFile}");
                }
            }
            ti.Status = Music.Core.TaskStatus.Finished;
            ti.FinishedAt = DateTimeOffset.Now;
            await db.SaveChangesAsync();
        }
    }

    public class UpdatePortraitsOld : TaskBaseOld
    {
        public UpdatePortraitsOld(MusicOptions options, long taskId, string connectionString) : base(options, taskId, connectionString, null)
        {
        }
        protected async override Task RunTask()
        {
            await this.ExecuteTaskItemAsync(async (db) =>
            {
                await UpdateAsync(db);
            });
        }
        private async Task UpdateAsync(MusicDb db/*, IEnumerable<string> paths*/)
        {
            var ti = await db.TaskItems.FindAsync(taskId);
            var artists = await db.ArtistStyles.Where(x => x.StyleId == musicStyle && x.Artist.Type != ArtistType.Various)
                .Select(x => x.Artist)
                .ToArrayAsync();
            foreach (var artist in artists)
            {
                var portraitFile = artist.GetPortraitFile(musicOptions);
                if(portraitFile == null)
                {
                    if (artist.Portrait != null)
                    {
                        artist.Portrait = null;
                        log.Information($"{ti} artist {artist.Name} portrait removed"); 
                    }
                }
                else if(artist.Portrait.HasChanged(portraitFile))
                {
                    artist.Portrait = await portraitFile.GetImage();
                    log.Information($"{ti} artist {artist.Name} portrait updated");
                }
            }
            ti.Status = Music.Core.TaskStatus.Finished;
            ti.FinishedAt = DateTimeOffset.Now;
            await db.SaveChangesAsync();
        }
    }
}

