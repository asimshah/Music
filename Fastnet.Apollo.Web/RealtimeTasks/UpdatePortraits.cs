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

namespace Fastnet.Apollo.Web
{
    public class UpdatePortraits : TaskBase
    {
        public UpdatePortraits(MusicOptions options, long taskId, string connectionString) : base(options, taskId, connectionString, null)
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

