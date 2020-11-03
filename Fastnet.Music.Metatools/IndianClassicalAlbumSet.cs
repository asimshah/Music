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
    public class IndianClassicalAlbumSet : BaseAlbumSet
    {
        static string[] honourifics = new string[] { "Pt.", "Pt", "Pandit", "Ustad", "Ustaad", "Shri", "Shrimati" };
        internal IndianClassicalAlbumSet(EntityHelper entityHelper, MusicOptions musicOptions,  IEnumerable<MusicFile> musicFiles, TaskItem taskItem)
            : base(entityHelper, musicOptions, MusicStyles.IndianClassical, musicFiles, taskItem)
        {
            SetAlbumNameToOpusName();
        }

        public override Task<BaseCatalogueResult> CatalogueAsync()
        {
            return base.CatalogueAsync((cs, w) => new IndianClassicalAlbumCatalogueResult(this, cs, w));
        }

        protected override Track SelectMatchingTrack(IEnumerable<Track> tracks, MusicFile mf)
        {
            return tracks.SingleOrDefault(t => t.MusicFiles.Any(x => x.GetRagaName().IsEqualIgnoreAccentsAndCase(mf.GetRagaName()))
                && t.MusicFiles.Any(x => x.GetAllPerformers(MusicOptions).Where(mp => mp.Type == PerformerType.Artist).Select(mp => mp.Name).ToCSV().ToLower()
                == mf.GetAllPerformers(MusicOptions).Where(mp => mp.Type == PerformerType.Artist).Select(mp => mp.Name).ToCSV().ToLower()));
        }

    }
}
