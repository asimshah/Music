using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public class DeletePath : TaskBase
    {
        private TaskItem taskItem;
        private readonly PlayManager playManager;
        public DeletePath(MusicOptions options, long taskId, string connectionString, PlayManager pm) : base(options, taskId, connectionString, null)
        {
            this.playManager = pm;
        }

        protected override async Task RunTask()
        {
            var pd = MusicMetaDataMethods.GetPathData(musicOptions, taskData, true);
            if (pd != null)
            {
                await this.ExecuteTaskItemAsync(async (db) =>
                {
                    await DeleteAsync(db, pd);
                });
            }
        }

        private async Task DeleteAsync(MusicDb db, PathData pd)
        {
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            taskItem = await db.TaskItems.FindAsync(taskId);
            IEnumerable<MusicFile> musicFileList = null;
            string path = "";
            if (pd.GetFullOpusPath() != null)
            {
                path = pd.IsCollections ? pd.OpusPath : $"{pd.ArtistPath}\\{pd.OpusPath}";
                musicFileList = db.MusicFiles
                    .Where(f => (f.DiskRoot.ToLower() == pd.DiskRoot.ToLower())
                    && (f.StylePath.ToLower() == pd.StylePath.ToLower())
                    && (f.OpusPath.ToLower() == path.ToLower()))
                    .OrderBy(f => f.File)
                    ;
            }
            else
            {
                if (pd.IsCollections)
                {
                    musicFileList = db.MusicFiles
                        .Where(f => (f.DiskRoot.ToLower() == pd.DiskRoot.ToLower())
                        && (f.StylePath.ToLower() == pd.StylePath.ToLower())
                        && f.Musician.ToLower() == "collections")
                        .OrderBy(f => f.File)
                        ;
                }
                else
                {
                    musicFileList = db.MusicFiles
                        .Where(f => (f.DiskRoot.ToLower() == pd.DiskRoot.ToLower())
                        && (f.StylePath.ToLower() == pd.StylePath.ToLower())
                        && f.OpusPath.StartsWith(pd.ArtistPath))
                        .OrderBy(f => f.File)
                        ;
                }
            }

            log.Information($"Deleting {musicFileList.Count()} music files");
            var dc = new DeleteContext(taskItem);
            foreach (var mf in musicFileList)
            {
                db.Delete(mf, dc);
            }
            taskItem.Status = Music.Core.TaskStatus.Finished;
            taskItem.FinishedAt = DateTimeOffset.Now;
            await db.SaveChangesAsync();
            foreach(var id in dc.DeletedArtistList)
            {
                await this.playManager.SendArtistDeleted(id);
            }
            foreach (var id in dc.ModifiedArtistList)
            {
                var shouldSend = true;
                var artist = await db.Artists.FindAsync(id);
                if (artist != null)
                {
                    shouldSend = artist.Type != ArtistType.Various;
                }
                if (shouldSend)
                {
                    await this.playManager.SendArtistNewOrModified(id);
                }
            }
        }
    }
}

