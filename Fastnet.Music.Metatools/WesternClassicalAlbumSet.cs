using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    /// <summary>
    /// Western classical music creates an album just like Popular music but this is not visible to the normal UI
    /// </summary>
    public class WesternClassicalAlbumSet : BaseAlbumSet 
    {
        //internal WesternClassicalAlbumSet()
        //{

        //}
        public WesternClassicalAlbumSet(EntityHelper entityHelper, MusicOptions musicOptions, IEnumerable<MusicFile> musicFiles, TaskItem taskItem)
            : base(entityHelper, musicOptions, MusicStyles.WesternClassical, musicFiles, taskItem)
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
                    log.Debug($"{taskItem} no artist found, using {first.Name}");
                }
            }
            var compositionNames = MusicFiles.Select(x => x.GetWorkName()).Distinct(StringComparer.CurrentCultureIgnoreCase);
            if(compositionNames.Any(cn => cn.IsEqualIgnoreAccentsAndCase(AlbumName)))
            {
                var temp = AlbumName;
                SetAlbumNameToOpusName();
                log.Warning($"{taskItem} album name {temp} matches a composition name [{compositionNames.ToCSV()}], changed to {AlbumName}");
            }
        }
        public override Task<BaseCatalogueResult> CatalogueAsync()
        {
            return base.CatalogueAsync((cs, w) => new WesternClassicalAlbumCatalogueResult(this, cs, w));
        }
        protected override Track SelectMatchingTrack(IEnumerable<Track> tracks, MusicFile mf)
        {
            //if there is an existing track with the same title and the same work name, i.e.composition
            //select it. Should the composer also be checked?
            return tracks.SingleOrDefault(t => t.MusicFiles.Any(x => x.GetWorkName().IsEqualIgnoreAccentsAndCase(mf.GetWorkName())));
        }
    }
}
