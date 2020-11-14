using Fastnet.Core;
using Fastnet.Core.Logging;
//using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Music.Data
{
    public enum EntityChangeType
    {
        Delete
    }
    public class EntityChangedEventArgs : EventArgs
    {
        internal static EntityChangedEventArgs Create(EntityChangeType type, string entityName, long entityId, string logMessage, params long[] artistIds)
        {
            var idlist = artistIds ?? new long[0];
            var args = new EntityChangedEventArgs
            {
                EntityName = entityName,
                Type = type,
                EntityId = entityId,
                LogMessage = logMessage
            };
            args.AddAffectedArtistId(idlist);
            return args;
        }
        public EntityChangeType Type { get; set; }
        public string EntityName { get; set; }
        public long EntityId { get; set; }
        public List<long> AffectedArtistIdList { get; set; } = new List<long>();
        public string LogMessage { get; set; } = string.Empty;
        internal void AddAffectedArtistId(params long[] ids)
        {
            AffectedArtistIdList.AddRange(ids);
        }
    }
    public class EntityObserver
    {
        public event EventHandler<EntityChangedEventArgs> EntityChanged;
        public void RaiseEntityChanged(EntityChangedEventArgs args)
        {
            EntityChanged?.Invoke(this, args);
        }
    }
    public partial class EntityHelper
    {
        private MusicDb db;
        private TaskItem taskItem;
        private readonly ILogger log;
        private readonly EntityObserver entityObserver;
        private readonly IOptionsMonitor<IndianClassicalInformation> iciMonitor;
        // helps delete
        // MusicFile, Track, Work, Artist
        // Performance, Performer, Composition
        // Raga
        public EntityHelper(IOptionsMonitor<IndianClassicalInformation> iciMonitor, EntityObserver entityObserver, ILogger<EntityHelper> logger)
        {
            this.iciMonitor = iciMonitor;
            this.log = logger;
            this.entityObserver = entityObserver;
        }
        public void Enable(MusicDb db, TaskItem taskItem)
        {
            this.db = db;
            this.taskItem = taskItem;
        }
        public void Delete(MusicFile musicFile)
        {
            BubbleDelete(musicFile);
        }
        public void Delete(Performance performance)
        {
            BubbleDelete(performance);
        }
        public Artist FindArtist(string name)
        {
            // i use db.Artists.Local in case this artist is recently added

            // **Outstanding ** should this be looking for an artist with matching music style?
            db.Artists.Load();
            try
            {
                return db.Artists.Local.SingleOrDefault(a => a.Name.IsEqualIgnoreAccentsAndCase(name));
            }
            catch (Exception xe)
            {
                log.Error($"{xe.Message}");
                throw;
            }
        }
        public async Task<T> FindEntityAsync<T>(long id) where T : EntityChangedEventArgs
        {
            return await db.Set<T>().FindAsync(id);
        }
        public async Task<MusicFile> FindMusicFileAsync(string filename)
        {
            return await db.MusicFiles.SingleOrDefaultAsync(x => x.File == filename);
        }
        public IEnumerable<MusicFile> FindMatchingFiles(string path, bool includeDescendants = false)
        {
            var targetFiles = db.MusicFiles.Where(x => x.File.ToLower().StartsWith(path.ToLower())).ToArray();
                //.OrderBy(f => f.File);
            if(includeDescendants)
            {
                return targetFiles;
            }
            //return targetFiles
            //    .Where(x => x.IsMultiPart && Path.Combine(x.DiskRoot, x.StylePath, x.OpusPath, x.PartName).ToLower() == path.ToLower()
            //        || x.IsMultiPart == false && Path.Combine(x.DiskRoot, x.StylePath, x.OpusPath).ToLower() == path.ToLower())
            //        .OrderBy(f => f.File);

            return targetFiles
                .Where(x => x.IsMultiPart && Path.Combine(x.GetRootPath(), x.PartName).ToLower() == path.ToLower()
                    || x.IsMultiPart == false && x.GetRootPath().ToLower() == path.ToLower())
                    .OrderBy(f => f.File);
        }
        public Work FindWorkByArtistIdsAndName(IEnumerable<long> idlist, string name)
        {
            idlist = idlist.OrderBy(x => x);
            var alphamericName = name.ToAlphaNumerics();
            var q1 = db.ArtistWorkList
                .Where(aw => aw.Work.AlphamericName == alphamericName && idlist.Contains(aw.Artist.Id))
                .Select(x => x.Work).Distinct()
                .Join(db.ArtistWorkList, w => w.Id, awl => awl.WorkId, (w, awl) => awl)
                .AsEnumerable();
            var q2 = q1.GroupBy(x => x.Work)
                .Where(g => g.Select(x => x.ArtistId).OrderBy(id => id).SequenceEqual(idlist));
            var work = q2.Select(aw => aw.Key).SingleOrDefault();
            return work;
        }
        public Performer GetPerformer(MetaPerformer mp)
        {
            //if (mp.Name == "collections")
            //{
            //    Debugger.Break();
            //}
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
                //log.Information($"{taskItem?.ToString() ?? "[No-TI]"} Performer {performer} added");
            }
            return performer;
        }
        public Raga GetRaga(string name)
        {
            //Debug.Assert(MusicDb != null);
            var ici = iciMonitor.CurrentValue;
            var lowerAlphaNumericName = name.ToAlphaNumerics().ToLower();
            var raga = db.Ragas.SingleOrDefault(r => r.AlphamericName.ToLower() == lowerAlphaNumericName);
            if (raga == null)
            {
                var alphamericName = name.ToAlphaNumerics();
                raga = new Raga
                {
                    Name = name,
                    DisplayName = string.IsNullOrWhiteSpace(ici.Lookup[alphamericName].DisplayName) ? $"Raga {name}" : ici.Lookup[alphamericName].DisplayName,
                    AlphamericName = name.ToAlphaNumerics()
                };
                db.Ragas.Add(raga);
            }
            return raga;
        }
        public IEnumerable<Performer> GetPerformers(IEnumerable<MetaPerformer> list)
        {
            return list.Select(n => GetPerformer(n));
        }
        public async Task<Artist> GetArtistAsync(MusicStyles musicStyle, string name, ArtistType type = ArtistType.Artist)
        {
            Artist artist = FindArtist(name);
            if (artist == null)
            {
                artist = new Artist
                {
                    UID = Guid.NewGuid(),
                    Name = name,
                    AlphamericName = name.ToAlphaNumerics(),
                    Type = type, // ArtistType.Artist,
                    OriginalName = name,
                };
                artist.ArtistStyles.Add(new ArtistStyle { Artist = artist, StyleId = musicStyle });
                await db.Artists.AddAsync(artist);
                await db.SaveChangesAsync();
            }
            else
            {
                if (artist.ArtistStyles.SingleOrDefault(x => x.StyleId == musicStyle) == null)
                {
                    artist.ArtistStyles.Add(new ArtistStyle { Artist = artist, StyleId = musicStyle });
                    //log.Information($"Existing artist {artist.ToIdent()} {artist.Name} added to style {MusicStyle}");
                    await db.SaveChangesAsync();
                }
            }

            return artist;
        }
        public CompositionPerformance AddPerformance(Composition composition, Performance performance)
        {
            var cp = new CompositionPerformance { Performance = performance, Composition = composition };
            composition.CompositionPerformances.Add(cp);
            performance.CompositionPerformances.Add(cp);
            db.CompositionPerformances.Add(cp);
            return cp;
        }
        public void AddRagaPerformance(Artist artist, Raga raga, Performance performance)
        {
            var rp = new RagaPerformance
            {
                Artist = artist,
                Raga = raga,
                Performance = performance
            };
            db.RagaPerformances.Add(rp);
        }
        public ArtistWork AddWork(Artist artist, Work work)
        {
            var aw = new ArtistWork { Artist = artist, Work = work };

            artist.ArtistWorkList.Add(aw);
            db.ArtistWorkList.Add(aw);
            return aw;
        }
        public async Task AddEntityAsync<T>(T entity) where T : EntityBase
        {
            await db.Set<T>().AddAsync(entity);
        }
        public bool ValidateTags(MusicFile mf)

        {
            bool result = true;
            bool isTagPresent(string tagName, bool logIfMissing = false)
            {
                var r = mf.IdTags.SingleOrDefault(x => string.Compare(x.Name, tagName, true) == 0) != null;
                if (!r && logIfMissing)
                {
                    log.Error($"{mf.File} tag {tagName} not present");
                }
                return r;
            }
            if (mf.IsGenerated)
            {
                result = true;
            }
            else
            {
                var standardTags = new string[] { "Artist", "Album", "TrackNumber", "Title" };
                if (standardTags.Any(t => isTagPresent(t, true) == false))
                {
                    result = false;
                }
                else
                {
                    switch (mf.Style)
                    {
                        case MusicStyles.IndianClassical:
                            var requiredTags = new string[] { "Raga" };
                            if (requiredTags.Any(t => !isTagPresent(t)))
                            {
                                result = false;
                                if (requiredTags.Count() > 1)
                                {
                                    log.Error($"{mf.File} none of {(string.Join(", ", requiredTags))} found");
                                }
                                else
                                {
                                    log.Error($"{mf.File} tag {requiredTags.First()} not found");
                                }
                            }
                            break;
                        case MusicStyles.WesternClassical:
                            // optional tags: these do  not need to be preent but we report if they are not
                            var optionalTags = new string[] { "Composer", "Composition", "Conductor", "Orchestra" };
                            var missingOptional = optionalTags.Where(t => isTagPresent(t) == false).ToList();
                            missingOptional.ForEach(t => log.Trace($"{mf.File} optional tag {t} not present"));
                            // alternate tags: atleast one of these neeeds to be present
                            var alternateTags = new string[] { "Performer", "Album Artist", "AlbumArtist", "AlbumArtists" };
                            if (!alternateTags.Any(t => isTagPresent(t)))
                            {
                                result = false;
                                log.Error($"{mf.File} none of {(string.Join(", ", alternateTags))} found");
                            }
                            break;
                    }
                }
            }
            return result;
        }
        public bool ValidateArtists()
        {
            var result = true;
            foreach (var artist in db.Artists)
            {
                var styleCount = artist.ArtistStyles.Count();
                var r = styleCount > 0;
                if (!r)
                {
                    log.Warning($"Artist {artist.Name} [A-{artist.Id}] has no artiststyle entries");
                    if (result == true)
                    {
                        result = false;
                    }
                }
                r = artist.Works.Count() > 0 || artist.Compositions.Count() > 0;
                if (!r)
                {
                    log.Warning($"Artist {artist.Name} [A-{artist.Id}] has neither works nor compositions");
                    if (result == true)
                    {
                        result = false;
                    }
                }
            }
            log.Information("ValidateArtists() completed");
            return result;
        }
        public bool ValidateWorks()
        {
            var result = true;
            foreach (var work in db.Works)
            {
                var r = work.Tracks.Count() > 0;
                if (!r)
                {
                    log.Warning($"Work {work.Name} [W-{work.Id}] has no tracks");
                    if (result == true)
                    {
                        result = false;
                    }
                }
            }
            log.Information("ValidateWorks() completed");
            return result;
        }
        public bool ValidateTracks()
        {
            //var result = true;
            int errorCount = 0;
            foreach (var track in db.Tracks)
            {
                if (track.MusicFiles.Count() == 0)
                {
                    log.Warning($"Track {track.Title} [T-{track.Id}] has no music files");
                    ++errorCount;
                }
                if (track.MusicFiles.All(x => x.IsGenerated))
                {
                    log.Warning($"Track {track.Title} [T-{track.Id}] has only generated music files");
                    ++errorCount;
                }
            }
            log.Information("ValidateTracks() completed");
            return errorCount == 0;
        }
        public bool ValidateCompositions()
        {
            var result = true;
            foreach (var composition in db.Compositions)
            {
                var performanceCount = composition.Performances.Count();
                var r = performanceCount > 0;
                if (!r)
                {
                    log.Warning($"Composition {composition.Name} [C-{composition.Id}] has no performances");
                    if (result == true)
                    {
                        result = false;
                    }
                }
            }
            log.Information("ValidateCompositions() completed");
            return result;
        }
        public bool Validate()
        {
            var list = new List<bool>() {
                ValidateArtists(),
                ValidateWorks(),
                ValidateTracks(),
                ValidateCompositions(),
                ValidatePerformances()
            };
            return list.All(x => x == true);
        }
        public bool ValidatePerformances()
        {
            var result = true;
            foreach (var performance in db.Performances)
            {
                var movementCount = performance.Movements.Count();
                var r = movementCount > 0;
                if (!r)
                {
                    //                    log.Warning($"{performance.Composition.Artist.Name} [A-{performance.Composition.Artist.Id}], \"{performance.Composition.Name}\" [C-{performance.Composition.Id}] performed by \"{performance.GetAllPerformersCSV()}\" [P-{performance.Id}] has no movements");
                    log.Warning($"{performance.ToLogIdentity()} performed by \"{performance.GetAllPerformersCSV()}\" has no movements");
                    if (result == true)
                    {
                        result = false;
                    }
                }
                if (movementCount > 0)
                {
                    var workCount = performance.Movements.Select(x => x.Work).Distinct().Count();
                    r = workCount == 1;
                    if (!r)
                    {
                        //                        log.Warning($"{performance.Composition.Artist.Name} [A-{performance.Composition.Artist.Id}], \"{performance.Composition.Name} [C-{performance.Composition.Id}] movements\" in performance by {performance.GetAllPerformersCSV()} [P-{performance.Id}] have a work count of {workCount}");
                        log.Warning($"{performance.ToLogIdentity()} in performance by {performance.GetAllPerformersCSV()} has a work count of {workCount}");
                        if (result == true)
                        {
                            result = false;
                        }
                    }
                }
            }
            log.Information("ValidatePerformances() completed");
            return result;
        }
        private string ToIdent()
        {
            return taskItem.ToString();
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
            var args = EntityChangedEventArgs.Create(EntityChangeType.Delete, nameof(MusicFile), musicFile.Id,
                $"{ToIdent()} {musicFile.ToIdent()} {musicFile.File} removed");
            entityObserver.RaiseEntityChanged(args);
        }
        private void RemoveEntity(Composition composition)
        {
            db.Compositions.Remove(composition);
            var args = EntityChangedEventArgs.Create(EntityChangeType.Delete, nameof(Composition), composition.Id,
                $"{ToIdent()} {composition.ToIdent()} {composition.Name} removed", composition.ArtistId);
            entityObserver.RaiseEntityChanged(args);
        }
        private void RemoveEntity(Performance performance)
        {
            if (performance is null)
            {
                throw new ArgumentNullException(nameof(performance));
            }
            EntityChangedEventArgs args = null;
            switch (performance.StyleId)
            {
                case MusicStyles.WesternClassical:
                    args = EntityChangedEventArgs.Create(EntityChangeType.Delete, nameof(Performance), performance.Id,
                        $"{ToIdent()} {performance.ToIdent()} removed");
                    break;

                case MusicStyles.IndianClassical:
                    var idlist = db.RagaPerformances.Where(x => x.Performance == performance).Select(x => x.ArtistId).ToArray();
                    args = EntityChangedEventArgs.Create(EntityChangeType.Delete, nameof(Performance), performance.Id,
                        $"{ToIdent()} {performance.ToIdent()} removed", idlist);
                    break;
            }

            db.Performances.Remove(performance);
            entityObserver.RaiseEntityChanged(args);
        }
        private void RemoveEntity(Track track)
        {
            if (track is null)
            {
                throw new ArgumentNullException(nameof(track));
            }
            db.Tracks.Remove(track);
            var args = EntityChangedEventArgs.Create(EntityChangeType.Delete, nameof(Track), track.Id,
                $"{ToIdent()} {track.ToIdent()} {track.Title} removed", track.Work.ArtistWorkList.Select(x => x.ArtistId).ToArray());
            entityObserver.RaiseEntityChanged(args);
        }
        private void RemoveEntity(Work work)
        {
            if (work is null)
            {
                throw new ArgumentNullException(nameof(work));
            }
            var artists = work.ArtistWorkList.Select(x => x.Artist);
            db.Works.Remove(work);
            var style = work.StyleId;
            foreach (var artist in artists)
            {
                if (artist.ArtistWorkList.Where(x => x.WorkId != work.Id)
                    .Select(x => x.Work).Where(w => w.StyleId == style)
                    .Count() == 0)
                {
                    var x = db.ArtistStyles.Single(x => x.StyleId == style && x.ArtistId == artist.Id);
                    db.ArtistStyles.Remove(x);
                }
            }
            var args = EntityChangedEventArgs.Create(EntityChangeType.Delete, nameof(Work), work.Id,
                $"{ToIdent()} {work.ToIdent()} {work.Name} removed", artists.Select(x => x.Id).ToArray());
            entityObserver.RaiseEntityChanged(args);
        }
        private void RemoveEntity(Raga raga)
        {
            if (raga is null)
            {
                throw new ArgumentNullException(nameof(raga));
            }
            db.Ragas.Remove(raga); // i don't bubble delete because ragas have no parents
            var args = EntityChangedEventArgs.Create(EntityChangeType.Delete, nameof(Raga), raga.Id,
                $"{ToIdent()} {raga.ToIdent()} {raga.Name} deleted");
            entityObserver.RaiseEntityChanged(args);
        }
        private void RemoveEntity(Artist artist)
        {
            var aslist = db.ArtistStyles.Where(x => x.Artist == artist);
            db.ArtistStyles.RemoveRange(aslist);
            db.Artists.Remove(artist);
            var args = EntityChangedEventArgs.Create(EntityChangeType.Delete, nameof(Artist), artist.Id,
                $"{ToIdent()} {artist.ToIdent()} {artist.Name} removed");
            entityObserver.RaiseEntityChanged(args);
        }
        /// <summary>
        /// removes any entity that refers to music file if that entity is now 'empty'
        /// currently checks the Track property
        /// </summary>
        /// <param name="musicFile"></param>
        private void RemoveReferencingEntitiesIfRequired(MusicFile musicFile)
        {
            if (musicFile is null)
            {
                throw new ArgumentNullException(nameof(musicFile));
            }

            var track = musicFile.Track;
            if (track != null)
            {
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
            else
            {
                log.Warning($"{musicFile.File} has no track");
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
            foreach (var artist in artists)
            {
                var aw = artist.ArtistWorkList.Single(x => x.Work == work);
                artist.ArtistWorkList.Remove(aw);

                if (artist.ArtistWorkList.Count() == 0 && artist.Compositions.Count() == 0)
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
                    composition.CompositionPerformances.Remove(cp);
                    performance.CompositionPerformances.Remove(cp);
                    db.CompositionPerformances.Remove(cp);
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
                    Debug.Assert(rplist.Select(x => x.Raga).Distinct().Count() == 1);
                    var raga = rplist.Select(x => x.Raga).Distinct().Single();
                    db.RagaPerformances.RemoveRange(rplist);
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
            if (composer.Compositions.Count() == 0 && composer.Works.Count() == 0)
            {
                BubbleDelete(composer);
            }
        }
        private void DeleteRelatedIdTags(MusicFile musicFile)
        {
            var tags = musicFile.IdTags.ToArray();
            db.IdTags.RemoveRange(tags);
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
                if (playlist.Items.Count() == 0)
                {
                    db.Playlists.Remove(playlist);
                }
            }
        }
        private void DeletedRelatedPerformancePerformers(Performance performance)
        {
            var pplist = performance.PerformancePerformers.ToArray();
            var performers = pplist.Select(x => x.Performer);
            db.PerformancePerformers.RemoveRange(pplist);
            performance.PerformancePerformers.Clear();
            foreach (var performer in performers)
            {
                if (db.PerformancePerformers.Where(x => x.Performer == performer).Count() == 0)
                {
                    db.Performers.Remove(performer);
                }
            }
        }
    }
    public partial class EntityHelperOld
    {
        private readonly ILogger log;
        private readonly MusicDb db;
        private readonly DeleteContext context;
        private readonly EntityObserver entityObserver;
        // helps delete
        // MusicFile, Track, Work, Artist
        // Performance, Performer, Composition
        // Raga
        //public EntityHelper(MusicDb db, EntityObserver entityObserver)
        //{
        //    this.log = ApplicationLoggerFactory.CreateLogger<EntityHelper>();
        //    this.db = db;
        //    this.entityObserver = entityObserver;
        //}
        public EntityHelperOld(MusicDb db, DeleteContext ctx)
        {
            this.log = ApplicationLoggerFactory.CreateLogger<EntityHelperOld>();
            this.db = db;
            this.context = ctx;
        }
        public EntityHelperOld(MusicDb db, TaskItem taskItem) : this(db, new DeleteContext(taskItem))
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
            //Debug.Assert(composition.Performances.Count() == 0);
            if (composition.Performances.Count() == 0)
            {
                log.Warning($"{composition.Artist.ToIdent()} {composition.Artist.Name} {composition.ToIdent()} {composition.Name} has zero performances!");
            }
            context.SetModifiedArtistId(composition.ArtistId);
            db.Compositions.Remove(composition);
            log.Information($"{ToIdent()} {composition.ToIdent()} {composition.Name} removed");
        }
        private void RemoveEntity(Performance performance)
        {
            if (performance is null)
            {
                throw new ArgumentNullException(nameof(performance));
            }
            //Debug.Assert(performance.Movements.Count() == 0);
            //if (performance.Movements.Count() == 0)
            //{
            //    log.Warning($"{performance.ToIdent()} {performance.GetAllPerformersCSV()} has zero movements!");
            //}
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
            if (track is null)
            {
                throw new ArgumentNullException(nameof(track));
            }
            //Debug.Assert(track.MusicFiles.Count() == 0);
            //if (track.MusicFiles.Count() == 0)
            //{
            //    log.Warning($"{track.Work.ToIdent()} {track.Work.Name} {track.ToIdent()} {track.Title} has zero music files!");
            //}
            context.SetModifiedArtistId(track.Work.ArtistWorkList.Select(x => x.ArtistId).ToArray());
            db.Tracks.Remove(track);
            log.Information($"{ToIdent()} {track.ToIdent()} {track.Title} removed");
        }
        private void RemoveEntity(Work work)
        {
            if (work is null)
            {
                throw new ArgumentNullException(nameof(work));
            }
            //Debug.Assert(work.Tracks.Count() == 0);
            if (work.Tracks.Count() == 0)
            {
                log.Warning($"{work.ToIdent()} {work.Name} has zero tracks!");
            }
            var artists = work.ArtistWorkList.Select(x => x.Artist);
            context.SetModifiedArtistId(artists.Select(x => x.Id).ToArray());
            db.Works.Remove(work);
            var style = work.StyleId;
            foreach (var artist in artists)
            {
                if (artist.ArtistWorkList.Where(x => x.WorkId != work.Id)
                    .Select(x => x.Work).Where(w => w.StyleId == style)
                    .Count() == 0)
                {
                    var x = db.ArtistStyles.Single(x => x.StyleId == style && x.ArtistId == artist.Id);
                    db.ArtistStyles.Remove(x);
                    log.Information($"{artist.ToIdent()} {artist.Name} removed from {style}");
                }
            }
            log.Information($"{ToIdent()} {work.ToIdent()} {work.Name} removed");
        }
        private void RemoveEntity(Raga raga)
        {
            if (raga is null)
            {
                throw new ArgumentNullException(nameof(raga));
            }
            //Debug.Assert(db.RagaPerformances.Where(x => x.Raga == raga).Count() == 0);
            if (db.RagaPerformances.Where(x => x.Raga == raga).Count() == 0)
            {
                log.Warning($"{raga.ToIdent()} {raga.Name} has zero ragaperformances!");
            }
            db.Ragas.Remove(raga); // i don't bubble delete becuase ragas have no parents
            log.Information($"{ToIdent()} {raga.ToIdent()} {raga.Name} deleted");
        }
        private void RemoveEntity(Artist artist)
        {
            //Debug.Assert(artist.Works.Count() == 0);
            if (artist.Works.Count() == 0)
            {
                log.Warning($"{artist.ToIdent()} {artist.Name} has zero works!");
            }
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
            if (musicFile is null)
            {
                throw new ArgumentNullException(nameof(musicFile));
            }

            var track = musicFile.Track;
            if (track != null)
            {
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
            else
            {
                log.Warning($"{musicFile.File} has no track");
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

                if (artist.ArtistWorkList.Count() == 0 && artist.Compositions.Count() == 0)
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
                    Debug.Assert(rplist.Select(x => x.Raga).Distinct().Count() == 1);
                    var raga = rplist.Select(x => x.Raga).Distinct().Single();
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
            if (composer.Compositions.Count() == 0 && composer.Works.Count() == 0)
            {
                BubbleDelete(composer);
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