using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
// temp:: using Fastnet.Music.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public static partial class Extensions
    {
        public static void Touch(this string fileName)
        {
            var fi = new System.IO.FileInfo(fileName);
            fi.Touch();
        }
        public static void Touch(this System.IO.FileInfo fi)
        {
            fi.LastWriteTimeUtc = DateTime.UtcNow;
        }
        public static IServiceCollection AddMusicLibraryTasks(this IServiceCollection services, IConfiguration configuration)
        {
            var so = new SchedulerOptions();
            configuration.GetSection("SchedulerOptions").Bind(so);
            services.AddScheduler(configuration);

            services.AddSingleton<FileSystemMonitorFactory>();
            services.AddSingleton<Messenger>();
            services.AddService<MusicFolderChangeMonitor>();
            services.AddService<TaskRunner>();
            services.AddService<TaskPublisher>();
            services.AddService<PlayManager>();
            //services.AddSingleton<SingletonLibraryService>();
            services.AddScoped<LibraryService>();
            services.AddService<Resampler>();
            if (!so.SuspendScheduling)
            {
                foreach (var s in so.Schedules)
                {
                    if (s.Enabled)
                    {
                        switch (s.Name)
                        {
                            case "MusicScanner":
                                // temp:: services.AddSingleton<ScheduledTask, MusicScanner>();
                                break;
                        }
                    }
                }
            }
            return services;
        }
    }
    public static partial class Extensions
    {
        public static (string artistNames, string workName, string albumName) GetNames(this Performance p)
        {
            switch (p.StyleId)
            {
                case MusicStyles.WesternClassical:
                    return (p.Composition.Artist.Name, p.Composition.Name, p.Movements.First().Work.Name);
                case MusicStyles.IndianClassical:
                    //var raga = p.RagaPerformances.Select(x => x.Raga).Distinct().Single();
                    var artists = p.RagaPerformances.Select(x => x.Artist);
                    var work = p.Movements.Select(x => x.Work).Distinct().Single();
                    return (artists.GetArtistNames(), p.GetRaga().DisplayName, work.Name);
            }
            throw new Exception($"style {p.StyleId} does not use performances");
        }
        //public static string GetArtistNames(this IEnumerable<Artist> artists)
        //{
        //    var set = new ArtistSet(artists);
        //    return set.GetNames();
        //    //if(work.Artists.Count() > 2)
        //    //{
        //    //    var x = string.Join(", ", work.Artists.Take(work.Artists.Count() - 1).Select(x => x.Name));
        //    //    return string.Join(" & ", x, work.Artists.Last().Name);
        //    //}
        //    //else
        //    //{
        //    //    return string.Join(" & ", work.Artists.Select(x => x.Name));
        //    //}
        //}
        public static MusicFile GetBestMusicFile(this Track t, DeviceRuntime dr)
        {
            //TODO:: a more complicated algorithm may be required browsers
            // i.e. one that tries to decide if they are flac capable, etc
            if (dr.Type == AudioDeviceType.Browser)
            {
                var mp3versions = t.MusicFiles.Where(x => x.Encoding == EncodingType.mp3);
                if (mp3versions.Count() > 0)
                {
                    return mp3versions.OrderByDescending(x => x.AverageBitRate).First();
                }
            }
            if (dr.MaxSampleRate == 0)
            {
                return t.MusicFiles.First();
            }
            else
            {
                var candidates = t.MusicFiles.Where(x => x.SampleRate <= dr.MaxSampleRate);
                if (candidates.Count() > 0)
                {
                    var bestSampleRate = candidates.Max(x => x.SampleRate);
                    var availableFiles = t.MusicFiles.Where(x => x.SampleRate == bestSampleRate).OrderBy(x => (x.AverageBitRate > 0 ? x.AverageBitRate : x.MaximumBitRate));
                    return availableFiles.First();
                }
            }
            return null;
        }
        //public static void AddPlaylistItem(this Playlist pl, PlaylistItem pli)
        //{
        //    pli.Sequence = pl.Items.Count() + 1;
        //    pli.Playlist = pl;
        //    pl.Items.Add(pli);
        //}
        //public static async Task FillPlaylistItemForRuntime(this MusicDb db, PlaylistItem pli)
        //{
        //    switch (pli.Type)
        //    {
        //        case PlaylistItemType.MusicFile:
        //            pli.MusicFile = await db.MusicFiles.FindAsync(pli.MusicFileId);
        //            break;
        //        case PlaylistItemType.Track:
        //            pli.Track = await db.Tracks.FindAsync(pli.ItemId);
        //            break;
        //        case PlaylistItemType.Work:
        //            pli.Work = await db.Works.FindAsync(pli.ItemId);
        //            break;
        //        case PlaylistItemType.Performance:
        //            pli.Performance = await db.Performances.FindAsync(pli.ItemId);
        //            break;
        //    }
        //}
        //public static async Task FillPlaylistForRuntime(this MusicDb db, Playlist pl)
        //{
        //    foreach (var pli in pl.Items)
        //    {
        //        await db.FillPlaylistItemForRuntime(pli);
        //    }
        //}
    }
}

