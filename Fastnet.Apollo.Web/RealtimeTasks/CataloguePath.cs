using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Fastnet.Apollo.Web
{
    public class CatalogueFailed : Exception
    {
        public long TaskId { get; set; }
    }
    public class CataloguePath : TaskBase
    {
        private TaskItem taskItem;
        private readonly PlayManager playManager;
        public CataloguePath(MusicOptions options, long taskId, string connectionString, BlockingCollection<TaskQueueItem> taskQueue, PlayManager pm) : base(options, taskId, connectionString, taskQueue)
        {
            this.playManager = pm;
        }
        protected override async Task RunTask()
        {
            var pd = MusicMetaDataMethods.GetPathData(musicOptions, taskData);
            if (pd != null)
            {
                Debug.Assert(pd.MusicStyle == musicStyle);
                var results = await this.ExecuteTaskItemWithRetryAsync(async (db) => await CatalogueAsync(db, pd));
                if (results != null)
                {
                    foreach (var item in results)
                    {
                        var cr = item;//.result;
                        if (cr.Status == CatalogueStatus.Success && item.MusicSet != null)
                        {
                            if (cr.TaskItem != null && cr.TaskItem.Id > 0)
                            {
                                // possibly do not queue resampling tasks
                                if (cr.TaskItem.Type != TaskType.ResampleWork)
                                {
                                    QueueTask(cr.TaskItem);
                                }
                            }
                            switch (cr.MusicSetType)
                            {
                                case Type T when T == typeof(PopularMusicAlbumSet) || T == typeof(WesternClassicalAlbumSet):
                                    log.Information($"{taskItem} {T.Name} {cr.Artist.Name} [A-{cr.Artist.Id}], {cr.Work.Name} [W-{cr.Work.Id}], {cr.Work.Tracks.Count()} tracks {GetTrackContentString(cr.Work)}");
                                    break;
                                case Type T when T == typeof(WesternClassicalCompositionSet):
                                    if (cr.Performance.Movements.Count() == 0)
                                    {
                                        log.Warning($"{cr.Composition.Name} [C-{cr.Composition.Id}], \"{cr.Performance.GetAllPerformersCSV()}\" [P-{cr.Performance.Id}] has no movements");
                                    }
                                    var work = cr.Performance.Movements.Select(m => m.Work).First();
                                    log.Information($"{taskItem} {T.Name} {cr.Artist.Name} [A-{cr.Artist.Id}], {cr.Composition.Name} [C-{cr.Composition.Id}], {cr.Performance.Movements.Count()} movements, \"{cr.Performance.GetAllPerformersCSV()}\" [P-{cr.Performance.Id}] (from {work.Name} [W-{work.Id}])");
                                    break;
                            }
                            // send hub message that artist is new/modified
                            if (cr.Artist.Type != ArtistType.Various)
                            {
                                await this.playManager.SendArtistNewOrModified(cr.Artist.Id);
                            }
                        }
                    }
                }
            }
        }
        private async Task<List<CatalogueResult>> CatalogueAsync(MusicDb db, PathData pd)
        {
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            taskItem = await db.TaskItems.FindAsync(taskId);
            try
            {
                ChangesDetected cd = ChangesDetected.None;
                bool changesPresent(OpusFolder folder)
                {
                    var (result, changes) = folder.CheckForChanges(db);
                    cd = changes;
                    if(result)
                    {
                        log.Information($"{folder}, change {changes}");
                    }
                    return result;
                };
                var results = new List<CatalogueResult>();
                var folder = new OpusFolder(musicOptions, pd);
                //if (forceChanges == true || folder.CheckForChanges(db))
                if (forceChanges == true || changesPresent(folder))
                {
                    var delay = GetRandomDelay();
                    log.Debug($"{taskItem} starting {folder.ToString()} after delay of {delay}ms");
                    await Task.Delay(TimeSpan.FromMilliseconds(delay));
                    results = await ProcessFolderAsync(db, folder, cd);
                    var success = results.All(x => x.Status == CatalogueStatus.Success || x.Status == CatalogueStatus.GeneratedFilesOutOfDate);
                    taskItem.Status = success ? Music.Core.TaskStatus.Finished : Music.Core.TaskStatus.Failed;
                }
                else
                {
                    taskItem.Status = Music.Core.TaskStatus.Finished;
                    results.Add(new CatalogueResult { Status = CatalogueStatus.Success });
                    log.Information($"{taskItem} starting {folder.ToString()} no update required");
                }
                taskItem.FinishedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync();
                return results;
            }
            catch (Exception xe)
            {
                log.Error(xe, $"task {taskItem}");
                throw new CatalogueFailed { TaskId = taskId };
            }
        }
        private async Task<List<CatalogueResult>> ProcessFolderAsync(MusicDb db, OpusFolder folder, ChangesDetected changes)
        {
            var results = new List<CatalogueResult>();
            db.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
            StepTimer st = null;
            if (musicOptions.TimeCatalogueSteps)
            {
                st = new StepTimer();
                st.Start();
            }
            var shouldDeleteMusicFiles = false;
            switch(changes)
            {
                case ChangesDetected.AtLeastOneFileNotCatalogued:
                case ChangesDetected.AtLeastOneFileModifiedOnDisk:
                    shouldDeleteMusicFiles = true;
                    break;
            }
            if (/*true ||*/ taskItem.Force || shouldDeleteMusicFiles)
            {
                var deletedFilesCount = folder.RemoveCurrentMusicFiles(db); st?.Time("Removal");
                if (deletedFilesCount > 0)
                {
                    await db.SaveChangesAsync();
                }
            }
            var musicFiles = await WriteAudioFilesAsync(db, folder); st?.Time("MusicFiles to DB");
            if (musicFiles.Count() > 0) // count is 0 most often when trying to find singles for an artist
            {
                await UpdateTagsAsync(db, musicFiles); st?.Time("Extract tags");
                var musicSets = GetMusicSets(db, folder, musicFiles); st?.Time("Split into sets");
                if (musicSets != null)
                {
                    int i = 0;
                    foreach (var set in musicSets)
                    {
                        var cr = await set.CatalogueAsync(); st?.Time($"Set {i++ + 1}");
                        results.Add(cr);
                        if (cr.Status == CatalogueStatus.Success)
                        {
                            if (cr.Artist == null)
                            {
                                log.Trace($"Artist missing");
                            }
                            switch (cr.MusicSetType)
                            {
                                case Type T when T == typeof(PopularMusicAlbumSet) || T == typeof(WesternClassicalAlbumSet):
                                    if (cr.Work == null)
                                    {
                                        log.Trace($"Album missing");
                                    }
                                    else if (cr.Work.Tracks == null || cr.Work.Tracks.Count() == 0)
                                    {
                                        log.Trace($"Work has no tracks");
                                    }
                                    if (cr.Tracks == null)
                                    {
                                        log.Trace($"Tracks missing");
                                    }
                                    else if (cr.Tracks.Count() == 0)
                                    {
                                        log.Trace($"Track count is 0");
                                    }
                                    //if(cr.TaskItem != null)
                                    //{
                                    //    QueueTask(cr.TaskItem);
                                    //}
                                    break;
                                case Type T when T == typeof(WesternClassicalCompositionSet):
                                    if (cr.Composition == null)
                                    {
                                        log.Trace($"Composition missing");
                                    }
                                    if (cr.Performance == null)
                                    {
                                        log.Trace($"Performance missing");
                                    }
                                    else if (cr.Performance.Movements == null || cr.Performance.Movements.Count() == 0)
                                    {
                                        log.Trace($"Performance has no movements");
                                    }

                                    break;
                            }
                        }
                    } 
                }                
            }
            else
            {
                results.Add(new CatalogueResult { Status = CatalogueStatus.Success });
            }
            return results;
        }
        private IEnumerable<IMusicSet> GetMusicSets(MusicDb db, OpusFolder musicFolder, List<MusicFile> files)
        {
            //Debug.Assert(ValidateMusicFileSet(db, files) == true);
            if (ValidateMusicFileSet(db, files))
            {
                var style = files.First().Style;
                switch (style)
                {
                    default:
                    case MusicStyles.Popular:
                        return new PopularMusicSetCollection(musicOptions, db, musicFolder, files, taskItem);
                    case MusicStyles.WesternClassical:
                        return new WesternClassicalMusicSetCollection(musicOptions, db, musicFolder, files, taskItem);
                }
            }
            else
            {
                log.Warning($"{musicFolder.ToString()} not catalogued");
                return null;
            }
        }
        private bool ValidateMusicFileSet(MusicDb db, List<MusicFile> files)
        {
            // 0. make sure there are some files
            if (!(files.Count > 0))
            {
                log.Warning($"ValidateMusicFileSet(): file set is empty");
                return false;
            }
            // 1. make sure that all files are in the same original folder and in the same style
            if (!files.All(f => f.Style == files[0].Style && f.DiskRoot == files[0].DiskRoot && f.StylePath == files[0].StylePath && f.OpusPath == files[0].OpusPath))
            {
                log.Warning($"ValidateMusicFileSet(): all files are not from the same original folder");
                return false;
            }
            // 2. make sure  all files are of the same opustype, i.e.  either in a collection or not in a collection, or singles
            if (!files.All(f => f.OpusType == files[0].OpusType))
            {
                log.Warning($"ValidateMusicFileSet(): all files are not of the same opus type");
                return false;
            }
            foreach (var file in files)
            {
                if (!db.ValidateTags(file))
                {
                    return false;
                }
            }
            return true;
        }
        private async Task UpdateTagsAsync(MusicDb db, List<MusicFile> files/*, bool force*/)
        {
            foreach (var musicFile in files)
            {
                await db.UpdateTagsAsync(musicFile/*, force*/);
            }
        }
        private async Task<List<MusicFile>> WriteAudioFilesAsync(MusicDb db, OpusFolder folder)
        {
            var list = await folder.UpdateAudioFilesToDbAsync(db);
            return list;
        }
        private int GetRandomDelay()
        {
            var r = new Random();
            return r.Next(1000, 5000);
        }
        private string GetTrackContentString(Work album)
        {
            var strings = new List<string>();
            var musicFiles = album.Tracks.First().MusicFiles;
            foreach (var mf in musicFiles)
            {
                var text = $"{mf.Encoding}{(mf.IsGenerated ? " (generated)" : string.Empty)}";
                strings.Add(text);
            }
            return $"({string.Join(", ", strings)})";
        }
    }
}

