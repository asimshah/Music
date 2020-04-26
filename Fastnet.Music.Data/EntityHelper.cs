using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fastnet.Music.Data
{
    public partial class EntityHelper
    {
        private readonly ILogger log;
        private readonly MusicDb db;
        private readonly DeleteContext context;

        // helps delete
        // MusicFile, Track, Work, Artist
        // Performance, Performer, Composition
        // Raga
        public EntityHelper(MusicDb db, DeleteContext ctx)
        {
            this.log = ApplicationLoggerFactory.CreateLogger<EntityHelper>();
            this.db = db;
            this.context = ctx;
        }
        public EntityHelper(MusicDb db, TaskItem taskItem) : this(db, new DeleteContext(taskItem))
        {

        }
        public void Delete(MusicFile musicFile)
        {
            BubbleDelete(musicFile);
        }
        public void Delete(Performance performance)
        {
            BubbleDelete(performance);
        }
        public IEnumerable<long> GetModifiedArtistIds()
        {
            return this.context?.ModifiedArtistList ?? Enumerable.Empty<long>();
        }
        public IEnumerable<long> GetDeletedArtistIds()
        {
            return this.context?.DeletedArtistList ?? Enumerable.Empty<long>();
        }
        private string ToIdent()
        {
            return context.ToString();
        }
        /// <summary>
        /// Delete entity and any related parent entities that should also be deleted 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        private void BubbleDelete<T>(T entity) where T : EntityBase
        {
            switch (entity)
            {
                case MusicFile mf:
                    RemoveReferencingEntitiesIfRequired(mf);
                    RemoveEntity(mf);
                    break;
                case Track track:
                    RemoveReferencingEntitiesIfRequired(track);
                    RemoveEntity(track);
                    break;
                case Work work:
                    RemoveReferencingEntitiesIfRequired(work);
                    RemoveEntity(work);
                    break;
                case Artist artist:
                    RemoveEntity(artist);
                    //RemoveReferencingEntitiesIfRequired(work);
                    break;
                case Performance performance:
                    RemoveReferencingEntitiesIfRequired(performance);
                    RemoveEntity(performance);
                    break;
                case Composition composition:
                    RemoveReferencingEntitiesIfRequired(composition);
                    RemoveEntity(composition);
                    break;
                case Raga raga:
                    RemoveEntity(raga);
                    break;
                default:
                    log.Error($"{ToIdent()} Delete<T> does not support type {typeof(T).Name}");
                    break;
            };

        }
        private void RemoveEntity(MusicFile musicFile)
        {
            DeleteRelatedPlaylistItems(musicFile);
            DeleteRelatedIdTags(musicFile);
            db.MusicFiles.Remove(musicFile);
            log.Information($"{ToIdent()} {musicFile.ToIdent()} {musicFile.File} deleted");
        }
        private void RemoveEntity(Composition composition)
        {
            Debug.Assert(composition.Performances.Count() == 0);
            context.SetModifiedArtistId(composition.ArtistId);
            db.Compositions.Remove(composition);
            log.Information($"{ToIdent()} {composition.ToIdent()} removed");
        }
        private void RemoveEntity(Performance performance)
        {
            Debug.Assert(performance.Movements.Count() == 0);
            switch (performance.StyleId)
            {
                case MusicStyles.WesternClassical:
                    //context.SetModifiedArtistId(performance.Composition.ArtistId);
                    break;
                case MusicStyles.IndianClassical:
                    var idlist = db.RagaPerformances.Where(x => x.Performance == performance).Select(x => x.ArtistId);
                    context.SetModifiedArtistId(idlist.ToArray());
                    break;
            }

            db.Performances.Remove(performance);
            log.Information($"{ToIdent()} {performance.ToIdent()} removed");
        }
        private void RemoveEntity(Track track)
        {
            Debug.Assert(track.MusicFiles.Count() == 0);
            context.SetModifiedArtistId(track.Work.ArtistWorkList.Select(x => x.ArtistId).ToArray());
            db.Tracks.Remove(track);
            log.Information($"{ToIdent()} {track.ToIdent()} {track.Title} removed");
        }
        private void RemoveEntity(Work work)
        {
            Debug.Assert(work.Tracks.Count() == 0);
            context.SetModifiedArtistId(work.ArtistWorkList.Select(x => x.ArtistId).ToArray());
            db.Works.Remove(work);
            log.Information($"{ToIdent()} {work.ToIdent()} {work.Name} removed");
        }
        private void RemoveEntity(Raga raga)
        {
            Debug.Assert(db.RagaPerformances.Where(x => x.Raga == raga).Count() == 0);
            db.Ragas.Remove(raga); // i don't bubble delete becuase ragas have no parents
            log.Information($"{ToIdent()} {raga.ToIdent()} {raga.Name} deleted");
        }
        private void RemoveEntity(Artist artist)
        {
            Debug.Assert(artist.Works.Count() == 0);
            var aslist = db.ArtistStyles.Where(x => x.Artist == artist);
            db.ArtistStyles.RemoveRange(aslist);
            log.Information($"{ToIdent()} {aslist.Select(x => x.ToIdent()).ToCSV()} removed");
            context.SetDeletedArtistId(artist.Id);
            db.Artists.Remove(artist);
            log.Information($"{ToIdent()} {artist.ToIdent()} {artist.Name} removed");
        }
        /// <summary>
        /// removes any entity that refers to music file if that entity is now 'empty'
        /// currently checks the Track property
        /// </summary>
        /// <param name="musicFile"></param>
        private void RemoveReferencingEntitiesIfRequired(MusicFile musicFile)
        {
            var track = musicFile.Track;
            Debug.Assert(track != null);
            track.MusicFiles.Remove(musicFile);
            if (track.MusicFiles.Count() > 0 && track.MusicFiles.All(x => x.IsGenerated))
            {
                // remaining music files are system generated
                foreach (var mf in track.MusicFiles.ToArray())
                {
                    RemoveEntity(mf);
                }
                track.MusicFiles.Clear();
            }
            if (track.MusicFiles.Count() == 0)
            {
                BubbleDelete(track);
            }
        }
        /// <summary>
        /// removes any entity that refers to track if that entity is now 'empty'
        /// currently checks the Performance and Work properties
        /// </summary>
        /// <param name="track"></param>
        private void RemoveReferencingEntitiesIfRequired(Track track)
        {
            var performance = track.Performance;
            if (performance != null)
            {
                track.Performance = null;
                performance.Movements.Remove(track);
                if (performance.Movements.Count() == 0)
                {
                    BubbleDelete(performance);
                }
            }
            var work = track.Work;
            Debug.Assert(work != null);
            work.Tracks.Remove(track);
            if (work.Tracks.Count() == 0)
            {
                BubbleDelete(work);
            }
        }
        private void RemoveReferencingEntitiesIfRequired(Work work)
        {
            var awlist = work.ArtistWorkList.ToArray();
            var artists = awlist.Select(x => x.Artist).ToArray();
            db.ArtistWorkList.RemoveRange(awlist);
            log.Information($"{ToIdent()} {awlist.Select(x => x.ToIdent()).ToCSV()} deleted");
            foreach (var artist in artists)
            {
                var aw = artist.ArtistWorkList.Single(x => x.Work == work);
                artist.ArtistWorkList.Remove(aw);
                if (artist.ArtistWorkList.Count() == 0)
                {
                    BubbleDelete(artist);
                }
            }
        }
        private void RemoveReferencingEntitiesIfRequired(Performance performance)
        {
            DeletedRelatedPerformancePerformers(performance);
            switch (performance.StyleId)
            {
                case MusicStyles.WesternClassical:
                    var cpList = performance.CompositionPerformances.ToArray();
                    Debug.Assert(cpList.Count() == 1);
                    var cp = cpList.Single();
                    var composition = cp.Composition;
                    context.SetModifiedArtistId(composition.ArtistId);
                    composition.CompositionPerformances.Remove(cp);
                    performance.CompositionPerformances.Remove(cp);
                    db.CompositionPerformances.Remove(cp);
                    log.Information($"{ToIdent()} {cp.ToIdent()} deleted");
                    if (composition.CompositionPerformances.Count() == 0)
                    {
                        BubbleDelete(composition);
                    }
                    break;
                case MusicStyles.IndianClassical:
                    // rplist.Count() can be > 1
                    // because a performance of a raga may be by multiple artists
                    // though any one performance can only be of one raga
                    var rplist = db.RagaPerformances.Where(x => x.Performance == performance).ToArray();
                    // removal of a performance may mean that
                    // there are no more performances of that raga - in which case delete the raga
                    // REMEMBER that artists in IndianClassical must all have works and therefore are
                    // bubble deleted if the work gets deleted
                    Debug.Assert(rplist.Select(x => x.Raga).Count() == 1);
                    var raga = rplist.Select(x => x.Raga).Single();
                    db.RagaPerformances.RemoveRange(rplist);
                    log.Information($"{ToIdent()} {rplist.Select(x => x.ToIdent()).ToCSV()} deleted");
                    if (db.RagaPerformances.Where(x => x.Raga == raga).Count() == 0)
                    {
                        BubbleDelete(raga);
                    }
                    break;
            }
        }
        private void RemoveReferencingEntitiesIfRequired(Composition composition)
        {
            var composer = composition.Artist;
            composer.Compositions.Remove(composition);
            if (composer.Compositions.Count() == 0)
            {
                if (composer.Works.Count() == 0)
                {
                    BubbleDelete(composer);
                }
            }
        }
        private void DeleteRelatedIdTags(MusicFile musicFile)
        {
            var tags = musicFile.IdTags.ToArray();
            db.IdTags.RemoveRange(tags);
            log.Information($"{ToIdent()} {musicFile.ToIdent()} {tags.Count()} idtags deleted");
        }
        private void DeleteRelatedPlaylistItems<T>(T entity)
        {
            (PlaylistItemType itemType, long itemId) = entity switch
            {
                Performance p => (PlaylistItemType.Performance, p.Id),
                Work w => (PlaylistItemType.Work, w.Id),
                Track t => (PlaylistItemType.Track, t.Id),
                MusicFile mf => (PlaylistItemType.MusicFile, mf.Id),
                _ => throw new Exception($"{ToIdent()} Parameter {nameof(entity)} type {entity.GetType().Name} not supported")
            };
            var items = db.PlaylistItems.Where(x => x.Type == itemType && x.ItemId == itemId).ToArray();
            foreach (var item in items)
            {
                var playlist = item.Playlist;
                item.Playlist = null;
                playlist.Items.Remove(item);
                db.PlaylistItems.Remove(item);
                log.Information($"{ToIdent()} playlist item {item.Title} removed from {playlist.Name} and deleted");
                if (playlist.Items.Count() == 0)
                {
                    db.Playlists.Remove(playlist);
                    log.Information($"{ToIdent()} playlist {playlist.Name} deleted");
                }
            }
        }
        private void DeletedRelatedPerformancePerformers(Performance performance)
        {
            var pplist = performance.PerformancePerformers.ToArray();
            var performers = pplist.Select(x => x.Performer);
            db.PerformancePerformers.RemoveRange(pplist);
            performance.PerformancePerformers.Clear();
            log.Information($"{ToIdent()} {pplist.Select(x => x.ToIdent()).ToCSV()} deleted");
            foreach (var performer in performers)
            {
                if (db.PerformancePerformers.Where(x => x.Performer == performer).Count() == 0)
                {
                    db.Performers.Remove(performer);
                    log.Information($"{ToIdent()} {performer.ToIdent()} {performer} deleted");
                }
            }
        }
    }
}
