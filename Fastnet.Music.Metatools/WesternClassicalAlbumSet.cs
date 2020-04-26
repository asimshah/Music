using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    /// <summary>
    /// Western classical music creates an album just like Popular music but this is not visible to the normal UI
    /// </summary>
    public class WesternClassicalAlbumSet : BaseAlbumSet 
    {
        internal WesternClassicalAlbumSet()
        {

        }
        public WesternClassicalAlbumSet(MusicDb db, MusicOptions musicOptions, IEnumerable<MusicFile> musicFiles, TaskItem taskItem)
            : base(db, musicOptions, MusicStyles.WesternClassical, musicFiles, taskItem)
        {
            if(artistPerformers.Count() == 0)
            {
                var composers = otherPerformers.Where(x => x.Type == PerformerType.Composer);
                if(composers.Count() > 0)
                {
                    var first = composers.First();
                    otherPerformers.Remove(first);
                    first.Reset(PerformerType.Artist);
                    artistPerformers.Add(first);
                    log.Warning($"{taskItem} no artist found, using {first.Name}");
                }
            }

        }
        public override Task<BaseCatalogueResult> CatalogueAsync()
        {
            return base.CatalogueAsync((cs, w) => new WesternClassicalAlbumCatalogueResult(this, cs, w));
        }
        protected override Track CreateTrackIfRequired(Work album, MusicFile mf, string title)
        {
            var alphamericTitle = title.ToAlphaNumerics();
            var track = album.Tracks.SingleOrDefault(x => x.Title == alphamericTitle && x.CompositionName.IsEqualIgnoreAccentsAndCase(mf.GetWorkName()));
            if (track == null)
            {
                track = new Track
                {
                    Work = album,
                    CompositionName = mf.GetWorkName(),
                    OriginalTitle = mf.Title,
                    UID = Guid.NewGuid(),
                };
                album.Tracks.Add(track);
            }
            return track;
        }




    }
}
