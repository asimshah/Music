using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    /// <summary>
    /// Set of MusicFiles from which catalog entities can be written
    /// (the set may be all the music files in a single opus folder, or a subset (for example for a composition)
    /// </summary>
    public abstract class BaseMusicSet //: IMusicSet
    {

        public string Name => GetName();
        public ITEOBase WorkTEO { get; protected set; }
        protected MusicDb MusicDb { get; private set; }
        protected MusicStyles MusicStyle { get; private set; }
        protected MusicOptions MusicOptions { get; private set; }
        //protected MusicFile FirstFile { get; private set; }
        protected IEnumerable<MusicFile> MusicFiles { get; private set; }
        protected OpusType OpusType { get; private set; }
        protected readonly ILogger log;
        protected readonly TaskItem taskItem;
        protected readonly int year;
        protected List<MetaPerformer> artistPerformers;
        protected List<MetaPerformer> otherPerformers;
        //private readonly bool generated;
        /// <summary>
        /// for use by generic method to create instances
        /// </summary>
        internal BaseMusicSet()
        {

        }
        /// <summary>
        /// create a a music set for the given music files in the given music style
        /// </summary>
        /// <param name="db"></param>
        /// <param name="musicOptions"></param>
        /// <param name="musicStyle"></param>
        /// <param name="musicFiles"></param>
        /// <param name="taskItem"></param>
        public BaseMusicSet(MusicDb db, MusicOptions musicOptions, MusicStyles musicStyle, IEnumerable<MusicFile> musicFiles, TaskItem taskItem)
        {
            Debug.Assert(db != null);
            Debug.Assert(musicFiles.Count() > 0);
            this.log = ApplicationLoggerFactory.CreateLogger(this.GetType());
            this.MusicDb = db;
            this.MusicOptions = musicOptions;
            this.MusicStyle = musicStyle;
            this.MusicFiles = musicFiles;
            var opusTypes = MusicFiles.Select(x => x.OpusType).Distinct();
            Debug.Assert(opusTypes.Count() == 1);
            this.OpusType = opusTypes.First();
            this.year = musicFiles.Select(f => f.GetYear() ?? 0).Max();
            this.taskItem = taskItem;

            var allPerformers = MusicFiles.GetAllPerformers(musicOptions);
            PartitionPerformers(allPerformers);
        }
        protected virtual void PartitionPerformers(IEnumerable<MetaPerformer> allPerformers)
        {
            artistPerformers = allPerformers.Where(p => p.Type == PerformerType.Artist).ToList();
            otherPerformers = allPerformers.Where(p => p.Type != PerformerType.Artist).ToList();
        }
        protected abstract string GetName();

        public abstract Task<BaseCatalogueResult> CatalogueAsync();
        /// <summary>
        /// looks for an artist in the database but does not create one
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected Artist FindArtist(string name)
        {
            // **Outstanding ** should this be looking for an artist with matching music style?
            MusicDb.Artists.Load();
            try
            {
                return MusicDb.Artists.Local.SingleOrDefault(a => a.Name.IsEqualIgnoreAccentsAndCase(name));
            }
            catch (Exception xe)
            {
                log.Error($"{xe.Message}");
                throw;
            }
        }
        protected async Task<IEnumerable<Artist>> GetArtistsAsync(IEnumerable<MetaPerformer> artistPerformers)
        {
            var list = new List<Artist>();
            foreach(var ap in artistPerformers)
            {
                var artist = await GetArtistAsync(ap);
                list.Add(artist);
            }
            return list;
        }
        protected virtual async Task<Artist> GetArtistAsync(MetaPerformer performer)
        {
            Debug.Assert(MusicDb != null);
            Artist artist = FindArtist(performer.Name);
            if (artist == null)
            {
                artist = await CreateNewArtist(performer.Name);
            }
            if (artist.Type != ArtistType.Various)
            {
                var portrait = artist.GetPortraitFile(MusicOptions);
                if (portrait != null)
                {
                    artist.Portrait = await portrait.GetImage();
                }
            }
            artist.LastModified = DateTimeOffset.Now;
            return artist;
        }
        private async Task<Artist> CreateNewArtist(string name)
        {
            Artist artist = new Artist
            {
                UID = Guid.NewGuid(),
                Name = name,
                AlphamericName = name.ToAlphaNumerics(),
                Type = ArtistType.Artist,
                OriginalName = name,
            };
            artist.ArtistStyles.Add(new ArtistStyle { Artist = artist, StyleId = MusicStyle });
            log.Debug($"{taskItem} new artist instance for {name}, {artist.Id}");
            if (this is BaseAlbumSet && OpusType == OpusType.Collection)
            {
                artist.Type = ArtistType.Various;
            }
            await MusicDb.Artists.AddAsync(artist);
            await MusicDb.SaveChangesAsync();
            return artist;
        }
        [Obsolete("use GetWork() in BaseAlbumSet")]
        protected Work GetWork(Artist artist, string name, int year)
        {
            Debug.Assert(MusicDb != null);
            try
            {
                var work = artist.Works.SingleOrDefault(w => w.Name.IsEqualIgnoreAccentsAndCase(name));
                if (work == null)
                {
                    work = new Work
                    {
                        StyleId = this.MusicStyle,
                        //Artist = artist,
                        Name = name,
                        AlphamericName = name.ToAlphaNumerics(),
                        Type = OpusType,
                        OriginalName = name,
                        Mood = string.Empty,
                        PartName = string.Empty,
                        PartNumber = 0,
                        UID = Guid.NewGuid(),
                        LastModified = DateTimeOffset.Now,
                        Year = year
                    };
                    //artist.Works.Add(work);
                    MusicDb.AddWork(artist, work);
                }

                return work;
            }
            catch (Exception xe)
            {
                log.Error($"{xe.Message}");
                throw;
            }
        }
        //protected (CatalogueStatus status, IEnumerable<Track> tracks) CatalogueTracks(Artist artist, Work album)
        //{
        //    Debug.Assert(MusicDb != null);
        //    var tracks = new List<Track>();
        //    var result = CatalogueStatus.Success;
        //    if (!generated || MusicOptions.AllowOutOfDateGeneratedFiles || album.LastModified < FirstFile.FileLastWriteTimeUtc)
        //    {
        //        var filesByPart = MusicFiles.GroupBy(x => x.PartNumber);
        //        int count = 0;
        //        foreach (var group in filesByPart.OrderBy(x => x.Key))
        //        {
        //            var files = group.Select(x => x).ToArray();
        //            //foreach(var mf in files)
        //            for (int i = 0; i < files.Count(); ++i)
        //            {
        //                var mf = files[i];
        //                var track = GetTrack(artist, album, mf, i, count);
        //                tracks.Add(track);
        //            }
        //            count += files.Count();
        //        }
        //    }
        //    else
        //    {
        //        // generated files are older than the album record
        //        // and may be out of date
        //        var path = Path.Combine(FirstFile.DiskRoot, FirstFile.StylePath, FirstFile.OpusPath); ////**NB** should this be FirstFile.GetRootPath()
        //        log.Warning($"{MusicFiles.Count()} files in {path} are generated and possibly out-of-date - files not catalogued");
        //        result = CatalogueStatus.GeneratedFilesOutOfDate;
        //        if (album.Tracks.Count() == 0)
        //        {
        //            //artist.Works.Remove(album);
        //            MusicDb.RemoveWork(artist, album);
        //            if (artist.Works.Count() == 0)
        //            {
        //                MusicDb.Artists.Remove(artist);
        //            }
        //        }
        //    }
        //    return (result, tracks);
        //}
        //private Track GetTrack(Artist artist, Work album, MusicFile mf, int index, int totalInPreviousParts)
        //{
        //    try
        //    {
        //        var basePath = mf.GetRootPath();
        //        var relativePath = Path.GetRelativePath(basePath, mf.File);
        //        MusicFileTEO fileteo = WorkTEO?.TrackList.Single(t => string.Compare(Path.Combine(WorkTEO.PathToMusicFiles, t.File), mf.File, true) == 0);
        //        var trackNumber = fileteo?.TrackNumberTag.GetValue<int>() ?? mf.GetTagIntValue("TrackNumber") ?? 0;
        //        if (trackNumber != (index + totalInPreviousParts + 1))
        //        {
        //            log.Debug($"{artist.Name}, {album.Name}, track number changing from {trackNumber} to {index + totalInPreviousParts + 1}");
        //            trackNumber = index + totalInPreviousParts + 1;
        //        }
        //        string title = fileteo?.TitleTag.GetValue<string>() ?? mf.GetTagValue<string>("Title");
        //        if (title.Contains(':'))
        //        {
        //            if (this is PopularMusicAlbumSet)
        //            {
        //                var temp = this as PopularMusicAlbumSet;
        //                var workName = this is WesternClassicalAlbumSet ? mf.GetWorkName() : temp.AlbumName;
        //                var parts = title.Split(':', StringSplitOptions.RemoveEmptyEntries);
        //                if (parts[0].IsEqualIgnoreAccentsAndCase(workName))
        //                {
        //                    title = string.Join(":", parts.Skip(1)).Trim();
        //                    log.Debug($"Title {string.Join(":", parts)} changed to {title}");
        //                }
        //            }
        //        }
        //        var track = this is WesternClassicalAlbumSet ?
        //            album.Tracks.SingleOrDefault(x => x.Title.IsEqualIgnoreAccentsAndCase(title) && x.CompositionName.IsEqualIgnoreAccentsAndCase(mf.GetWorkName()))
        //            : album.Tracks.SingleOrDefault(x => x.Title.IsEqualIgnoreAccentsAndCase(title));
        //        if (track == null)
        //        {
        //            if (!mf.IsGenerated)
        //            {
        //                track = new Track
        //                {
        //                    Work = album,
        //                    CompositionName = this is WesternClassicalAlbumSet ? mf.GetWorkName() : string.Empty,
        //                    OriginalTitle = mf.Title,
        //                    UID = Guid.NewGuid(),
        //                };
        //                album.Tracks.Add(track);
        //            }
        //            else
        //            {
        //                //log.Error($"{artist.Name}, {album.Name} initial track cannot be created using a generated music file: {mf.File}");
        //                throw new Exception($"{artist.Name}, {album.Name} initial track cannot be created using a generated music file: {mf.File}");
        //            }
        //        }
        //        track.Title = title;
        //        track.AlphamericTitle = title.ToAlphaNumerics();
        //        track.Number = trackNumber;
        //        track.LastModified = DateTimeOffset.Now;

        //        var tmpMf = track.MusicFiles.SingleOrDefault(x => x.File.IsEqualIgnoreAccentsAndCase(mf.File));
        //        if (tmpMf == null)
        //        {
        //            // this music file is not in a track
        //            track.MusicFiles.Add(mf);
        //            mf.Track = track; //NB: also set the track becuase I need this later even though SaveChanges will not have been called
        //        }
        //        else
        //        {
        //            //Debug.Assert(tmpMf == mf);
        //        }
        //        mf.ParsingStage = MusicFileParsingStage.Catalogued;
        //        mf.LastCataloguedAt = DateTimeOffset.Now;
        //        return track;
        //    }
        //    catch (Exception xe)
        //    {
        //        log.Error($"[{taskItem}] {xe.Message}");
        //        throw;
        //    }
        //}

    }
}
