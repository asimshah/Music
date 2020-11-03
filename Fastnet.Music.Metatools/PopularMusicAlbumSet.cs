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
    public class HindiFilmsAlbumSet : BaseAlbumSet
    {
        public HindiFilmsAlbumSet(EntityHelper entityHelper, MusicOptions musicOptions, MusicStyles musicStyle, IEnumerable<MusicFile> musicFiles, TaskItem taskItem) : base(entityHelper, musicOptions, musicStyle, musicFiles, taskItem)
        {
            SetAlbumNameToOpusName();
        }

        public override Task<BaseCatalogueResult> CatalogueAsync()
        {
            return base.CatalogueAsync((cs, w) => new HindiFilmsCatalogueResult(this, cs, w));
        }
        public override string ToString()
        {
            return $"{Name}::{MusicFiles.Count()} files";
        }
    }
    public class PopularMusicAlbumSet : BaseAlbumSet
    {
        /// <summary>
        /// internal use by WesternClassicalAlbumSet only
        /// </summary>
        /// <param name="entityHelper"></param>
        /// <param name="musicOptions"></param>
        /// <param name="musicFiles"></param>
        /// <param name="taskItem"></param>
        internal PopularMusicAlbumSet(EntityHelper entityHelper, MusicOptions musicOptions, 
            IEnumerable<MusicFile> musicFiles, TaskItem taskItem) : base(entityHelper, musicOptions, MusicStyles.Popular, musicFiles, taskItem)
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
        public override string ToString()
        {
            return $"{Name}::{MusicFiles.Count()} files";
        }
    }
}
