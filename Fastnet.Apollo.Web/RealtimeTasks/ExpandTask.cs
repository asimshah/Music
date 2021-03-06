﻿using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public class ExpandTask : TaskBase
    {
        private MusicDb db;

        public ExpandTask(MusicOptions options, long taskId, string connectionString, BlockingCollection<TaskQueueItem> taskQueue) : base(options, taskId, connectionString, taskQueue)
        {
            //this.taskQueue = taskQueue;
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
            this.db = db;
            List<TaskItem> taskList = null;// new List<TaskItem>();
            var ti = await db.TaskItems.FindAsync(taskId);
            switch (ti.Type)
            {
                case TaskType.ArtistFolder:
                    taskList = ExpandArtistFolder(taskData);
                    break;
                case TaskType.ArtistName:
                    taskList = ExpandArtistName(taskData);
                    break;
                case TaskType.MusicStyle:
                    taskList = ExpandMusicStyle(musicStyle);
                    break;
                default:
                    log.Warning($"Task id {taskId} unexpected task type {ti.Type}");
                    break;
            }
            ti.Status = Music.Core.TaskStatus.Finished;
            ti.FinishedAt = DateTimeOffset.Now;
            if (taskList.Count() > 0)
            {
                foreach (var item in taskList)
                {
                    item.Force = ti.Force;
                }
                var generatedRoots = new List<string>();
                foreach (var source in new MusicSources(musicOptions))
                {
                    if (source.IsGenerated)
                    {
                        generatedRoots.Add(source.DiskRoot);
                    }
                }
                var generatedItems = taskList.Where(x => generatedRoots.Any(z => x.TaskString.StartsWith(z, System.Globalization.CompareOptions.IgnoreCase))).ToList();
                var originalItems = taskList.Except(generatedItems).ToList();
                originalItems.Shuffle();
                generatedItems.Shuffle();
                db.TaskItems.AddRange(originalItems);
                await db.SaveChangesAsync();
                db.TaskItems.AddRange(generatedItems);
            }
            await db.SaveChangesAsync();
            if (taskList.Count() > 0)
            {
                foreach (var item in taskList.OrderBy(t => t.Id))
                {
                    QueueTask(item);
                }
                var taskIdList = taskList.Select(t => t.Id).OrderBy(x => x);
                log.Information($"{ti} expanded to {taskIdList.Count()} items from [TI-{taskIdList.First()}] to [TI-{taskIdList.Last()}]");
            }
            else
            {
                log.Information($"{ti} expanded to 0 items");
            }
        }

        private List<TaskItem> ExpandMusicStyle(MusicStyles style)
        {
            var taskList = new List<TaskItem>();
            foreach (var af in style.GetArtistFolders(musicOptions))
            {
                // check here for $portraits
                // check here for singles - are these causing duplicates/
                var items = ExpandArtistFolder(af);
                taskList.AddRange(items);
            }
            var cf = style.GetCollectionsFolder(musicOptions);
            foreach (var folder in cf.GetOpusFolders())
            {
                var l = ExpandOpusFolder(folder.Folderpath, false);
                if (l != null)
                {
                    taskList.Add(l);
                }
            }
            return taskList;
        }
        private TaskItem ExpandOpusFolder(string folder, bool singlesFolder)
        {
            return CreateTask(db, musicStyle, TaskType.DiskPath, folder);
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
        private List<TaskItem> ExpandArtistFolder(string artistFolder)
        {
            var artistName = System.IO.Path.GetFileName(artistFolder);
            //var af = new ArtistFolder(musicOptions, musicStyle, artistName);
            return ExpandArtistName(artistName);
        }
        private List<TaskItem> ExpandArtistFolder(ArtistFolder artistFolder)
        {
            var taskList = new List<TaskItem>();
            foreach (var folder in artistFolder.GetOpusFolders())
            {
                var l = ExpandOpusFolder(folder.Folderpath, folder.ForSinglesOnly);
                if (l != null)
                {
                    taskList.Add(l);
                }
            }
            return taskList;
        }
        private List<TaskItem> ExpandArtistName(string artistName)
        {
            var artistFolder = new ArtistFolder(musicOptions, musicStyle, artistName);
            return ExpandArtistFolder(artistFolder);
        }

    }
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
    }
}

