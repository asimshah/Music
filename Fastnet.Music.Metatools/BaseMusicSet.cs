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
        //public ITEOBase WorkTEO { get; protected set; }
        protected MusicDb MusicDb { get; private set; }
        protected MusicStyles MusicStyle { get; private set; }
        protected MusicOptions MusicOptions { get; private set; }
        protected IEnumerable<MusicFile> MusicFiles { get; private set; }
        protected OpusType OpusType { get; private set; }
        protected readonly ILogger log;
        protected readonly TaskItem taskItem;
        protected readonly int year;
        protected List<MetaPerformer> artistPerformers;
        protected List<MetaPerformer> otherPerformers;
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
        //[Obsolete("use GetWork() in BaseAlbumSet")]
        //protected Work GetWork(Artist artist, string name, int year)
        //{
        //    Debug.Assert(MusicDb != null);
        //    try
        //    {
        //        var work = artist.Works.SingleOrDefault(w => w.Name.IsEqualIgnoreAccentsAndCase(name));
        //        if (work == null)
        //        {
        //            work = new Work
        //            {
        //                StyleId = this.MusicStyle,
        //                //Artist = artist,
        //                Name = name,
        //                AlphamericName = name.ToAlphaNumerics(),
        //                Type = OpusType,
        //                OriginalName = name,
        //                Mood = string.Empty,
        //                PartName = string.Empty,
        //                PartNumber = 0,
        //                UID = Guid.NewGuid(),
        //                LastModified = DateTimeOffset.Now,
        //                Year = year
        //            };
        //            MusicDb.AddWork(artist, work);
        //        }
        //        return work;
        //    }
        //    catch (Exception xe)
        //    {
        //        log.Error($"{xe.Message}");
        //        throw;
        //    }
        //}
    }
}
