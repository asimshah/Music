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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    class FolderChange
    {
        public PathData PathData { get; set; }
        public bool Deleted { get; set; } // if false then path is new, or modified
        //public string Path { get; set; }
        public override string ToString()
        {
            return $"{PathData}{(Deleted ? " (deleted)" : "")}";
        }
    }
    public class MusicFolderChangeMonitor : HostedService // RealtimeTask
    {
        private readonly Queue<List<(string Path, bool Deleted)>> changeListList = new Queue<List<(string Path, bool Deleted)>>();
        private List<FolderChange> changeList;
        private MusicOptions musicOptions;
        private CancellationToken cancellationToken;
        //private DateTimeOffset lastChangeTime = DateTimeOffset.Now;
        private IDictionary<string, FileSystemMonitor> sources;
        private readonly FileSystemMonitorFactory mf;
        private readonly TaskPublisher publisher;
        private readonly string connectionString;
        public MusicFolderChangeMonitor(IConfiguration cfg, IWebHostEnvironment environment,
            TaskPublisher publisher, IOptionsMonitor<MusicOptions> mo,
            FileSystemMonitorFactory mf, ILogger<MusicFolderChangeMonitor> logger) : base(logger)
        {
            connectionString = environment.LocaliseConnectionString(cfg.GetConnectionString("MusicDb"));
            this.publisher = publisher;
            this.musicOptions = mo.CurrentValue;
            this.mf = mf;
            mo.OnChangeWithDelay<MusicOptions>((opt) =>
            {
                this.musicOptions = opt;
                Restart();
            });
        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            log.Trace($"{nameof(ExecuteAsync)}");
            this.cancellationToken = cancellationToken;
            await StartAsync();
        }
        private void Restart()
        {
            try
            {
                log.Information($"{nameof(Restart)}");
                CleanUp();
                Initialise();
                Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            await ProcessChanges();
                            log.Error("ProcessChanges() ended!!!!!!!!!!!!!!!");
                        }
                        catch (Exception xe)
                        {
                            log.Error(xe, "ProcessChanges() failed");
                            //throw;
                        } 
                    }
                }, cancellationToken);
            }
            catch (Exception xe)
            {
                log.Error(xe, "Restart failed");
            }
            return;
        }
        private async Task StartAsync()
        {
            bool CheckPathsAccessible()
            {
                var result = true;
                foreach (var path in sources.Values.Select(x => x.Path))
                {
                    if (path.CanAccess(true) == false)
                    {
                        result = false;
                        log.Warning($"{path} no longer writable - restarting");
                        break;
                    }
                }
                return result;
            }
            Restart();
            // this loop simply holds this coomponent active
            while (!this.cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(musicOptions.FolderChangeDiskAccessCheckInterval, cancellationToken);
                if (!CheckPathsAccessible())
                {
                    Restart();
                }
            }
            if (cancellationToken.IsCancellationRequested)
            {
                CleanUp();
            }
            log.Debug($"CancellationRequested");
        }
        private void Initialise()
        {
            sources = new Dictionary<string, FileSystemMonitor>();
            foreach (var style in musicOptions.Styles)
            {
                var stylePaths = style.Style.GetPaths(musicOptions, false, false);
                foreach (var sp in stylePaths)
                {
                    var fsm = AddMonitor(sp);
                    sources.Add(sp, fsm);
                    log.Information($"Monitor added for {sp}");
                }
            }
            foreach (var kvp in sources)
            {
                kvp.Value.Start();
            }

            changeList = new List<FolderChange>();
        }
        //private void OnChangeOccurred(string folderName, IEnumerable<FileSystemMonitorEvent> actions)
        //{
        //    foreach (FileSystemMonitorEvent item in actions)
        //    {
        //        if (!item.Path.EndsWith(".txt") && !item.Path.EndsWith(".json"))
        //        {
        //            log.Debug($"{item.Type}, path {item.Path}, old path {item.OldPath ?? "none"}");
        //            ShouldProcess(item.Path, item.Type == WatcherChangeTypes.Deleted);
        //        }
        //    }
        //}
        private void ProcessChangeList(List<(string Path, bool Deleted)> list)
        {
            log.Information($"processing list of {list.Count()} items");
            foreach(var item in list)
            {
                ShouldProcess(item.Path, item.Deleted);
            }
        }
        private bool ShouldProcess(string path, bool deleted)
        {
            void addIfNotPresent(PathData pd, bool deleted = false)
            {
                if (changeList.SingleOrDefault(x => x.PathData == pd && x.Deleted == deleted) == null)
                {
                    var fc = new FolderChange { PathData = pd,/* Path = path,*/ Deleted = deleted };
                    changeList.Add(fc);
                    log.Debug($"{fc}, changeList total now {changeList.Count()}");
                }
            }
            var result = false;
            switch(Path.GetExtension(path).ToLower())
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                    var oldPath = path;
                    path = Path.GetDirectoryName(path);
                    //log.Debug($"{oldPath} changed to {path}");
                    break;
            }
            var pd = MusicMetaDataMethods.GetPathData(musicOptions, path, true);
            if (pd != null)
            {
                if (pd.IsPortraits)
                {
                    var portraitsPath = pd.GetPortraitsPath();
                    //addIfNotPresent(portraitsPath);
                    addIfNotPresent(pd);
                }
                else
                {
                    if (pd.GetFullOpusPath() != null)
                    {
                        using (var db = new MusicDb(connectionString))
                        {
                            if (!Directory.Exists(pd.GetFullOpusPath()))
                            {
                                addIfNotPresent(pd, true);
                                //log.Warning($"{pd}, deleted folder {pd.GetFullOpusPath()} not yet supported");
                            }
                            else
                            {
                                var filesOnDisk = Directory.EnumerateFiles(pd.GetFullOpusPath(), "*.flac", SearchOption.AllDirectories)
                                    .Union(Directory.EnumerateFiles(pd.GetFullOpusPath(), "*.mp3", SearchOption.AllDirectories));
                                var opusPath = pd.IsCollections ? pd.OpusPath : Path.Combine(pd.ArtistPath, pd.OpusPath);
                                var filesInDb = db.MusicFiles.Where(mf => mf.DiskRoot.ToLower() == pd.DiskRoot.ToLower()
                                    && mf.StylePath.ToLower() == pd.StylePath.ToLower()
                                    && mf.OpusPath.ToLower() == opusPath.ToLower());
                                if (filesOnDisk.Count() == 0 && filesInDb.Count() == 0)
                                {
                                    // how can this happen?
                                    log.Error($"{pd}, no files on disk and no files in the db!!");
                                }
                                else if (filesOnDisk.Count() == filesInDb.Count())
                                {
                                    // existing files
                                    var works = filesInDb.Select(x => x.Track.Work).Distinct();
                                    if (works.Count() != 1)
                                    {
                                        log.Error($"{pd}, has multiple works in the db");
                                    }
                                    else
                                    {
                                        DateTime getCoverFileDate()
                                        {
                                            IEnumerable<string> imageFiles = null;
                                            foreach(var pattern in musicOptions.CoverFilePatterns)
                                            {
                                                imageFiles = imageFiles?.Union(Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories)) ?? Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories);
                                            }
                                            if(imageFiles.Count() > 0)
                                            {
                                                return imageFiles.Select(f => new FileInfo(f).LastWriteTime).Max();
                                            }
                                            return DateTime.MinValue;
                                        }
                                        var work = works.First();

                                        var latestWriteDate = filesOnDisk.Select(x => new FileInfo(x)).Max(x => x.LastWriteTime);
                                        if (work.LastModified < latestWriteDate || work.LastModified < getCoverFileDate())
                                        {
                                            addIfNotPresent(pd);
                                        }
                                    }
                                }
                                else if (filesOnDisk.Count() == 0)
                                {
                                    // deletions
                                    addIfNotPresent(pd, true);
                                }
                                else
                                {
                                    //new music
                                    addIfNotPresent(pd);
                                }
                            }
                        }
                    }
                    else
                    {
                        var artistPath = pd.GetFullArtistPath();//.ToLower();
                        if (!deleted)
                        {
                            //log.Information($"Artist path {artistPath} added");
                            //addIfNotPresent(artistPath);
                            addIfNotPresent(pd);
                        }
                        else
                        {
                            using (var db = new MusicDb(connectionString))
                            {
                                var musicFiles = db.MusicFiles.Where(x => x.File.ToLower().StartsWith(artistPath.ToLower())).ToArray();
                                var deletedMusicFiles = musicFiles.Where(x => !File.Exists(x.File));
                                var opusPaths = deletedMusicFiles.Select(x => x.OpusPath).Distinct(StringComparer.CurrentCultureIgnoreCase);
                                foreach (var op in opusPaths)
                                {
                                    var fullPath = Path.Combine(pd.DiskRoot, pd.StylePath, op);
                                    if (!Directory.Exists(fullPath))
                                    {
                                        var pdt = MusicMetaDataMethods.GetPathData(musicOptions, fullPath, true);
                                        log.Information($"{fullPath}, music files on disk deleted");
                                        addIfNotPresent(pdt, true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }
        private async Task ProcessChanges()
        {
            log.Information($"{nameof(ProcessChanges)} started");
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(musicOptions.FolderChangePollingInterval, cancellationToken); //default 3 sec
                //var target = lastChangeTime.Add(musicOptions.FolderChangeAfterChangesInterval); // default 10secs
                //var now = DateTimeOffset.Now;
                //log.Debug($"{changeList.Count()} changes, lastchange = {lastChangeTime.ToDefaultWithTime()}, target {target.ToDefaultWithTime()} waiting for {(target - now).TotalMilliseconds} ms");
                //if (now > target)
                //{

                //}
                if (changeListList.Count() > 0)
                {
                    List<(string Path, bool Deleted)> list = null;
                    lock (changeListList)
                    {
                        list = changeListList.Dequeue();
                    }
                    ProcessChangeList(list);
                    try
                    {
                        while (changeList.Count() > 0)
                        {
                            foreach (var item in changeList.ToArray())
                            {
                                var pd = item.PathData;// MusicMetaDataMethods.GetPathData(musicOptions, item.Path, true);
                                if (pd.IsPortraits)
                                {
                                    await publisher.AddPortraitsTask(pd.MusicStyle);
                                }
                                else
                                {
                                    if (item.Deleted)
                                    {
                                        await publisher.AddTask(pd.MusicStyle, Music.Data.TaskType.DeletedPath, pd.GetFullOpusPath() ?? pd.GetFullArtistPath());
                                    }
                                    else
                                    {
                                        if (pd.GetFullOpusPath() != null)
                                        {
                                            await publisher.AddTask(pd.MusicStyle, Music.Data.TaskType.DiskPath, pd.GetFullOpusPath());
                                        }
                                        else
                                        {
                                            await publisher.AddTask(pd.MusicStyle, Music.Data.TaskType.ArtistFolder, pd.GetFullArtistPath());
                                        }

                                    }
                                }
                                changeList.Remove(item);
                            }
                        }
                    }
                    catch (Exception xe)
                    {
                        log.Error(xe);
                        throw;
                    }
                }
            }
            log.Error($"{nameof(ProcessChanges)} finished");
        }
        private FileSystemMonitor AddMonitor(string folderName)
        {
            var fsm = this.mf.CreateMonitor(folderName, (changes) =>
            {
                var list = changes.Where(x => !x.Path.EndsWith(".txt") && !x.Path.EndsWith(".json"))
                    .Select(x => (Path: x.Path, Deleted: x.Type == WatcherChangeTypes.Deleted))
                    .ToList();
                log.Debug($"adding new list of {changes.Count()} filesystemmonitor events");
                lock (changeListList)
                {
                    changeListList.Enqueue(list);
                }
                //Debugger.Break();
                //OnChangeOccurred(folderName, changes);
            });
            fsm.IncludeSubdirectories = true;
            //fsm.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime;
            return fsm;
        }
        private void CleanUp()
        {
            if (sources != null)
            {
                foreach (var kvp in sources)
                {
                    kvp.Value.Stop();
                    kvp.Value.Dispose();
                }
            }
            return;
        }
    }
}
