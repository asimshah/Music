using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly IndianClassicalInformation indianClassicalInformation;
        public CataloguePath(MusicOptions options, long taskId, string connectionString,
            IndianClassicalInformation ici,
            BlockingCollection<TaskQueueItem> taskQueue, PlayManager pm) : base(options, taskId, connectionString, taskQueue)
        {
            this.indianClassicalInformation = ici;
            this.playManager = pm;
        }
        protected override async Task RunTask()
        {
            var pd = MusicMetaDataMethods.GetPathData(musicOptions, taskData);
            if (pd != null)
            {
                Debug.Assert(pd.MusicStyle == musicStyle);
                //try
                //{
                    var results = await this.ExecuteTaskItemWithRetryAsync(async (db) => await CatalogueAsync(db, pd));
                    if (results != null)
                    {
                        foreach (var item in results)
                        {
                            try
                            {
                                var cr = item;//.result;
                                if (cr.Status == CatalogueStatus.Success && item.MusicSet != null)
                                {
                                    //if (cr.TaskItem != null && cr.TaskItem.Id > 0)
                                    //{
                                    //    // possibly do not queue resampling tasks
                                    //    if (cr.TaskItem.Type != TaskType.ResampleWork)
                                    //    {
                                    //        QueueTask(cr.TaskItem);
                                    //    }
                                    //}
                                    //switch (cr.MusicSetType)
                                    //{
                                    //    case Type T when T == typeof(PopularMusicAlbumSet) || T == typeof(WesternClassicalAlbumSet):
                                    //        log.Information($"{taskItem} {T.Name} {cr.ArtistName} {cr.ArtistDescr}, {cr.WorkName} {cr.WorkDescr}, {cr.WorkTrackCount} tracks {cr.TrackContent}");
                                    //        break;
                                    //    case Type T when T == typeof(WesternClassicalCompositionSet):
                                    //        //if (cr.Performance.Movements.Count() == 0)
                                    //        if (cr.PerformanceMovementCount == 0)
                                    //        {
                                    //            log.Warning($"{cr.CompositionName} {cr.CompositionDescr}, \"{cr.PerformersCSV}\" {cr.PerformanceDescr} has no movements");
                                    //        }
                                    //        //var work = cr.Performance.Movements.Select(m => m.Work).First();
                                    //        log.Information($"{taskItem} {T.Name} {cr.ArtistName} {cr.ArtistDescr}, {cr.CompositionName} {cr.CompositionDescr}, {cr.PerformanceMovementCount} movements, \"{cr.PerformersCSV}\" {cr.PerformanceDescr} (from {cr.WorkName} {cr.WorkDescr})");
                                    //        break;
                                    //}
                                    log.Information($"{taskItem} {cr.MusicSetType.Name} {cr}");
                                    // send hub message that artist is new/modified
                                    foreach (var id in cr.ArtistIdListForNotification)
                                    {
                                        await this.playManager.SendArtistNewOrModified(id);
                                    }
                                }
                            }
                            catch (Exception xe)
                            {
                                log.Error(xe, $"[TI-{taskItem}]");
                                throw;
                            }
                        }
                    }
                //}
                //catch(RetryLimitExceededException)
                //{
                //    await SetTaskFailed();
                //}
                //catch (Exception xe)
                //{
                //    log.Error(xe, $"{taskItem}");
                //    await SetTaskFailed();
                //    throw;
                //}
            }
        }



        private async Task<List<BaseCatalogueResult>> CatalogueAsync(MusicDb db, PathData pd)
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
                        log.Information($"{taskItem} {folder}, change {changes}");
                    }
                    return result;
                };
                var results = new List<BaseCatalogueResult>();
                var folder = new OpusFolder(musicOptions, pd);
                if (forceChanges == true || changesPresent(folder))
                {
                    var delay = GetRandomDelay();
                    log.Debug($"{taskItem} starting {folder.ToString()} after delay of {delay}ms");
                    await Task.Delay(TimeSpan.FromMilliseconds(delay));
                    results = await ProcessFolderAsync(db, folder, cd);
                    await db.SaveChangesAsync();
                    var success = results.All(x => x.Status == CatalogueStatus.Success || x.Status == CatalogueStatus.GeneratedFilesOutOfDate);
                    taskItem.Status = success ? Music.Core.TaskStatus.Finished : Music.Core.TaskStatus.Failed;
                }
                else
                {
                    taskItem.Status = Music.Core.TaskStatus.Finished;
                    //results.Add(new CatalogueResult { Status = CatalogueStatus.Success });
                    log.Information($"{taskItem} starting {folder.ToString()} no update required");
                }
                taskItem.FinishedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync();
                return results;
            }
            catch(DbUpdateException due)
            {
                if (due.InnerException is SqlException)
                {
                    var se = due.InnerException as SqlException;
                    log.Error($"{taskItem} DbUpdateException, {se.Message}");
                    //var numbers = se.Errors.OfType<SqlError>().Select(x => x.Number);
                    //if (!(numbers.Contains(1205)))
                    //{
                        
                    //}
                }
                else
                {
                    log.Error($"{taskItem} {(due is DbUpdateConcurrencyException ? due.Message : due.InnerException.Message)}");
                }
                throw new CatalogueFailed { TaskId = taskId };
            }
            catch (Exception xe)
            {
                //var entries = db.ChangeTracker.Entries<Performer>();
                //var performers = entries.Select(x => x.Entity);
                //var targets = performers.Where(x => x.AlphamericName == "FriederikeHaug");
                //var indb = db.Performers.Where(x => x.AlphamericName == "FriederikeHaug");
                //var inlocal = db.Performers.Local.Where(x => x.AlphamericName == "FriederikeHaug");
                log.Error(xe, $"{taskItem}");
                throw new CatalogueFailed { TaskId = taskId };
            }
        }
        private async Task<List<BaseCatalogueResult>> ProcessFolderAsync(MusicDb db, OpusFolder folder, ChangesDetected changes)
        {
            var results = new List<BaseCatalogueResult>();
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
                case ChangesDetected.MusicFileCountHasChanged:
                    shouldDeleteMusicFiles = true;
                    break;
            }
            if (taskItem.Force || shouldDeleteMusicFiles)
            {
                try
                {
                    var deletedFilesCount = folder.RemoveCurrentMusicFiles(db, taskItem); st?.Time("Removal");
                    if (deletedFilesCount > 0)
                    {
                        await db.SaveChangesAsync();
                        log.Information($"{taskItem} {deletedFilesCount} music files removed from db");
                    }
                }
                catch (Exception xe)
                {
                    log.Error(xe);
                    throw;
                }
            }
            var musicFiles = await WriteAudioFilesAsync(db, folder); st?.Time("MusicFiles to DB");
            if (musicFiles.Count() > 0) // count is 0 most often when trying to find singles for an artist
            {
                await UpdateTagsAsync(db, musicFiles, indianClassicalInformation); st?.Time("Extract tags");
                var musicSets = GetMusicSets(db, folder, musicFiles); st?.Time("Split into sets");
                if (musicSets != null)
                {
                    int i = 0;
                    foreach (var set in musicSets)
                    {
                        var cr = await set.CatalogueAsync(); st?.Time($"Set {i++ + 1}");
                        results.Add(cr);
                    } 
                }                
            }
            else
            {
                //results.Add(new CatalogueResult { Status = CatalogueStatus.Success });
            }
            return results;
        }
        private IEnumerable<BaseMusicSet> GetMusicSets(MusicDb db, OpusFolder musicFolder, List<MusicFile> files)
        {

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
                    case MusicStyles.IndianClassical:
                        return new IndianClassicalMusicSetCollection(musicOptions, db, musicFolder, files, taskItem);
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
        private async Task UpdateTagsAsync(MusicDb db, List<MusicFile> files, IndianClassicalInformation ici)
        {
            foreach (var musicFile in files)
            {
                await db.UpdateTagsAsync(musicFile, ici);
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

