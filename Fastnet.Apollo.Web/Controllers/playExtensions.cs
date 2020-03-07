using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fastnet.Music.Core;
using Fastnet.Music.Data;

namespace Fastnet.Apollo.Web.Controllers
{
    public static class playExtensions
    {
        public static MusicFile GetBestMusicFile(this Track t, DeviceRuntime dr)
        {
            //TODO:: a more complicated algorithm may be required browsers
            // i.e. one that tries to decide if they are flac capable, etc
            if (dr.Type == AudioDeviceType.Browser)
            {
                var mp3versions = t.MusicFiles.Where(x => x.Encoding == EncodingType.mp3);
                if(mp3versions.Count() > 0)
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
        public static void AddPlaylistItem(this Playlist pl, PlaylistItem pli)
        {
            pli.Sequence = pl.Items.Count() + 1;
            pli.Playlist = pl;
            pl.Items.Add(pli);
        }
        public static async Task FillPlaylistItemForRuntime(this MusicDb db, PlaylistItem pli)
        {
            switch (pli.Type)
            {
                case PlaylistItemType.MusicFile:
                    pli.MusicFile = await db.MusicFiles.FindAsync(pli.MusicFileId);
                    break;
                case PlaylistItemType.Track:
                    pli.Track = await db.Tracks.FindAsync(pli.ItemId);
                    break;
                case PlaylistItemType.Work:
                    pli.Work = await db.Works.FindAsync(pli.ItemId);
                    break;
                case PlaylistItemType.Performance:
                    pli.Performance = await db.Performances.FindAsync(pli.ItemId);
                    break;
            }
        }
        public static async Task FillPlaylistForRuntime(this MusicDb db, Playlist pl)
        {
            foreach (var pli in pl.Items)
            {
                await db.FillPlaylistItemForRuntime(pli);
            }
        }
    }
}