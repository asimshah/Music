using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fastnet.Music.Data
{
    public static partial class Extensions
    {
        private static readonly ILogger log = ApplicationLoggerFactory.CreateLogger("Fastnet.Music.Data.Extensions");
        // musicdb extensions
        private static void Delete(this MusicDb musicDb, MusicFile mf, /*Opus*/DeleteContext context)
        {
            void clearRelatedEntities(MusicFile file)
            {
                musicDb.RemovePlaylistItems(file);
                var tags = file.IdTags.ToArray();
                musicDb.IdTags.RemoveRange(tags);
            }
            clearRelatedEntities(mf);
            var track = mf.Track;
            if (track != null)
            {
                track.MusicFiles.Remove(mf);
                if (track.MusicFiles.Count() > 0 && track.MusicFiles.All(x => x.IsGenerated))
                {
                    foreach (var f in track.MusicFiles.ToArray())
                    {
                        clearRelatedEntities(f);
                        f.Track = null;
                        track.MusicFiles.Remove(f);
                        musicDb.MusicFiles.Remove(f);
                        log.Information($"{context}: {mf.ToIdent()} Musicfile deleted: {f.File}");
                    }
                }
                if (track.MusicFiles.Count() == 0)
                {
                    musicDb.Delete(track, context);
                } 
            }
            musicDb.MusicFiles.Remove(mf);
            log.Information($"{context}: {mf.ToIdent()} Musicfile  deleted: {mf.File}");
        }
        /// <summary>
        /// Gets a collection of Performer entries, creating any that are new
        /// </summary>
        /// <param name="db"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IEnumerable<Performer> GetPerformers(this MusicDb db, IEnumerable<MetaPerformer> list, TaskItem taskItem = null)
        {
            return list.Select(n => db.GetPerformer(n, taskItem));
        }
        /// <summary>
        /// Gets a collection of Performer entries, creating any that are new
        /// </summary>
        /// <param name="db"></param>
        /// <param name="names"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Performer> GetPerformers(this MusicDb db, IEnumerable<string> names, PerformerType type)
        {
            var list = names.Select(n => new MetaPerformer(type, n));
            return db.GetPerformers(list);
        }
        /// <summary>
        /// Gets the correspnding Performer nentry, creating one if required
        /// </summary>
        /// <param name="db"></param>
        /// <param name="mp"></param>
        /// <returns></returns>
        public static Performer GetPerformer(this MusicDb db, MetaPerformer mp, TaskItem taskItem = null)
        {
            if(mp.Name == "collections")
            {
                Debugger.Break();
            }
            var alphamericName = mp.Name.ToAlphaNumerics().ToLower();

            db.Performers.Load();
            var performer = db.Performers.Local
                .SingleOrDefault(p => p.AlphamericName.ToLower() == alphamericName && p.Type == mp.Type);
            if (performer == null)
            {
                performer = new Performer
                {
                    AlphamericName = alphamericName,
                    Name = mp.Name,
                    Type = mp.Type
                };
                db.Performers.Add(performer);
                log.Information($"{taskItem?.ToString() ?? "[No-TI]"} Performer {performer} added");
            }
            return performer;
        }
        /// <summary>
        /// Gets the corresponding Performer nentry, creating one if required
        /// </summary>
        /// <param name="db"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Performer GetPerformer(this MusicDb db, string name, PerformerType type)
        {
            var mp = new MetaPerformer(type, name);
            return db.GetPerformer(mp);
        }
        /// <summary>
        /// returns a collection of performers that exist in the database
        /// i.e. the collection may not conatin all the names asked for (as they were not found in the db)
        /// </summary>
        /// <param name="db"></param>
        /// <param name="names"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Performer> FindPerformers(this MusicDb db, IEnumerable<string> names, PerformerType type)
        {
            var list = new List<Performer>();
            foreach (var name in names)
            {
                var performer = db.Performers
                    .SingleOrDefault(p => p.AlphamericName.ToLower() == name.ToAlphaNumerics().ToLower() && p.Type == type);
                if (performer != null)
                {
                    list.Add(performer);
                }
            }
            return list;
        }
        public static async Task ReplacePerformer(this MusicDb db, Performer performer, Performer validPerformer)
        {
            try
            {
                log.Information($"replacing [Pf-{performer.Id}] with [Pf-{validPerformer.Id}]");
                var ppList = performer.PerformancePerformers.ToArray();
                var performances = ppList.Select(x => x.Performance).ToArray();
                db.Performers.Remove(performer);
                db.PerformancePerformers.RemoveRange(ppList);
                log.Information($"removed pairs {(string.Join(", ", ppList.Select(x => x.ToString())))}");
                await db.SaveChangesAsync();
                foreach (var performance in performances)
                {
                    var newPP = new PerformancePerformer { Performer = validPerformer, Performance = performance };
                    log.Information($"adding pair [Pf-{validPerformer.Id}+p-{performance.Id}]");
                    if (db.PerformancePerformers.SingleOrDefault(pp => pp.PerformerId == validPerformer.Id && pp.PerformanceId == performance.Id) == null)
                    {
                        db.PerformancePerformers.Add(newPP);
                    }

                }

                log.Information($"[Pf-{performer.Id}] {performer} removed, replaced with [Pf-{validPerformer.Id}] {validPerformer} ");
            }
            catch (DbUpdateConcurrencyException xe)
            {
                xe.Report();
                throw;
            }
            catch (Exception xe)
            {
                log.Error(xe);
                throw;
            }
        }
        private static void Delete(this MusicDb musicDb, Track track, DeleteContext context)
        {
            var artistIds = track.Work.Artists.Select(x => x.Id).ToArray();
            foreach (var musicFile in track.MusicFiles.ToArray())
            {
                musicFile.Track = null;
                musicDb.Delete(musicFile, context);
            }
            var performance = track.Performance;
            performance?.Movements.Remove(track);
            if (performance?.Movements.Count() == 0)
            {
                musicDb.Delete(performance, context);
            }
            var work = track.Work;
            work?.Tracks.Remove(track);
            if (work?.Tracks.Count() == 0)
            {
                musicDb.Delete(work, context);
            }
            musicDb.Tracks.Remove(track);
            context.SetModifiedArtistId(artistIds.ToArray());
            log.Information($"{context}: Track [T-{track.Id}] deleted: {track.Title}");
        }
        private static void Delete(this MusicDb musicDb, Artist artist, DeleteContext context)
        {
            long artistId = artist.Id;
            foreach (var composition in artist.Compositions)
            {
                composition.Artist = null;
                musicDb.Delete(composition, context);
            }
            foreach (var work in artist.Works)
            {
                //work.Artist = null;
                musicDb.Delete(work, context);
            }
            var styles = artist.ArtistStyles.ToArray();
            musicDb.ArtistStyles.RemoveRange(styles);
            musicDb.Artists.Remove(artist);
            context.SetDeletedArtistId(artistId);
            log.Information($"{context}: Artist [A-{artist.Id}] deleted: {artist.Name}");
        }
        private static void Delete(this MusicDb musicDb, Composition composition, DeleteContext context)
        {
            long artistId = composition.ArtistId;
            foreach (var performance in composition.Performances.ToArray())
            {
                musicDb.RemovePerformance(composition, performance);
                musicDb.Delete(performance, context);
            }
            musicDb.Compositions.Remove(composition);
            var artist = composition.Artist;
            artist?.Compositions.Remove(composition);
            if (artist != null)
            {
                if (artist.Works.Count() == 0 && artist.Compositions.Count() == 0)
                {
                    musicDb.Delete(artist, context);
                }
            }
            context.SetModifiedArtistId(artistId);
            log.Information($"{context}: Composition [C-{composition.Id}] deleted: {composition.Name}");
        }
        private static void Delete(this MusicDb musicDb, Work work, DeleteContext context)
        {
            var artistIds = work.Artists.Select(x => x.Id).ToArray();
            foreach (var track in work.Tracks)
            {
                track.Work = null;
                musicDb.Delete(track, context);
            }
            var artists = work.Artists.ToArray();
            var list = work.ArtistWorkList.ToArray();
            musicDb.ArtistWorkList.RemoveRange(list);
            work.ArtistWorkList.Clear();           
            foreach(var artist in artists)
            {
                var aw = artist.ArtistWorkList.Single(x => x.Work == work);
                artist.ArtistWorkList.Remove(aw);
                if (artist.Works.Count() == 0 && artist.Compositions.Count() == 0)
                {
                    musicDb.Delete(artist, context);
                }
            }
            musicDb.Works.Remove(work);
            context.SetModifiedArtistId(artistIds.ToArray());
            log.Information($"{context}: Work [W-{work.Id}] deleted: {work.Name}");
        }
        private static void Delete(this MusicDb musicDb, Performance performance, DeleteContext context)
        {
            long artistId = performance.Composition.ArtistId;
            foreach (var movement in performance.Movements)
            {
                movement.Performance = null;
                // we do not delete movements here because
                // a movement is a track and tracks are also in an album
            }
            performance.Movements.Clear();
            var compositions = Enumerable.Empty<Composition>();
            var ragas = Enumerable.Empty<Raga>();
            switch (performance.StyleId)
            {
                case MusicStyles.WesternClassical:
                    compositions = performance.CompositionPerformances.Select(x => x.Composition);
                    if (compositions.Count() > 1)
                    {
                        log.Error($"{performance.ToIdent()} is associated with more than one {performance.CompositionPerformances.Select(x => x.ToIdent()).ToCSV()}");
                    }
                    break;
                case MusicStyles.IndianClassical:
                    ragas = musicDb.RagaPerformances.Where(x => x.Performance == performance)
                        .Select(x => x.Raga);
                    break;
            }
            foreach (var composition in compositions)
            {
                musicDb.RemovePerformance(composition, performance);
                if (composition.Performances.Count() == 0)
                {
                    musicDb.Delete(composition, context);
                }
            }
            var performersCSV = performance.GetAllPerformersCSV();
            var ppList = performance.PerformancePerformers.ToArray();
            foreach (var pp in ppList)
            {
                pp.Performance.PerformancePerformers.Remove(pp);
                pp.Performer.PerformancePerformers.Remove(pp);
            }
            musicDb.PerformancePerformers.RemoveRange(ppList);
            var performers = ppList.Select(x => x.Performer);
            foreach (var performer in performers.ToArray())
            {
                var count = performer.PerformancePerformers.Count();
                if (count == 0)
                {
                    musicDb.Performers.Remove(performer);
                    log.Information($"{context}: [Pf-{performer.Id}] performer {performer.Name}, {performer.Type} deleted");
                }
            }
            musicDb.Performances.Remove(performance);
            context.SetModifiedArtistId(artistId);
            log.Information($"{context}: Performance [P-{performance.Id}] deleted: {performersCSV}");
        }
        private static void RemovePlaylistItems<T>(this MusicDb musicDb, T entity)
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
    }
    public static partial class Extensions
    {
        // musicFile extensions
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
        public static IdTag GetTag(this MusicFile mf, string tagName)
        {
            return mf.IdTags.FirstOrDefault(t => string.Compare(t.Name, tagName, true) == 0);
            //return mf.IdTags.SingleOrDefault(x => string.Compare(x.Name, tagName, true) == 0).GetValue<T>();
        }
    }
    public static partial class Extensions
    {
        public static string GetMostRecentOpusCoverFile(this Work work, MusicOptions musicOptions)
        {
            var folders = new List<string>();
            foreach (var mf in work.Tracks.SelectMany(t => t.MusicFiles)
                .Where(mf => !mf.IsGenerated))
            {
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
        public static MusicFile GetBestQuality(this Track t)
        {
            return t.MusicFiles.Where(x => x.IsGenerated == false).OrderByDescending(x => x.Rank()).First();
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
        public static void Report(this DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                log.Information($"entry type {entry.Entity.GetType().Name}");
                var proposedValues = entry.CurrentValues;
                var databaseValues = entry.GetDatabaseValues();

                foreach (var property in proposedValues.Properties)
                {
                    var proposedValue = proposedValues[property];
                    var databaseValue = databaseValues[property];
                    log.Information($"property {property.Name}: database value {databaseValue.ToString()}, proposed value {proposedValue.ToString()}");
                }
            }
        }
    }
    public static partial class Extensions
    {
        public static CompositionPerformance AddPerformance(this MusicDb musicDb, Composition composition, Performance performance)
        {
            var cp = new CompositionPerformance { Performance = performance, Composition = composition };
            composition.CompositionPerformances.Add(cp);
            performance.CompositionPerformances.Add(cp);
            musicDb.CompositionPerformances.Add(cp);
            return cp;
        }
        public static void RemovePerformance(this MusicDb musicDb, Composition composition, Performance performance)
        {
            if (composition != null)
            {
                var cp = composition.CompositionPerformances.FirstOrDefault(x => x.Performance == performance);
                if (cp == null)
                {
                    log.Error($"{performance.ToIdent()} not found in CompositionPerformances for {composition.ToIdent()} {composition}");
                }
                else
                {
                    composition.CompositionPerformances.Remove(cp);
                    musicDb.CompositionPerformances.Remove(cp);
                } 
            }
        }
        public static ArtistWork AddWork(this MusicDb musicDb, Artist artist, Work work)
        {
            var aw = new ArtistWork { Artist = artist, Work = work };

            artist.ArtistWorkList.Add(aw);
            musicDb.ArtistWorkList.Add(aw);
            return aw;
        }
        public static void RemoveWork(this MusicDb musicDb, Artist artist, Work work)
        {
            var aw = artist.ArtistWorkList.FirstOrDefault(x => x.Work == work);
            if(aw == null)
            {
                log.Error($"[W-{work.Id}] {work} not found in ArtistWorkList for [A-{artist.Id}] {artist}");
            }
            else
            {
                artist.ArtistWorkList.Remove(aw);
                musicDb.ArtistWorkList.Remove(aw);
                //log.Information($"{aw} removed");
            }            
        }

    }
}
