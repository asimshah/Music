using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Core;
using System.IO;
using Fastnet.Music.Resampler;
using Fastnet.Music.MediaTools;
using Microsoft.EntityFrameworkCore;

namespace Fastnet.Apollo.Web
{
    //public class ResampleTask : TaskBase
    //{
    //    //private TaskItem taskItem;
    //    //public ResampleTask(MusicOptions options, long taskId, string connectionString) : base(options, taskId, connectionString, null)
    //    //{
    //    //}
    //    //protected async override Task RunTask()
    //    //{
    //    //    await this.ExecuteTaskItemAsync(async (db) =>
    //    //    {
    //    //        try
    //    //        {
    //    //            //var workId = long.Parse(taskData);
    //    //            await ResampleAsync(db);
    //    //        }
    //    //        catch (Exception xe)
    //    //        {
    //    //            log.Error(xe);
    //    //            throw;
    //    //        }
    //    //    });
    //    //}
    //    //private async Task ResampleAsync(MusicDb db)
    //    //{
    //    //    db.ChangeTracker.AutoDetectChangesEnabled = false;
    //    //    taskItem = await db.TaskItems.FindAsync(taskId);
    //    //    var sources = new MusicSources(musicOptions, true);
    //    //    var vbrDestination = sources.FirstOrDefault(x => x.IsGenerated);
    //    //    if (vbrDestination != null)
    //    //    {
    //    //        var work = await db.Works.SingleAsync(x => x.UID.ToString() == taskItem.TaskString);
    //    //        foreach (var track in work.Tracks)
    //    //        {
    //    //            var mf = track.MusicFiles.First(f => f.Encoding == EncodingType.flac);
    //    //            var destinationFile = mf.File.Replace(mf.DiskRoot, vbrDestination.DiskRoot, StringComparison.CurrentCultureIgnoreCase);
    //    //            destinationFile = destinationFile.Replace(".flac", ".mp3", StringComparison.CurrentCultureIgnoreCase);
    //    //            var srcFi = new FileInfo(mf.File);
    //    //            var vbrFi = new FileInfo(destinationFile);
    //    //            if(!vbrFi.Exists || srcFi.LastWriteTimeUtc > vbrFi.LastWriteTimeUtc)
    //    //            {
    //    //                var existingVbr = track.MusicFiles.FirstOrDefault(f => f.IsGenerated);
    //    //                if (existingVbr != null)
    //    //                {
    //    //                    track.MusicFiles.Remove(existingVbr);
    //    //                    db.MusicFiles.Remove(existingVbr);
    //    //                }
    //    //                await Resample(srcFi.FullName, vbrFi.FullName);
    //    //                vbrFi.Refresh();
    //    //                AddToTrack(vbrDestination, track, mf, vbrFi);
    //    //            }
    //    //        }
    //    //    }
    //    //    else
    //    //    {
    //    //        log.Warning($"{taskItem} Resampling failed as no source for generated files is available");
    //    //    }
    //    //    taskItem.FinishedAt = DateTimeOffset.Now;
    //    //    taskItem.Status = Music.Core.TaskStatus.Finished;
    //    //    await db.SaveChangesAsync();
    //    //}

    //    //private void AddToTrack(MusicSource destination, Track track, MusicFile mf, FileInfo vbrFi)
    //    //{
    //    //    //var vbrFi = new FileInfo(vbrFile);
    //    //    var mp3tools = new Mp3Tools(vbrFi.FullName);
    //    //    var musicFile = new MusicFile
    //    //    {
    //    //        Style = mf.Style,
    //    //        DiskRoot = destination.DiskRoot,
    //    //        StylePath = mf.StylePath,
    //    //        OpusPath = mf.OpusPath,
    //    //        File = vbrFi.FullName,
    //    //        FileLength = vbrFi.Length,
    //    //        FileLastWriteTimeUtc = vbrFi.LastWriteTimeUtc,
    //    //        Uid = Guid.NewGuid().ToString(),
    //    //        Musician = mf.Musician,
    //    //        MusicianType = mf.MusicianType,
    //    //        OpusType = mf.OpusType,
    //    //        IsMultiPart = mf.IsMultiPart,
    //    //        OpusName = mf.OpusName,
    //    //        PartName = mf.PartName,
    //    //        PartNumber = mf.PartNumber,
    //    //        Encoding = EncodingType.mp3,
    //    //        Mode = mf.Mode,
    //    //        Duration = mp3tools.Duration.TotalMilliseconds,
    //    //        BitsPerSample = mp3tools.BitsPerSample,
    //    //        SampleRate = mp3tools.SampleRate,
    //    //        MinimumBitRate = mp3tools.MinimumBitRate,
    //    //        MaximumBitRate = mp3tools.MaximumBitRate,
    //    //        AverageBitRate = mp3tools.AverageBitRate,
    //    //        IsGenerated = true,
    //    //        LastCataloguedAt = DateTimeOffset.Now,
    //    //        ParsingStage = MusicFileParsingStage.Catalogued,
    //    //        Mood = mf.Mood,
    //    //        Track = track,
    //    //    };
    //    //    track.MusicFiles.Add(musicFile);
    //    //}
    //    //private async Task<string> Resample(string sourceFile, string destinationFile)
    //    //{
    //    //    //var sourceFile = musicFile.File;
    //    //    if (File.Exists(destinationFile))
    //    //    {
    //    //        File.Delete(destinationFile);
    //    //        log.Information($"{taskItem} {destinationFile} deleted");
    //    //    }
    //    //    var parentDirectory = Path.GetDirectoryName(destinationFile);
    //    //    if (!Directory.Exists(parentDirectory))
    //    //    {
    //    //        Directory.CreateDirectory(parentDirectory);
    //    //        log.Information($"{taskItem} {parentDirectory} created");
    //    //    }
    //    //    var resampler = new FlacResampler();
    //    //    await resampler.Resample(sourceFile, destinationFile);
    //    //    log.Information($"{taskItem} {destinationFile} created");
    //    //    return destinationFile;
    //    //}
    //}
}
