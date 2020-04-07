using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    public class PopularAlbumTEO : TEOBase// TEOBase<PopularMusicFileTEO
    {
        public PopularAlbumTEO(MusicOptions musicOptions) : base(musicOptions)
        {
        }

        public override void SaveChanges(MusicDb db, Work work)
        {
            base.SaveChanges(db, work);
            foreach (var track in TrackList)
            {
                var t = work.Tracks.First(x => track.TrackId == x.Id);
                if (track.TrackNumberTag.GetValue<int>() != t.Number)
                {
                    log.Information($"{work.GetArtistNames()}, \"{work.Name}\": movement number changed from {t.Number} to {track.TrackNumberTag.GetValue<int>()}");
                }
                if (track.TitleTag.GetValue<string>() != t.Title)
                {
                    log.Information($"{work.GetArtistNames()}, \"{work.Name}\": title changed changed from {t.Title} to {track.TitleTag.GetValue<string>()}");
                }
            }
        }
        public override async Task Load(Work work)
        {
            await base.Load(work);
            TrackList = TrackList.OrderBy(x => x.TrackNumberTag.GetValue<int>()).ToArray();
        }

        protected override MusicFileTEO CreateMusicFileTeo(MusicFile mf)
        {
            //return new PopularMusicFileTEO(musicOptions) { MusicFile = mf };
            return new MusicFileTEO(musicOptions) { MusicFile = mf };
        }
    }    
}
