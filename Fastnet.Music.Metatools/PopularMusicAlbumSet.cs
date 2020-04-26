using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    public class PopularMusicAlbumSet : BaseAlbumSet
    {
        internal PopularMusicAlbumSet()
        {

        }
        /// <summary>
        /// internal use by WesternClassicalAlbumSet only
        /// </summary>
        /// <param name="db"></param>
        /// <param name="musicOptions"></param>
        /// <param name="musicFiles"></param>
        /// <param name="taskItem"></param>
        internal PopularMusicAlbumSet(MusicDb db, MusicOptions musicOptions, 
            IEnumerable<MusicFile> musicFiles, TaskItem taskItem) : base(db, musicOptions, MusicStyles.Popular, musicFiles, taskItem)
        {
            if(artistPerformers.Count() > 1)
            {
                var names = artistPerformers.Select(x => x.Name);
                var original = names.ToCSV();
                var cn = $"{string.Join(", ", names.Take(names.Count() - 1))} & {names.Last()}";
                cn = MusicOptions.ReplaceAlias(cn);
                artistPerformers.Clear();
                artistPerformers.Add(new MetaPerformer(PerformerType.Artist, cn));
                log.Warning($"{taskItem} multiple artist names combined: {original} => {cn}");
            }
        }
        public override Task<BaseCatalogueResult> CatalogueAsync()
        {
            return base.CatalogueAsync((cs, w) =>  new PopularCatalogueResult(this, cs, w));
        }
        //public override async Task<BaseCatalogueResult> CatalogueAsync()
        //{
        //    var album = await GetWork();
        //    var (result, tracks) = CatalogueTracks(album);// CatalogueTracks(artist, album);
        //    await UpdateAlbumCover(album);
        //    await InitiateResampling(album.UID.ToString());
        //    var cr = new PopularCatalogueResult(this, result, album/*, resamplingTask*/);
        //    return cr;
        //}
        public override string ToString()
        {
            return $"{Name}::{MusicFiles.Count()} files";
        }

        protected override Track CreateTrackIfRequired(Work album, MusicFile mf, string title)
        {
            var alphamericTitle = title.ToAlphaNumerics();
            var track = album.Tracks.SingleOrDefault(x => x.Title == alphamericTitle);
            if (track == null)
            {
                track = new Track
                {
                    Work = album,
                    CompositionName = string.Empty,
                    OriginalTitle = mf.Title,
                    UID = Guid.NewGuid(),
                };
                album.Tracks.Add(track);
            }
            return track;
        }
    }
}
