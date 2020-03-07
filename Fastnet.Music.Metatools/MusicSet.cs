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
    /// Set of MusicFiles from which catalog entrties can be written
    /// (the set may be all the music files in a single opus folder, or a subset (for example for a composition)
    /// </summary>
    //public abstract class MusicSet<MT> : IMusicSet where MT : MusicTags
    public abstract class MusicSet : IMusicSet
    {
        protected abstract Task LoadMusicTags();
        public string Name => GetName();
        public ITEOBase WorkTEO { get; protected set; }
        protected MusicDb MusicDb { get; private set; }
        protected MusicStyles MusicStyle { get; private set; }
        protected readonly ILogger log;
        protected MusicOptions MusicOptions { get; private set; }
        protected MusicFile FirstFile { get; private set; }
        protected IEnumerable<MusicFile> MusicFiles { get; private set; }
        protected OpusType OpusType { get; private set; }
        private readonly TaskItem taskItem;
        private readonly bool generated;
        /// <summary>
        /// create a a music set for the given music files in the given music style
        /// </summary>
        /// <param name="musicOptions"></param>
        /// <param name="musicStyle"></param>
        /// <param name="musicFiles"></param>
        public MusicSet(MusicDb db, MusicOptions musicOptions, MusicStyles musicStyle, IEnumerable<MusicFile> musicFiles, TaskItem taskItem)
        {
            Debug.Assert(musicFiles.Count() > 0);
            this.log = ApplicationLoggerFactory.CreateLogger(this.GetType());
            this.MusicDb = db;
            this.MusicOptions = musicOptions;
            this.MusicStyle = musicStyle;
            this.MusicFiles = musicFiles;
            this.taskItem = taskItem;
            this.FirstFile = musicFiles.First();
            this.OpusType = FirstFile.OpusType;
            this.generated = FirstFile.IsGenerated;
        }
        protected abstract string GetName();
        public abstract Task<CatalogueResult> CatalogueAsync();
        protected async Task<string> ReadMusicTagJson()
        {
            var diskPath = Path.Combine(FirstFile.DiskRoot, FirstFile.StylePath, FirstFile.OpusPath);
            var filename = Path.Combine(diskPath, ITEOBase.TagFile);
            if (File.Exists(filename))
            {
                return await File.ReadAllTextAsync(filename);
            }
            return null;
        }
        private Artist FindArtist(string name)
        {
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
        protected async Task<Artist> GetArtistAsync(string name)
        {
            Debug.Assert(MusicDb != null);
            name = MusicOptions.ReplaceAlias(name);
            Artist artist = FindArtist(name);
            if (artist == null)
            {
                artist = new Artist
                {
                    UID = Guid.NewGuid(),
                    Name = name,
                    Type = ArtistType.Artist,
                    OriginalName = name,
                };
                artist.ArtistStyles.Add(new ArtistStyle { Artist = artist, StyleId = MusicStyle });
                log.Debug($"{taskItem} new artist instance for {name}, {artist.Id}");
                if (this is PopularMusicAlbumSet && OpusType == OpusType.Collection)
                {
                    artist.Type = ArtistType.Various;
                }
                await MusicDb.Artists.AddAsync(artist);
                await MusicDb.SaveChangesAsync();
            }
            if (artist.Type != ArtistType.Various)
            {
               // var artistFoldername = OpusType == OpusType.Collection ? null : FirstFile.OpusPath.Split('\\')[0];// Path.GetFileName(Path.GetDirectoryName(FirstFile.File));
                var portrait = artist.GetPortraitFile(MusicOptions);
                if (portrait != null)
                {
                    artist.Portrait = await portrait.GetImage();
                }
            }
            artist.LastModified = DateTimeOffset.Now;
            return artist;
        }
        protected async Task<Work> GetWorkAsync(Artist artist, string name, int year)
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
                        Artist = artist,
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
                    artist.Works.Add(work);
                }
                work.LastModified = DateTimeOffset.Now;
                //var cover = GetOpusCoverFile(artist);
                var cover = work.GetMostRecentOpusCoverFile(MusicOptions);
                if (cover != null)
                {
                    work.Cover = await cover.GetImage();
                }
                return work;
            }
            catch (Exception xe)
            {
                log.Error($"{xe.Message}");
                throw;
            }
        }
        protected (CatalogueStatus status, IEnumerable<Track> tracks) CatalogueTracks(Artist artist, Work album)
        {
            Debug.Assert(MusicDb != null);
            var tracks = new List<Track>();
            var result = CatalogueStatus.Success;
            if (!generated || MusicOptions.AllowOutOfDateGeneratedFiles || album.LastModified < FirstFile.FileLastWriteTimeUtc)
            {
                var filesByPart = MusicFiles.GroupBy(x => x.PartNumber);
                int count = 0;
                foreach (var group in filesByPart.OrderBy(x => x.Key))
                {
                    var files = group.Select(x => x).ToArray();
                    //foreach(var mf in files)
                    for (int i = 0; i < files.Count(); ++i)
                    {
                        var mf = files[i];
                        var track = GetTrack(artist, album, mf, i, count);
                        tracks.Add(track);
                    }
                    count += files.Count();
                }
                //foreach (var mf in MusicFiles)
                //{
                //    var track = GetTrack(artist, album, mf);
                //    tracks.Add(track);
                //}
            }
            else
            {
                // generated files are older than the album record
                // and may be out of date
                var path = Path.Combine(FirstFile.DiskRoot, FirstFile.StylePath, FirstFile.OpusPath);
                log.Warning($"{MusicFiles.Count()} files in {path} are generated and possibly out-of-date - files not catalogued");
                result = CatalogueStatus.GeneratedFilesOutOfDate;
                if (album.Tracks.Count() == 0)
                {
                    artist.Works.Remove(album);
                    if (artist.Works.Count() == 0)
                    {
                        MusicDb.Artists.Remove(artist);
                    }
                }
            }
            return (result, tracks);
        }
        private Track GetTrack(Artist artist, Work album, MusicFile mf, int index, int totalInPreviousParts)
        {
            try
            {
                var basePath = Path.Combine(mf.DiskRoot, mf.StylePath, mf.OpusPath);
                if (mf.OpusType == OpusType.Collection)
                {
                    basePath = Path.Combine(mf.DiskRoot, mf.StylePath, mf.Musician, mf.OpusPath);
                }
                var relativePath = Path.GetRelativePath(basePath, mf.File);
                MusicFileTEO fileteo = WorkTEO?.TrackList.Single(t => string.Compare(Path.Combine(WorkTEO.PathToMusicFiles, t.File), mf.File, true) == 0);
                var trackNumber = fileteo?.TrackNumberTag.GetValue<int>() ?? mf.GetTagIntValue("TrackNumber") ?? 0;
                if (trackNumber != (index + totalInPreviousParts + 1))
                {
                    log.Debug($"{artist.Name}, {album.Name}, track number changing from {trackNumber} to {index + totalInPreviousParts + 1}");
                    trackNumber = index + totalInPreviousParts + 1;
                }
                string title = fileteo?.TitleTag.GetValue<string>() ?? mf.GetTagValue<string>("Title");
                if (title.Contains(':'))
                {
                    if (this is PopularMusicAlbumSet)
                    {
                        var temp = this as PopularMusicAlbumSet;
                        var workName = this is WesternClassicalAlbumSet ? mf.GetWorkName() : temp.AlbumName;
                        var parts = title.Split(':', StringSplitOptions.RemoveEmptyEntries);
                        if (parts[0].IsEqualIgnoreAccentsAndCase(workName))
                        {
                            title = string.Join(":", parts.Skip(1)).Trim();
                            log.Debug($"Title {string.Join(":", parts)} changed to {title}");
                        }
                    }
                }
                var track = this is WesternClassicalAlbumSet ?
                    album.Tracks.SingleOrDefault(x => x.Title.IsEqualIgnoreAccentsAndCase(title) && x.CompositionName.IsEqualIgnoreAccentsAndCase(mf.GetWorkName()))
                    : album.Tracks.SingleOrDefault(x => x.Title.IsEqualIgnoreAccentsAndCase(title));
                if (track == null)
                {
                    if (!mf.IsGenerated)
                    {
                        track = new Track
                        {
                            Work = album,
                            CompositionName = this is WesternClassicalAlbumSet ? mf.GetWorkName() : string.Empty,
                            OriginalTitle = mf.Title,
                            UID = Guid.NewGuid(),
                        };
                        album.Tracks.Add(track);
                    }
                    else
                    {
                        //log.Error($"{artist.Name}, {album.Name} initial track cannot be created using a generated music file: {mf.File}");
                        throw new Exception($"{artist.Name}, {album.Name} initial track cannot be created using a generated music file: {mf.File}");
                    }
                }
                track.Title = title;
                track.AlphamericTitle = title.ToAlphaNumerics();
                track.Number = trackNumber;
                track.LastModified = DateTimeOffset.Now;

                var tmpMf = track.MusicFiles.SingleOrDefault(x => x.File.IsEqualIgnoreAccentsAndCase(mf.File));
                if (tmpMf == null)
                {
                    // this music file is not in a track
                    track.MusicFiles.Add(mf);
                    mf.Track = track; //NB: also set the track becuase I need this later even though SaveChanges will not have been called
                }
                else
                {
                    Debug.Assert(tmpMf == mf);
                }
                mf.ParsingStage = MusicFileParsingStage.Catalogued;
                mf.LastCataloguedAt = DateTimeOffset.Now;
                return track;
            }
            catch (Exception xe)
            {
                log.Error($"[{taskItem}] {xe.Message}");
                throw;
            }
        }
        //private string GetOpusCoverFile(Artist artist)
        //{
        //    //return MusicStyle.GetMostRecentOpusCoverFile(MusicOptions, artist.Name,
        //    //    OpusType == OpusType.Collection ? Path.Combine("Collections", FirstFile.OpusPath) : FirstFile.OpusPath);
        //    return MusicStyle.GetMostRecentOpusCoverFile(MusicOptions, artist.Type, artist.Name, FirstFile.OpusPath);
        //}
    }
}
