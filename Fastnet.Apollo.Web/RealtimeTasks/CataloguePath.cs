using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private MusicDb musicDb;
        private readonly IHubContext<MessageHub, IHubMessage> messageHub;
        //private readonly IndianClassicalInformation indianClassicalInformation;
        private readonly IOptionsMonitor<IndianClassicalInformation> monitoredIndianClassicalInformation;
        private readonly IOptionsMonitor<MusicOptions> optionsMonitor;
        private readonly EntityHelper entityHelper;
        public CataloguePath(
            IOptionsMonitor<MusicOptions> optionsMonitor, IHubContext<MessageHub, IHubMessage> messageHub,
            EntityHelper entityHelper, IOptionsMonitor<IndianClassicalInformation> monitoredIndianClassicalInformation,// IndianClassicalInformation ici,
            ILogger<CataloguePath> log, IConfiguration cfg, IWebHostEnvironment environment) : base(log, cfg, environment)
        {
            this.optionsMonitor = optionsMonitor;
            this.entityHelper = entityHelper;
            this.messageHub = messageHub;
            //this.indianClassicalInformation = ici;
            this.monitoredIndianClassicalInformation = monitoredIndianClassicalInformation;
        }

        //public CataloguePathOld(MusicOptions options, long taskId, string connectionString,
        //    IndianClassicalInformation ici, BlockingCollection<TaskQueueItem> taskQueue,
        //    IOptions<MusicServerOptions> serverOptions, IHubContext<MessageHub, IHubMessage> messageHub, ILoggerFactory loggerFactory) : base(options, taskId, connectionString, taskQueue)
        //{
        //    this.indianClassicalInformation = ici;
        //    this.libraryService = new LibraryService(serverOptions, messageHub, loggerFactory.CreateLogger<LibraryService>(), new MusicDb(connectionString));// lm;
        //}
        protected override async Task RunTask()
        {
            var results = await this.ExecuteTaskItemWithRetryAsync(async (db) =>
            {
                return await CatalogueAsync(db);
            });
            if (results != null)
            {
                foreach (var item in results)
                {
                    try
                    {
                        var cr = item;
                        if (cr.Status == CatalogueStatus.Success && item.MusicSet != null)
                        {
                            log.Information($"{taskItem} {cr.MusicSetType.Name} {cr}");
                            // send hub message that artist is new/modified
                            foreach (var id in cr.ArtistIdListForNotification)
                            {
                                await SendArtistNewOrModified(id);
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
        }
        public async Task SendArtistNewOrModified(long id)
        {
            try
            {
                await this.messageHub.Clients.All.SendArtistNewOrModified(id);
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }

        }
        private async Task<List<BaseCatalogueResult>> CatalogueAsync(MusicDb db/*, PathData pd*/)
        {
            this.musicDb = db;
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            taskItem = await db.TaskItems.FindAsync(taskId);
            this.entityHelper.Enable(db, taskItem);
            var mpa = MusicRoot.AnalysePath(optionsMonitor.CurrentValue, taskItem.TaskString);
            var results = new List<BaseCatalogueResult>();
            try
            {
                ChangesDetected cd = ChangesDetected.None;
                bool changesPresent(WorkFolder wf)
                {
                    var (result, changes) = CheckForChanges(wf);// folder.CheckForChanges(db);
                    cd = changes;
                    if (result)
                    {
                        log.Debug($"{taskItem} {wf}, change {changes}");
                    }
                    return result;
                };

                //var folder = new OpusFolder(musicOptions, pd);
                //if(mpa.ToplevelName.IsEqualIgnoreAccentsAndCase("Marcie Blane"))
                //{
                //    Debugger.Break();
                //}
                var tf = mpa.GetFolder();
                var folder = tf switch
                {
                    ArtistFolder af => af.GetSinglesFolder() ,//as WorkFolder,
                    WorkFolder wf => wf,// as WorkFolder,
                    _ => throw new NotImplementedException()
                };
                //var folder = mpa.GetFolder() as WorkFolder; //!!!!!!!!!!!!!!!!! can be an artist folder - means singles?
                if (folder != null)
                {
                    if (taskItem.Force == true || changesPresent(folder))
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
                        log.Information($"{taskItem} {folder.ToString()} no update required");
                    }
                    taskItem.FinishedAt = DateTimeOffset.Now;
                    await db.SaveChangesAsync(); 
                }
                else
                {
                    log.Error($"mpa.GetFolder(): {mpa}");
                }
                //return results;
            }
            catch (DbUpdateException due)
            {
                if (due.InnerException is SqlException)
                {
                    var se = due.InnerException as SqlException;
                    log.Warning($"{taskItem} DbUpdateException, {se.Message}");
                }
                else
                {
                    log.Warning($"{taskItem} {(due is DbUpdateConcurrencyException ? due.Message : due.InnerException.Message)}");
                }
                throw new CatalogueFailed { TaskId = taskId };
            }
            catch (Exception xe)
            {
                log.Error(xe, $"{taskItem}");
                throw new CatalogueFailed { TaskId = taskId };
            }
            return results;
        }
        private async Task<List<BaseCatalogueResult>> ProcessFolderAsync(MusicDb db, WorkFolder folder, ChangesDetected changes)
        {
            var results = new List<BaseCatalogueResult>();
            db.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
            var shouldDeleteMusicFiles = false;
            switch (changes)
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
                    var currentMusicFiles = folder.GetFilesInDb(this.entityHelper);
                    var deletedFilesCount = currentMusicFiles.Count();// folder.RemoveCurrentMusicFiles(db, taskItem);
                    if (deletedFilesCount > 0)
                    {
                        foreach (var mf in currentMusicFiles)
                        {
                            entityHelper.Delete(mf);
                        }
                        await db.SaveChangesAsync();
                        var reason = taskItem.Force ? "forced removal" : $"{changes}";
                        log.Information($"{taskItem} {deletedFilesCount} music files removed from db, reason: {reason}");
                    }
                }
                catch (Exception xe)
                {
                    log.Error(xe);
                    throw;
                }
            }
            var musicFiles = await UpdateAudioFilesToDbAsync(folder);
            if (musicFiles.Count() > 0) // count is 0 most often when trying to find singles for an artist
            {
                await UpdateTagsAsync(musicFiles);
                var musicSets = GetMusicSets(/*db,*/ folder, musicFiles);
                if (musicSets != null)
                {
                    //int i = 0;
                    foreach (var set in musicSets)
                    {
                        var cr = await set.CatalogueAsync();
                        results.Add(cr);
                    }
                }
            }
            return results;
        }
        private IEnumerable<BaseMusicSet> GetMusicSets(/*MusicDb db,*/ WorkFolder musicFolder, List<MusicFile> files)
        {

            if (ValidateMusicFileSet(/*db,*/ files))
            {
                var style = files.First().Style;
                switch (style)
                {
                    default:
                    case MusicStyles.Popular:
                        return new PopularMusicSetCollection(optionsMonitor.CurrentValue, entityHelper, musicFolder, files, taskItem);
                    case MusicStyles.WesternClassical:
                        return new WesternClassicalMusicSetCollection(optionsMonitor.CurrentValue, entityHelper, musicFolder, files, taskItem);
                    case MusicStyles.IndianClassical:
                        return new IndianClassicalMusicSetCollection(optionsMonitor.CurrentValue, /*indianClassicalInformation,*/ entityHelper, musicFolder, files, taskItem);
                    case MusicStyles.HindiFilms:
                        return new HindiFilmsMusicSetCollection(optionsMonitor.CurrentValue, entityHelper, musicFolder, files, taskItem);
                }
            }
            else
            {
                log.Error($"{musicFolder.ToString()} not catalogued");
                return null;
            }
        }
        private bool ValidateMusicFileSet(/*MusicDb db,*/ List<MusicFile> files)
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
                if (!entityHelper.ValidateTags(file))
                {
                    return false;
                }
            }
            return true;
        }
        private async Task UpdateTagsAsync(List<MusicFile> files/*, IndianClassicalInformation ici*/)
        {
            foreach (var musicFile in files)
            {
                await musicDb.UpdateTagsAsync(musicFile, monitoredIndianClassicalInformation.CurrentValue);
            }
        }
        //private async Task<List<MusicFile>> WriteAudioFilesAsync(MusicDb db, OpusFolder folder)
        //{
        //    var list = await folder.UpdateAudioFilesToDbAsync(db);
        //    return list;
        //}
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
        private (bool result, ChangesDetected changes) CheckForChanges(WorkFolder wf)
        {
            ChangesDetected changesDetected = ChangesDetected.None;
            bool result = false;
            var currentMusicFiles = wf.GetFilesInDb(this.entityHelper);// GetMusicFilesFromDb(db);
            var filesOnDisk = wf.GetFilesOnDisk();// GetFilesOnDisk();
            bool anyFilesNotCatalogued()
            {
                bool r = false;
                var list = currentMusicFiles.Where(x => x.Track == null);
                r = list.Count() > 0;
                if (r)
                {
                    changesDetected = ChangesDetected.AtLeastOneFileNotCatalogued;
                }
                return r;
            }
            bool anyFilesRewritten()
            {
                bool r = false;
                var l1 = currentMusicFiles.Select(x => x.FileLastWriteTimeUtc);
                var l2 = filesOnDisk.Select(x => new DateTimeOffset(x.fi.LastWriteTimeUtc, TimeSpan.Zero));
                r = !l1.SequenceEqual(l2);
                if (r)
                {
                    changesDetected = ChangesDetected.AtLeastOneFileModifiedOnDisk;
                    log.Trace($"anyFilesRewritten() returns true");
                }
                return r;
            }

            bool additionsOrDeletionsExist()
            {
                var differences = filesOnDisk.Select(f => f.fi.FullName).Except(currentMusicFiles.Select(mf => mf.File), StringComparer.CurrentCultureIgnoreCase);
                var r = differences.Count() != 0;
                if (r)
                {
                    changesDetected = ChangesDetected.MusicFileCountHasChanged;
                    log.Debug($"music file difference count is {differences.Count()}");
                }
                return r;
            }
            bool anyImageChanged()
            {
                bool r = false;
                var works = currentMusicFiles.Select(mf => mf.Track).Select(x => x.Work).Distinct();
                var artists = works.SelectMany(x => x.Artists)
                    .Union(currentMusicFiles.Where(mf => mf.Track.Performance != null)
                        .Select(x => x.Track.Performance)
                        .Where(x => x.GetComposition() != null).Select(x => x.GetComposition().Artist))
                    .Union(currentMusicFiles.Where(mf => mf.Track.Performance != null)
                        .Select(x => x.Track.Performance)
                        .Where(x => x.GetRaga() != null).SelectMany(x => x.RagaPerformances.Select(rp => rp.Artist)))
                    .Distinct();
                foreach (var artist in artists.Where(a => a.Type != ArtistType.Various))
                {
                    var f = artist.GetPortraitFile(optionsMonitor.CurrentValue);
                    if (artist.Portrait.HasChanged(f))
                    {
                        log.Debug($"artist {artist.Name}, portrait file {f} found");
                        r = true;
                        break;
                    }
                }
                if (!r)
                {
                    foreach (var work in works)
                    {
                        var coverFile = work.GetMostRecentOpusCoverFile(optionsMonitor.CurrentValue);
                        if (work.Cover.HasChanged(coverFile))
                        {
                            log.Debug($"artist(s) {work.GetArtistNames()}, work {work.Name}, cover art file {coverFile}");
                            r = true;
                            break;
                        }
                    }
                }
                if (r)
                {
                    changesDetected = ChangesDetected.CoverArtHasChanged;
                    //log.Trace($"anyImageChanged() returns true");
                }
                return r;
            }
            if (additionsOrDeletionsExist() /*|| musicTagsAreNew()*/ || anyFilesRewritten() || anyFilesNotCatalogued() || anyImageChanged())
            {
                result = true;
            }
            return (result, changesDetected);
        }
        private async Task<List<MusicFile>> UpdateAudioFilesToDbAsync(WorkFolder wf/*, MusicDb db*/)
        {
            var musicFiles = new List<MusicFile>();
            //foreach (var audioFile in new AudioFileCollection(this))
            //foreach (var item in wf.GetFilesOnDisk())
            foreach (var af in wf.GetAudioFiles())
            {
                //AudioFile af = null;
                //switch (item.fi.Extension.ToLower())
                //{
                //    case ".mp3":
                //        af = new Mp3File(item.fi);
                //        break;
                //    case ".flac":
                //        af = new FlacFile(item.fi);
                //        break;
                //}
                var mf = await wf.AddMusicFile(entityHelper, af);
                musicFiles.Add(mf);
            }
            return musicFiles;
        }

    }
    //public class CataloguePathOld : TaskBaseOld
    //{
    //    private TaskItem taskItem;
    //    private readonly LibraryService libraryService;
    //    private readonly IndianClassicalInformation indianClassicalInformation;
    //    public CataloguePathOld(MusicOptions options, long taskId, string connectionString,
    //        IndianClassicalInformation ici, BlockingCollection<TaskQueueItem> taskQueue,
    //        IOptions<MusicServerOptions> serverOptions, IHubContext<MessageHub, IHubMessage> messageHub, ILoggerFactory loggerFactory) : base(options, taskId, connectionString, taskQueue)
    //    {
    //        this.indianClassicalInformation = ici;
    //        this.libraryService = new LibraryService(serverOptions, messageHub, loggerFactory.CreateLogger<LibraryService>(), new MusicDb(connectionString));// lm;
    //    }
    //    protected override async Task RunTask()
    //    {
    //        var pd = MusicMetaDataMethods.GetPathData(musicOptions, taskData);
    //        if (pd != null)
    //        {
    //            Debug.Assert(pd.MusicStyle == musicStyle);
    //            var results = await this.ExecuteTaskItemWithRetryAsync(async (db) => await CatalogueAsync(db, pd));
    //            if (results != null)
    //            {
    //                foreach (var item in results)
    //                {
    //                    try
    //                    {
    //                        var cr = item;
    //                        if (cr.Status == CatalogueStatus.Success && item.MusicSet != null)
    //                        {
    //                            log.Information($"{taskItem} {cr.MusicSetType.Name} {cr}");
    //                            // send hub message that artist is new/modified
    //                            foreach (var id in cr.ArtistIdListForNotification)
    //                            {
    //                                await this.libraryService.SendArtistNewOrModified(id);
    //                            }
    //                        }
    //                    }
    //                    catch (Exception xe)
    //                    {
    //                        log.Error(xe, $"[TI-{taskItem}]");
    //                        throw;
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    private async Task<List<BaseCatalogueResult>> CatalogueAsync(MusicDb db, PathData pd)
    //    {
    //        db.ChangeTracker.AutoDetectChangesEnabled = false;
    //        taskItem = await db.TaskItems.FindAsync(taskId);
    //        try
    //        {
    //            ChangesDetected cd = ChangesDetected.None;
    //            bool changesPresent(OpusFolder folder)
    //            {
    //                var (result, changes) = folder.CheckForChanges(db);
    //                cd = changes;
    //                if (result)
    //                {
    //                    log.Information($"{taskItem} {folder}, change {changes}");
    //                }
    //                return result;
    //            };
    //            var results = new List<BaseCatalogueResult>();
    //            var folder = new OpusFolder(musicOptions, pd);
    //            if (forceChanges == true || changesPresent(folder))
    //            {
    //                var delay = GetRandomDelay();
    //                log.Debug($"{taskItem} starting {folder.ToString()} after delay of {delay}ms");
    //                await Task.Delay(TimeSpan.FromMilliseconds(delay));
    //                results = await ProcessFolderAsync(db, folder, cd);
    //                await db.SaveChangesAsync();
    //                var success = results.All(x => x.Status == CatalogueStatus.Success || x.Status == CatalogueStatus.GeneratedFilesOutOfDate);
    //                taskItem.Status = success ? Music.Core.TaskStatus.Finished : Music.Core.TaskStatus.Failed;
    //            }
    //            else
    //            {
    //                taskItem.Status = Music.Core.TaskStatus.Finished;
    //                log.Information($"{taskItem} starting {folder.ToString()} no update required");
    //            }
    //            taskItem.FinishedAt = DateTimeOffset.Now;
    //            await db.SaveChangesAsync();
    //            return results;
    //        }
    //        catch (DbUpdateException due)
    //        {
    //            if (due.InnerException is SqlException)
    //            {
    //                var se = due.InnerException as SqlException;
    //                log.Error($"{taskItem} DbUpdateException, {se.Message}");
    //            }
    //            else
    //            {
    //                log.Warning($"{taskItem} {(due is DbUpdateConcurrencyException ? due.Message : due.InnerException.Message)}");
    //            }
    //            throw new CatalogueFailed { TaskId = taskId };
    //        }
    //        catch (Exception xe)
    //        {
    //            log.Error(xe, $"{taskItem}");
    //            throw new CatalogueFailed { TaskId = taskId };
    //        }
    //    }
    //    private async Task<List<BaseCatalogueResult>> ProcessFolderAsync(MusicDb db, OpusFolder folder, ChangesDetected changes)
    //    {
    //        var results = new List<BaseCatalogueResult>();
    //        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
    //        StepTimer st = null;
    //        if (musicOptions.TimeCatalogueSteps)
    //        {
    //            st = new StepTimer();
    //            st.Start();
    //        }
    //        var shouldDeleteMusicFiles = false;
    //        switch (changes)
    //        {
    //            case ChangesDetected.AtLeastOneFileNotCatalogued:
    //            case ChangesDetected.AtLeastOneFileModifiedOnDisk:
    //            case ChangesDetected.MusicFileCountHasChanged:
    //                shouldDeleteMusicFiles = true;
    //                break;
    //        }
    //        if (taskItem.Force || shouldDeleteMusicFiles)
    //        {
    //            try
    //            {
    //                var deletedFilesCount = folder.RemoveCurrentMusicFiles(db, taskItem); st?.Time("Removal");
    //                if (deletedFilesCount > 0)
    //                {
    //                    await db.SaveChangesAsync();
    //                    log.Information($"{taskItem} {deletedFilesCount} music files removed from db");
    //                }
    //            }
    //            catch (Exception xe)
    //            {
    //                log.Error(xe);
    //                throw;
    //            }
    //        }
    //        var musicFiles = await WriteAudioFilesAsync(db, folder); st?.Time("MusicFiles to DB");
    //        if (musicFiles.Count() > 0) // count is 0 most often when trying to find singles for an artist
    //        {
    //            await UpdateTagsAsync(db, musicFiles, indianClassicalInformation); st?.Time("Extract tags");
    //            var musicSets = GetMusicSets(db, folder, musicFiles); st?.Time("Split into sets");
    //            if (musicSets != null)
    //            {
    //                int i = 0;
    //                foreach (var set in musicSets)
    //                {
    //                    var cr = await set.CatalogueAsync(); st?.Time($"Set {i++ + 1}");
    //                    results.Add(cr);
    //                }
    //            }
    //        }
    //        return results;
    //    }
    //    private IEnumerable<BaseMusicSet> GetMusicSets(MusicDb db, WorkFolder musicFolder, List<MusicFile> files)
    //    {

    //        if (ValidateMusicFileSet(db, files))
    //        {
    //            var style = files.First().Style;
    //            switch (style)
    //            {
    //                default:
    //                case MusicStyles.Popular:
    //                    return new PopularMusicSetCollection(musicOptions, db, musicFolder, files, taskItem);
    //                case MusicStyles.WesternClassical:
    //                    return new WesternClassicalMusicSetCollection(musicOptions, db, musicFolder, files, taskItem);
    //                case MusicStyles.IndianClassical:
    //                    return new IndianClassicalMusicSetCollection(musicOptions, indianClassicalInformation, db, musicFolder, files, taskItem);
    //                case MusicStyles.HindiFilms:
    //                    return new HindiFilmsMusicSetCollection(musicOptions, db, musicFolder, files, taskItem);
    //            }
    //        }
    //        else
    //        {
    //            log.Error($"{musicFolder.ToString()} not catalogued");
    //            return null;
    //        }
    //    }
    //    private bool ValidateMusicFileSet(MusicDb db, List<MusicFile> files)
    //    {
    //        // 0. make sure there are some files
    //        if (!(files.Count > 0))
    //        {
    //            log.Warning($"ValidateMusicFileSet(): file set is empty");
    //            return false;
    //        }
    //        // 1. make sure that all files are in the same original folder and in the same style
    //        if (!files.All(f => f.Style == files[0].Style && f.DiskRoot == files[0].DiskRoot && f.StylePath == files[0].StylePath && f.OpusPath == files[0].OpusPath))
    //        {
    //            log.Warning($"ValidateMusicFileSet(): all files are not from the same original folder");
    //            return false;
    //        }
    //        // 2. make sure  all files are of the same opustype, i.e.  either in a collection or not in a collection, or singles
    //        if (!files.All(f => f.OpusType == files[0].OpusType))
    //        {
    //            log.Warning($"ValidateMusicFileSet(): all files are not of the same opus type");
    //            return false;
    //        }
    //        foreach (var file in files)
    //        {
    //            if (!db.ValidateTags(file))
    //            {
    //                return false;
    //            }
    //        }
    //        return true;
    //    }
    //    private async Task UpdateTagsAsync(MusicDb db, List<MusicFile> files, IndianClassicalInformation ici)
    //    {
    //        foreach (var musicFile in files)
    //        {
    //            await db.UpdateTagsAsync(musicFile, ici);
    //        }
    //    }
    //    private async Task<List<MusicFile>> WriteAudioFilesAsync(MusicDb db, OpusFolder folder)
    //    {
    //        var list = await folder.UpdateAudioFilesToDbAsync(db);
    //        return list;
    //    }
    //    private int GetRandomDelay()
    //    {
    //        var r = new Random();
    //        return r.Next(1000, 5000);
    //    }
    //    private string GetTrackContentString(Work album)
    //    {
    //        var strings = new List<string>();
    //        var musicFiles = album.Tracks.First().MusicFiles;
    //        foreach (var mf in musicFiles)
    //        {
    //            var text = $"{mf.Encoding}{(mf.IsGenerated ? " (generated)" : string.Empty)}";
    //            strings.Add(text);
    //        }
    //        return $"({string.Join(", ", strings)})";
    //    }
    //}
}

