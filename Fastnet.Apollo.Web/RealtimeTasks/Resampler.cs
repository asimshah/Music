﻿using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.MediaTools;
using Fastnet.Music.Resampler;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public class Resampler : HostedService
    {
        private CancellationToken cancellationToken;
        private readonly MusicOptions musicOptions;
        private readonly string connectionString;
        public Resampler(ILogger<Resampler> logger, IWebHostEnvironment environment, IConfiguration cfg, IOptions<MusicOptions> options) : base(logger)
        {
            this.musicOptions = options.Value;
            connectionString = environment.LocaliseConnectionString(cfg.GetConnectionString("MusicDb"));
        }
        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            await Start();
        }
        private async Task Start()
        {
            log.Information($"started");
            while(!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(2000);
                try
                {
                    TaskItem taskItem = null;
                    using (var db = new MusicDb(connectionString))
                    {
                        taskItem = db.TaskItems
                            .Where(x => x.Type == TaskType.ResampleWork && x.Status == Music.Core.TaskStatus.Pending)
                            .OrderBy(x => x.ScheduledAt)
                            .FirstOrDefault();
                        if (taskItem != null)
                        {
                            taskItem.Status = Music.Core.TaskStatus.InProgress;
                            await db.SaveChangesAsync();
                        }
                    }
                    if (taskItem != null)
                    {
                        await ResampleAsync(taskItem.Id);
                    }
                }
                catch (Exception xe)
                {
                    log.Error(xe);
                }            
            }
            log.Error($"finished!!!!!!!!!!!!!!!!!!!!!");
        }
        private async Task ResampleAsync(long taskId)
        {
            using (var db = new MusicDb(connectionString))
            {
                TaskItem taskItem = null;
                db.ChangeTracker.AutoDetectChangesEnabled = false;
                taskItem = await db.TaskItems.FindAsync(taskId);
                var sources = new MusicSources(musicOptions, true);
                var vbrDestination = sources.FirstOrDefault(x => x.IsGenerated);
                if (vbrDestination != null)
                {
                    var work = await db.Works.SingleOrDefaultAsync(x => x.UID.ToString() == taskItem.TaskString);
                    if (work != null)
                    {
                        //log.Information($"Resampling {work.Artist.Name}, {work.Name}");
                        int resampledCount = 0;
                        foreach (var track in work.Tracks)
                        {
                            var mf = track.MusicFiles.First(f => f.Encoding == EncodingType.flac);
                            var destinationFile = mf.File.Replace(mf.DiskRoot, vbrDestination.DiskRoot, StringComparison.CurrentCultureIgnoreCase);
                            destinationFile = destinationFile.Replace(".flac", ".mp3", StringComparison.CurrentCultureIgnoreCase);
                            var srcFi = new FileInfo(mf.File);
                            var vbrFi = new FileInfo(destinationFile);
                            if (!vbrFi.Exists || srcFi.LastWriteTimeUtc > vbrFi.LastWriteTimeUtc)
                            {
                                await Resample(taskItem, srcFi.FullName, vbrFi.FullName);
                                vbrFi.Refresh();
                                resampledCount++;
                            }
                            else
                            {
                                log.Debug($"{taskItem} {vbrFi.Name} is up to date");
                            }
                            var existingVbr = track.MusicFiles.FirstOrDefault(f => f.IsGenerated);
                            if (existingVbr != null)
                            {
                                track.MusicFiles.Remove(existingVbr);
                                db.MusicFiles.Remove(existingVbr);
                            }
                            AddToTrack(vbrDestination, track, mf, vbrFi);
                            await db.SaveChangesAsync();
                        }
                        log.Information($"{taskItem} resampling completed {work.Artist.Name}, {work.Name}, {resampledCount}/{work.Tracks.Count()} files");
                    }
                    else
                    {
                        log.Warning($"{taskItem} work with uid {taskItem.TaskString} not found");
                        taskItem.FinishedAt = DateTimeOffset.Now;
                        taskItem.Status = Music.Core.TaskStatus.Failed;
                        await db.SaveChangesAsync();
                        return;
                    }
                }
                else
                {
                    log.Warning($"{taskItem} Resampling failed as no source for generated files is available");
                }
                taskItem.FinishedAt = DateTimeOffset.Now;
                taskItem.Status = Music.Core.TaskStatus.Finished;
                await db.SaveChangesAsync();
            }
        }
        private void AddToTrack(MusicSource destination, Track track, MusicFile mf, FileInfo vbrFi)
        {
            //var vbrFi = new FileInfo(vbrFile);
            var mp3tools = new Mp3Tools(vbrFi.FullName);
            var musicFile = new MusicFile
            {
                Style = mf.Style,
                DiskRoot = destination.DiskRoot,
                StylePath = mf.StylePath,
                OpusPath = mf.OpusPath,
                File = vbrFi.FullName,
                FileLength = vbrFi.Length,
                FileLastWriteTimeUtc = vbrFi.LastWriteTimeUtc,
                Uid = Guid.NewGuid().ToString(),
                Musician = mf.Musician,
                MusicianType = mf.MusicianType,
                OpusType = mf.OpusType,
                IsMultiPart = mf.IsMultiPart,
                OpusName = mf.OpusName,
                PartName = mf.PartName,
                PartNumber = mf.PartNumber,
                Encoding = EncodingType.mp3,
                Mode = mf.Mode,
                Duration = mp3tools.Duration.TotalMilliseconds,
                BitsPerSample = mp3tools.BitsPerSample,
                SampleRate = mp3tools.SampleRate,
                MinimumBitRate = mp3tools.MinimumBitRate,
                MaximumBitRate = mp3tools.MaximumBitRate,
                AverageBitRate = mp3tools.AverageBitRate,
                IsGenerated = true,
                LastCataloguedAt = DateTimeOffset.Now,
                ParsingStage = MusicFileParsingStage.Catalogued,
                Mood = mf.Mood,
                Track = track,
            };
            track.MusicFiles.Add(musicFile);
        }
        private async Task<string> Resample(TaskItem taskItem, string sourceFile, string destinationFile)
        {
            if (File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
                log.Debug($"{taskItem} {destinationFile} deleted");
            }
            var parentDirectory = Path.GetDirectoryName(destinationFile);
            if (!Directory.Exists(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
                log.Information($"{taskItem} {parentDirectory} created");
            }
            var resampler = new FlacResampler();
            await resampler.Resample(sourceFile, destinationFile);
            log.Debug($"{taskItem} {destinationFile} created");
            return destinationFile;
        }
    }

}
