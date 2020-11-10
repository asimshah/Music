using Fastnet.Core;
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
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public class ExpandTask : TaskBase
    {
        //private MusicDb db;
        private readonly TaskRunner taskRunner;
        private readonly IOptionsMonitor<MusicOptions> optionsMonitor;
        //public ExpandTask(MusicOptions options, long taskId, string connectionString, BlockingCollection<TaskQueueItem> taskQueue) : base(options, taskId, connectionString, taskQueue)
        //{
        //    //this.taskQueue = taskQueue;
        //}
        public ExpandTask(TaskRunner taskRunner, IOptionsMonitor<MusicOptions> optionsMonitor, ILogger<ExpandTask> log, IConfiguration cfg, IWebHostEnvironment environment) : base(log, cfg, environment)
        {
            this.taskRunner = taskRunner;
            this.optionsMonitor = optionsMonitor;
        }
        protected override async Task RunTask()
        {
            await this.ExecuteTaskItemAsync(async (db) =>
            {
                await ExpandAsync(db);
            });
        }
        private async Task ExpandAsync(MusicDb db)
        {
            IEnumerable<TaskItem> taskList = null;// new List<TaskItem>();
            var ti = await db.TaskItems.FindAsync(taskId);
            switch (ti.Type)
            {
                case TaskType.ArtistFolder:
                    var mpa = MusicRoot.AnalysePath(optionsMonitor.CurrentValue, ti.TaskString);
                    if (mpa.MusicRoot.MusicStyle == ti.MusicStyle)
                    {
                        var af = mpa.GetFolder() as ArtistFolder;
                        if (af == null)
                        {
                            log.Error($"{ti} {ti.TaskString} is not an artist folder");
                        }
                        else
                        {
                            taskList = ExpandArtistFolder(db, af);
                        }
                    }
                    else
                    {
                        log.Error($"{ti} style {ti.MusicStyle} does not match path ${ti.TaskString}");
                    }
                    break;
                case TaskType.MusicStyle:
                    taskList = ExpandMusicStyle(db, ti.MusicStyle);
                    break;

                default:
                    log.Warning($"{ti} unexpected task type {ti.Type}");
                    break;
            }
            //var mpa = MusicRoot.AnalysePath(optionsMonitor.CurrentValue, ti.TaskString);
            //if (mpa.MusicRoot.MusicStyle == ti.MusicStyle)
            //{
            //    switch (ti.Type)
            //    {
            //        case TaskType.ArtistFolder:
            //            var af = mpa.GetFolder() as ArtistFolder;
            //            if (af == null)
            //            {
            //                log.Error($"{ti} {ti.TaskString} is not an artist folder");
            //            }
            //            else
            //            {
            //                taskList = ExpandArtistFolder(db, af);
            //            }
            //            break;
            //        case TaskType.MusicStyle:
            //            taskList = ExpandMusicStyle(db, ti.MusicStyle);
            //            break;

            //        default:
            //            log.Warning($"{ti} unexpected task type {ti.Type}");
            //            break;
            //    }
            //}
            //else
            //{
            //    log.Error($"{ti} style {ti.MusicStyle} does not match path ${ti.TaskString}");
            //}

            ti.Status = Music.Core.TaskStatus.Finished;
            ti.FinishedAt = DateTimeOffset.Now;
            if (taskList != null)
            {
                if (taskList.Count() > 0)
                {
                    foreach (var item in taskList.Where(x => !(x is null)))
                    {
                        item.Force = ti.Force;
                    }
                    db.TaskItems.AddRange(taskList);
                    await db.SaveChangesAsync();
                }
                //await db.SaveChangesAsync();
                if (taskList.Count() > 0)
                {
                    log.Information($"{ti.ToDescription()} expanded to {taskList.Count()} items:");
                    foreach (var item in taskList.OrderBy(t => t.Id))
                    {
                        taskRunner.QueueTask(item);
                        log.Information($"==> {item.ToDescription()}");
                        // QueueTask(item);
                    }
                    //var taskIdList = taskList.Select(t => t.Id).OrderBy(x => x);

                    //log.Information($"{ti} expanded to {taskIdList.Count()} items from [TI-{taskIdList.First()}] to [TI-{taskIdList.Last()}]");
                }
                else
                {
                    log.Information($"{ti.ToDescription()} expanded to 0 items");
                }
            }
        }
        private IEnumerable<TaskItem> ExpandMusicStyle(MusicDb db, MusicStyles musicStyle)
        {
            var taskList = new List<TaskItem>();
            foreach (var tf in musicStyle.GetTopFoldersExcludingCollections(optionsMonitor.CurrentValue))
            {
                var items = tf switch
                {
                    ArtistFolder af => ExpandArtistFolder(db, af),
                    HindiFilmFolder hf => new TaskItem[] { CreateTask(db, musicStyle, TaskType.FilmFolder, hf.Fullpath) },
                    _ => throw new NotSupportedException()
                };
                taskList.AddRange(items);
            }
            foreach(var cf in musicStyle.GetCollectionAlbumFolders(optionsMonitor.CurrentValue))
            {
                taskList.Add( CreateTask(db, musicStyle, TaskType.DiskPath, cf.Fullpath));
            }

            var works = db.Works.Where(x => x.StyleId == musicStyle);
            foreach (var work in works)
            {
                taskList.AddRange(DeleteWorkIfRequired(db, work));
            }
            return taskList;
        }
        private List<TaskItem> DeleteWorkIfRequired(MusicDb db, Work work)
        {
            var taskList = new List<TaskItem>();
            var workFiles = work.Tracks.SelectMany(x => x.MusicFiles)
                .Where(mf => mf.IsGenerated == false)
                .ToArray();
            if (workFiles.All(mf => System.IO.File.Exists(mf.File) == false))
            {
                var roots = workFiles.Select(x => x.GetRootPath()).Distinct(StringComparer.CurrentCultureIgnoreCase);
                foreach (var root in roots)
                {
                    var task = CreateTask(db, work.StyleId, TaskType.DeletedPath, root);
                    taskList.Add(task);
                }
            }
            return taskList;
        }
        private TaskItem CreateTask(MusicDb db, MusicStyles style, TaskType type, string taskString)
        {
            var existing = db.TaskItems
                .Where(t => t.Type == type && t.MusicStyle == style
                    && t.TaskString.ToLower() == taskString.ToLower()
                    && (t.Status == Music.Core.TaskStatus.Pending || t.Status == Music.Core.TaskStatus.InProgress)
                    );
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
                    TaskString = taskString
                };
                return ti;
            }
            else
            {
                log.Information($"Task type {type} for target {taskString} skipped as already present");
            }
            return null;
        }
        private IEnumerable<TaskItem> ExpandArtistFolder(MusicDb db, ArtistFolder artistFolder)
        {
            var taskList = new List<TaskItem>();
            foreach (var f in artistFolder.GetAlbumFolders())
            {
                var ti = CreateTask(db, f.MusicStyle, TaskType.DiskPath, f.Fullpath);
                if (ti != null)
                {
                    taskList.Add(ti);
                }
            }
            return taskList;
        }
    }
    //public class ExpandTaskOld : TaskBaseOld
    //{
    //    private MusicDb db;
    //    public ExpandTaskOld(MusicOptions options, long taskId, string connectionString, BlockingCollection<TaskQueueItem> taskQueue) : base(options, taskId, connectionString, taskQueue)
    //    {
    //        //this.taskQueue = taskQueue;
    //    }
    //    protected override async Task RunTask()
    //    {
    //        await this.ExecuteTaskItemAsync(async (db) =>
    //        {
    //            await ExpandAsync(db);
    //        });
    //    }

    //    private async Task ExpandAsync(MusicDb db)
    //    {
    //        this.db = db;
    //        List<TaskItem> taskList = null;// new List<TaskItem>();
    //        var ti = await db.TaskItems.FindAsync(taskId);
    //        switch (ti.Type)
    //        {
    //            case TaskType.ArtistFolder:
    //                taskList = ExpandArtistFolder(taskData);
    //                break;

    //            case TaskType.ArtistName:
    //                taskList = ExpandArtistName(taskData);
    //                break;

    //            case TaskType.MusicStyle:
    //                if (musicStyle == MusicStyles.HindiFilms)
    //                {
    //                    taskList = ExpandHindiFilmMusicStyle();
    //                }
    //                else
    //                {
    //                    taskList = ExpandOtherMusicStyle(musicStyle);
    //                }
    //                break;

    //            default:
    //                log.Warning($"Task id {taskId} unexpected task type {ti.Type}");
    //                break;
    //        }
    //        ti.Status = Music.Core.TaskStatus.Finished;
    //        ti.FinishedAt = DateTimeOffset.Now;
    //        if (taskList.Count() > 0)
    //        {
    //            foreach (var item in taskList)
    //            {
    //                item.Force = ti.Force;
    //            }
    //            var generatedRoots = new List<string>();
    //            foreach (var source in new MusicSources(musicOptions))
    //            {
    //                if (source.IsGenerated)
    //                {
    //                    generatedRoots.Add(source.DiskRoot);
    //                }
    //            }
    //            var generatedItems = taskList.Where(x => generatedRoots.Any(z => x.TaskString.StartsWith(z, System.Globalization.CompareOptions.IgnoreCase))).ToList();
    //            var originalItems = taskList.Except(generatedItems).ToList();
    //            originalItems.Shuffle();
    //            generatedItems.Shuffle();
    //            db.TaskItems.AddRange(originalItems);
    //            await db.SaveChangesAsync();
    //            db.TaskItems.AddRange(generatedItems);
    //        }
    //        await db.SaveChangesAsync();
    //        if (taskList.Count() > 0)
    //        {
    //            foreach (var item in taskList.OrderBy(t => t.Id))
    //            {
    //                QueueTask(item);
    //            }
    //            var taskIdList = taskList.Select(t => t.Id).OrderBy(x => x);
    //            log.Information($"{ti} expanded to {taskIdList.Count()} items from [TI-{taskIdList.First()}] to [TI-{taskIdList.Last()}]");
    //        }
    //        else
    //        {
    //            log.Information($"{ti} expanded to 0 items");
    //        }
    //    }
    //    private List<TaskItem> ExpandHindiFilmMusicStyle()
    //    {
    //        var taskList = new List<TaskItem>();
    //        var filmFolders = MusicStyles.HindiFilms.GetFilmFolders(musicOptions);
    //        return taskList;
    //    }
    //    private List<TaskItem> ExpandOtherMusicStyle(MusicStyles style)
    //    {
    //        var taskList = new List<TaskItem>();
    //        //if (style != MusicStyles.HindiFilms)
    //        //{
    //        //    foreach (var af in style.GetArtistFolders(musicOptions))
    //        //    {
    //        //        // check here for $portraits
    //        //        // check here for singles - are these causing duplicates/
    //        //        var items = ExpandArtistFolder(af);
    //        //        taskList.AddRange(items);
    //        //    }
    //        //}
    //        //else
    //        //{
    //        //    var filmFoldes = style.GetFilmFolders(musicOptions);
    //        //}
    //        foreach (var af in style.GetArtistFolders(musicOptions))
    //        {
    //            // check here for $portraits
    //            // check here for singles - are these causing duplicates/
    //            var items = ExpandArtistFolder(af);
    //            taskList.AddRange(items);
    //        }
    //        var cf = style.GetCollectionsFolder(musicOptions);
    //        foreach (var folder in cf.GetOpusFolders())
    //        {
    //            var l = ExpandOpusFolder(folder.Folderpath, false);
    //            if (l != null)
    //            {
    //                taskList.Add(l);
    //            }
    //        }
    //        var works = db.Works.Where(x => x.StyleId == style);
    //        foreach (var work in works)
    //        {
    //            taskList.AddRange(DeleteWorkIfRequired(work));
    //        }
    //        return taskList;
    //    }
    //    private List<TaskItem> DeleteWorkIfRequired(Work work)
    //    {
    //        var taskList = new List<TaskItem>();
    //        var workFiles = work.Tracks.SelectMany(x => x.MusicFiles)
    //            .Where(mf => mf.IsGenerated == false)
    //            .ToArray();
    //        if (workFiles.All(mf => System.IO.File.Exists(mf.File) == false))
    //        {
    //            var roots = workFiles.Select(x => x.GetRootPath()).Distinct(StringComparer.CurrentCultureIgnoreCase);
    //            foreach (var root in roots)
    //            {
    //                var task = CreateTask(db, work.StyleId, TaskType.DeletedPath, root);
    //                taskList.Add(task);
    //            }
    //        }
    //        return taskList;
    //    }
    //    private TaskItem ExpandOpusFolder(string folder, bool singlesFolder)
    //    {
    //        return CreateTask(db, musicStyle, TaskType.DiskPath, folder);
    //    }
    //    private TaskItem CreateTask(MusicDb db, MusicStyles style, TaskType type, string taskString)
    //    {
    //        var existing = db.TaskItems
    //            .Where(t => t.Type == type && t.MusicStyle == style
    //                && t.TaskString.ToLower() == taskString.ToLower()
    //                && (t.Status == Music.Core.TaskStatus.Pending || t.Status == Music.Core.TaskStatus.InProgress)
    //                );
    //        if (existing.Count() == 0)
    //        {
    //            var now = DateTimeOffset.Now;
    //            var ti = new TaskItem
    //            {
    //                Type = type,
    //                CreatedAt = now,
    //                ScheduledAt = now,
    //                Status = Music.Core.TaskStatus.Pending,
    //                MusicStyle = style,
    //                TaskString = taskString
    //            };
    //            return ti;
    //        }
    //        else
    //        {
    //            log.Information($"Task type {type} for target {taskString} skipped as already present");
    //        }
    //        return null;
    //    }
    //    private List<TaskItem> ExpandArtistFolder(string artistFolder)
    //    {
    //        var artistName = System.IO.Path.GetFileName(artistFolder);
    //        //var af = new ArtistFolder(musicOptions, musicStyle, artistName);
    //        return ExpandArtistName(artistName);
    //    }
    //    private List<TaskItem> ExpandArtistFolder(ArtistFolder artistFolder)
    //    {
    //        var taskList = new List<TaskItem>();
    //        foreach (var folder in artistFolder.GetOpusFolders())
    //        {
    //            var l = ExpandOpusFolder(folder.Folderpath, folder.ForSinglesOnly);
    //            if (l != null)
    //            {
    //                taskList.Add(l);
    //            }
    //        }
    //        return taskList;
    //    }
    //    private List<TaskItem> ExpandArtistName(string artistName)
    //    {
    //        var artistFolder = new ArtistFolder(musicOptions, musicStyle, artistName);
    //        return ExpandArtistFolder(artistFolder);
    //    }
    //}

    //// move this to metatools eventually
    //public class FilmFolder //: OpusFolder
    //{
    //}

    public static class _l
    {
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        // move this to metatools eventually

        //public static PathData[] GetPathDataList(this MusicStyles musicStyle, MusicOptions musicOptions)
        //{
        //    var list = new List<PathData>();
        //    foreach (var path in musicStyle.GetPaths(musicOptions, false, false))
        //    {
        //        list.Add(MusicMetaDataMethods.GetPathData(musicOptions, path));
        //    }

        //    return list.ToArray();
        //}
    }
}