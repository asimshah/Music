using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fastnet.Music.Data
{
    public static class Extensions
    {
        private static readonly ILogger log = ApplicationLoggerFactory.CreateLogger("Fastnet.Music.Data.Extensions");
        public static MusicFile GetBestQuality(this Track t)
        {
            return t.MusicFiles.Where(x => x.IsGenerated == false).OrderByDescending(x => x.Rank()).First();
        }
        public static int Rank(this MusicFile mf)
        {
            int rank = -1;
            if (!mf.IsFaulty)
            {
                switch (mf.Encoding)
                {
                    case EncodingType.flac:
                        // max sample rate = 192000                       
                        rank = mf.GetBitRate().rate ?? 1024;// 1_000_000 + mf.SampleRate ?? 0;
                        break;
                    //case EncodingType.m4a:
                    //    rank = 100000;
                    //    break;
                    case EncodingType.mp3:
                        rank = mf.GetBitRate().rate ?? 10; // from ? to 320, say 1000 mx
                        break;
                    default:
                        rank = 3;
                        break;
                }
            }
            return rank;
        }
        public static MetadataQuality ToMetadataQuality(this LibraryParsingStage stage)
        {
            var quality = MetadataQuality.Low;
            switch (stage)
            {
                case LibraryParsingStage.Unknown:
                case LibraryParsingStage.Initial:
                    break;
                case LibraryParsingStage.IdTagsEvaluated:
                    quality = MetadataQuality.Medium;
                    break;
                //case LibraryParsingStage.MusicbrainzQueryCompleted:
                case LibraryParsingStage.UserConfirmed:
                    quality = MetadataQuality.High;
                    break;
            }
            return quality;
        }
        public static (int? rate, string type) GetBitRate(this MusicFile mf)
        {
            switch (mf.Encoding)
            {
                case EncodingType.mp3:
                    if (mf.MinimumBitRate.HasValue && mf.MaximumBitRate.HasValue)
                    {
                        if (mf.MinimumBitRate.Value == mf.MaximumBitRate.Value)
                        {
                            return (mf.MinimumBitRate / 1000, "CBR");
                        }
                        else
                        {
                            return ((int)Math.Round(mf.AverageBitRate.Value / 1000, 0), "VBR");
                        }
                    }
                    else
                    {
                        return (null, null);
                    }
                case EncodingType.flac:
                    if (mf.BitsPerSample.HasValue && mf.SampleRate.HasValue)
                    {
                        return ((int)Math.Round((double)(mf.BitsPerSample.Value * mf.SampleRate.Value * 2) / 1000, 0), "FLAC");
                    }

                    return (null, null);
                default:
                    return (null, null);
            }
        }
        public static string GetAudioProperties(this MusicFile mf)
        {
            var sb = new StringBuilder();
            switch (mf.Encoding)
            {
                case EncodingType.mp3:
                    sb.Append(mf.Encoding.ToString());
                    var br = mf.GetBitRate();
                    if (br.rate.HasValue)
                    {
                        sb.Append($", {br.rate.Value}kbs");
                    }
                    if (br.type == "VBR")
                    {
                        sb.Append(", VBR");
                    }
                    if (mf.BitsPerSample.HasValue)
                    {
                        sb.Append($", { mf.BitsPerSample }bit");
                    }
                    if (mf.SampleRate.HasValue)
                    {
                        var fmt = (mf.SampleRate.Value % 1000.0) == 0.0 ? "#0" : "#0.0";
                        sb.Append($", {(mf.SampleRate.Value / 1000.0).ToString(fmt)}kHz");
                    }
                    if (mf.Mode == ChannelMode.Mono)
                    {
                        sb.Append(" mono");
                    }
                    break;
                case EncodingType.flac:
                    sb.Append("flac");
                    if (mf.BitsPerSample.HasValue)
                    {
                        sb.Append($", {mf.BitsPerSample}bit");
                    }
                    if (mf.SampleRate.HasValue)
                    {
                        var fmt = (mf.SampleRate.Value % 1000.0) == 0.0 ? "#0" : "#0.0";
                        sb.Append($", {((mf.SampleRate.Value / 1000.0)).ToString(fmt)}kHz");
                    }
                    if (mf.Mode == ChannelMode.Mono)
                    {
                        sb.Append(" mono");
                    }
                    break;
                default:
                    sb.Append(mf.Encoding.ToString());
                    break;
            }
            return sb.ToString();
        }
        [Obsolete]
        public static T GetValue<T>(this IdTag tag)
        {
            //tmd.TrackNumber = tnTag != null ? Int32.Parse(tnTag.Value) : 0;
            return tag == null ? default(T) : (T)Convert.ChangeType(tag.Value, typeof(T));
        }
        public static string GetRootPath(this MusicFile mf)
        {
            return mf.OpusType == OpusType.Collection ?
                Path.Combine(mf.DiskRoot, mf.StylePath, "Collections", mf.OpusPath)
                : Path.Combine(mf.DiskRoot, mf.StylePath, mf.OpusPath);
        }
        public static T GetTagValue<T>(this MusicFile mf, string tagName)
        {
            var tag = mf.GetTag(tagName);
            return tag == null ? default(T) : (T)Convert.ChangeType(tag.Value, typeof(T));
        }
        public static int? GetTagIntValue(this MusicFile mf, string tagName)
        {
            var tag = mf.GetTag(tagName);
            if (string.IsNullOrWhiteSpace(tag?.Value) || int.TryParse(tag.Value, out int result) == false)
            {
                return null;
            }
            return result;
        }
        [Obsolete]
        public static T GetTag<T>(this MusicFile mf, string tagName)
        {
            //tmd.TrackNumber = mf.IdTags.SingleOrDefault(x => x.Name == "Track").GetValue<int>();// extension methods can be called on null!
            return mf.IdTags.SingleOrDefault(x => string.Compare(x.Name, tagName, true) == 0).GetValue<T>();
        }
        public static IdTag GetTag(this MusicFile mf, string tagName)
        {
            return mf.IdTags.FirstOrDefault(t => string.Compare(t.Name, tagName, true) == 0);
            //return mf.IdTags.SingleOrDefault(x => string.Compare(x.Name, tagName, true) == 0).GetValue<T>();
        }
        //public static void Delete(this MusicDb musicDb, MusicFile mf)
        //{
        //    musicDb.RemovePlaylistItems(mf);
        //    var tags = mf.IdTags.ToArray();
        //    musicDb.IdTags.RemoveRange(tags);
        //    musicDb.MusicFiles.Remove(mf);
        //}
        public static void RemovePlaylistItems<T>(this MusicDb musicDb, T entity)
        {
            PlaylistItemType itemType = PlaylistItemType.MusicFile;
            long itemId = 0;
            switch (entity)
            {
                case Performance p:
                    itemType = PlaylistItemType.Performance;
                    itemId = p.Id;
                    break;
                case Work w:
                    itemType = PlaylistItemType.Work;
                    itemId = w.Id;
                    break;
                case Track t:
                    itemType = PlaylistItemType.Track;
                    itemId = t.Id;
                    break;
                case MusicFile mf:
                    itemType = PlaylistItemType.MusicFile;
                    itemId = mf.Id;
                    break;
            }
            var items = musicDb.PlaylistItems.Where(x => x.Type == itemType && x.ItemId == itemId).ToArray();
            foreach (var item in items)
            {
                var playlist = item.Playlist;
                item.Playlist = null;
                playlist.Items.Remove(item);
                musicDb.PlaylistItems.Remove(item);
                //log.Information($"playlist item {item.Title} removed from {playlist.Name}");
                if (playlist.Items.Count() == 0)
                {
                    musicDb.Playlists.Remove(playlist);
                    //log.Information($"playlist {playlist.Name} removed");
                }
            }
        }
        //public static string GetMostRecentOpusCoverFile(this MusicStyles musicStyle, MusicOptions musicOptions, Work work, string opusPath = null)
        //{
        //    return musicStyle.GetMostRecentOpusCoverFile(musicOptions, work.Artist.Type, work.Artist.Name, opusPath ??= work.Name);
        //}
        public static string GetMostRecentOpusCoverFile(this Work work, MusicOptions musicOptions)
        {
            //var musicFiles = work.Tracks.SelectMany(t => t.MusicFiles)
            //    .Where(mf => !mf.IsGenerated).AsEnumerable();
            var folders = new List<string>();
            foreach(var mf in work.Tracks.SelectMany(t => t.MusicFiles)
                .Where(mf => !mf.IsGenerated))
            {
                //var pathFragments = new List<string>(new string[] { mf.DiskRoot, mf.StylePath });
                //if(work.Type == OpusType.Collection)
                //{
                //    pathFragments.Add("Collections");
                //}
                //pathFragments.AddRange(mf.OpusPath.Split("\\"));
                //folders.Add(Path.Combine(pathFragments.ToArray()));
                folders.Add(mf.GetRootPath());
            }
            IEnumerable<string> imageFiles = null;
            try
            {
                foreach (var pattern in musicOptions.CoverFilePatterns)
                {
                    foreach (var path in folders)
                    {
                        imageFiles = imageFiles?.Union(Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories)) ?? Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories);
                    }
                }
                return imageFiles.OrderByDescending(x => new FileInfo(x).LastWriteTime).FirstOrDefault();
            }
            catch (Exception xe)
            {
                log.Error(xe, $"called with [W-{work.Id}] {work.Name}, imagefiles: {imageFiles?.Count().ToString() ?? "null"}");
            }
            return null;
        }

    }
    public static class plExtensions
    {
        public static string GetDisplayName(this MusicDb db, Performance performance)
        {
            //await db.Entry(performance).Reference(x => x.Composition).LoadAsync();
            //await db.Entry(performance.Composition).Reference(x => x.Artist).LoadAsync();
            var parts = performance.Composition.Artist.Name.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return $"{parts.Last()}, {performance.Composition.Name}";
        }
        public static IEnumerable<(Track track, long musicFileId)> GetTracks(this MusicDb db, IPlayable playable)
        {
            var list = new List<(Track track, long musicFileId)>();
            //await db.LoadRelatedEntities(playable);
            foreach (var track in playable.Tracks.OrderBy(t => t.Number))
            {
                //await db.LoadRelatedEntities(track);
                list.Add((track, track.MusicFiles.OrderByDescending(mf => mf.Rank()).First().Id));
            }
            return list;
        }
        private static async Task LoadRelatedEntities<T>(this MusicDb db, T playable) where T : class, IPlayable
        {
            switch (playable)
            {
                case Work work:
                    await db.LoadRelatedEntities(work);
                    break;
                case Performance performance:
                    await db.LoadRelatedEntities(performance);
                    break;
            }
        }
        private static async Task LoadRelatedEntities(this MusicDb db, Performance performance)
        {
            await db.Entry(performance).Collection(x => x.Movements).LoadAsync();
        }
        private static async Task LoadRelatedEntities(this MusicDb db, Work work)
        {
            await db.Entry(work).Collection(x => x.Tracks).LoadAsync();
        }
        private static async Task LoadRelatedEntities(this MusicDb db, Track track)
        {
            await db.Entry(track).Collection(x => x.MusicFiles).LoadAsync();
        }
    }
    public class PlaylistEntry
    {
        //public PlaylistEntryType Type { get; set; }
        public int Sequence { get; set; }
        public string Title { get; set; }
        public long PlaylistItemId { get; set; }
        public long PlaylistSubItemId { get; set; }
        public double TotalTime { get; set; }
        public List<PlaylistEntry> SubEntries { get; set; }
    }
}
