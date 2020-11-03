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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    //internal class FolderChange
    //{
    //    public PathData PathData { get; set; }
    //    public bool Deleted { get; set; } // if false then path is new, or modified
    //    //public string Path { get; set; }
    //    public override string ToString()
    //    {
    //        return $"{PathData}{(Deleted ? " (deleted)" : "")}";
    //    }
    //}

    public class MusicFolderChangeMonitor : HostedService // RealtimeTask
    {
        private readonly BlockingCollection<List<(string Path, bool Deleted)>> listOfChangeLists = new BlockingCollection<List<(string Path, bool Deleted)>>();
        private CancellationToken cancellationToken;
        private IDictionary<string, FileSystemMonitor> sources;
        private readonly FileSystemMonitorFactory mf;
        private readonly TaskPublisher publisher;
        private readonly string connectionString;
        private readonly IOptionsMonitor<MusicOptions> musicOptionsMonitor;
        public MusicFolderChangeMonitor(IConfiguration cfg, IWebHostEnvironment environment,
            TaskPublisher publisher, IOptionsMonitor<MusicOptions> musicOptionsMonitor,
            FileSystemMonitorFactory mf, ILogger<MusicFolderChangeMonitor> logger) : base(logger)
        {
            connectionString = environment.LocaliseConnectionString(cfg.GetConnectionString("MusicDb"));
            this.publisher = publisher;
            this.musicOptionsMonitor = musicOptionsMonitor;
            this.musicOptionsMonitor.OnChange((mo) =>
                {
                    Thread.Sleep(10000);
                    Restart();
                });
            this.mf = mf;
        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            log.Trace($"{nameof(ExecuteAsync)}");
            this.cancellationToken = cancellationToken;
            await StartAsync();
        }
        private void Restart()
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    log.Information($"{nameof(Restart)}");
                    CleanUp();
                    Initialise();
                    Task.Run(async () =>
                    {
                        while (!cancellationToken.IsCancellationRequested)
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
                await Task.Delay(musicOptionsMonitor.CurrentValue.FolderChangeDiskAccessCheckInterval, cancellationToken);
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
            foreach (var style in musicOptionsMonitor.CurrentValue.Styles)
            {
                var stylePaths = style.Style.GetPaths(musicOptionsMonitor.CurrentValue, false, false);
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
        }
        private IEnumerable<MusicPathAnalysis> ProcessChangeList(List<(string Path, bool Deleted)> list)
        {
            log.Debug($"processing list of {list.Count()} items");
            var pathList = new List<MusicPathAnalysis>();
            foreach (var item in list)
            {
                var mpa = MusicRoot.AnalysePath(musicOptionsMonitor.CurrentValue, item.Path);
                if (item.Deleted)
                {
                    mpa.IsDeletion = true;
                }
                log.Information($"{mpa}");
                if (mpa.IsPortraitsFolder)
                {
                    // portrait folder deletion is ignored - is that right?
                    if (!mpa.IsDeletion)
                    {
                        if (pathList.SingleOrDefault(x => x.MusicRoot.MusicStyle == mpa.MusicRoot.MusicStyle && x.IsPortraitsFolder) == null)
                        {
                            pathList.Add(mpa);
                        }
                    }
                }
                else if(mpa.IsCollection && mpa.ToplevelName == null)
                {
                    // skip changes that are for the collections folder itself
                    continue;
                }
                else
                {
                    pathList.Add(mpa);
                }
            }
            return pathList;
        }
        private async Task ProcessChanges()
        {
            log.Information($"{nameof(ProcessChanges)} started");
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var changeList in listOfChangeLists.GetConsumingEnumerable(cancellationToken))
                {
                    var mpaList = ProcessChangeList(changeList);
                    foreach (var mpa in mpaList)
                    {
                        if (mpa.IsPortraitsFolder)
                        {
                            await publisher.AddPortraitsTask(mpa.MusicRoot.MusicStyle);
                        }
                        else
                        {
                            if (mpa.IsDeletion)
                            {
                                await publisher.AddTask(mpa.MusicRoot.MusicStyle, Music.Data.TaskType.DeletedPath, mpa.GetPath());
                            }
                            else
                            {
                                switch (mpa.MusicRoot.MusicStyle)
                                {
                                    case MusicStyles.HindiFilms:
                                        await publisher.AddTask(mpa.MusicRoot.MusicStyle, TaskType.FilmFolder, mpa.GetPath());
                                        break;

                                    default:
                                        if(mpa.IsCollection && mpa.ToplevelName != null)
                                        {
                                            await publisher.AddTask(mpa.MusicRoot.MusicStyle, TaskType.DiskPath, mpa.GetPath());
                                        }
                                        else if (mpa.SecondlevelName == null)
                                        {
                                            await publisher.AddTask(mpa.MusicRoot.MusicStyle, TaskType.ArtistFolder, mpa.GetPath());
                                        }
                                        else
                                        {
                                            await publisher.AddTask(mpa.MusicRoot.MusicStyle, TaskType.DiskPath, mpa.GetPath());
                                        }
                                        break;
                                }
                            }
                        }
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
                listOfChangeLists.Add(list);
            });
            fsm.IncludeSubdirectories = true;
            fsm.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime;
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