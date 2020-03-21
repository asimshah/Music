using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    /// <summary>
    /// Western classical music creates an album just like Popular music but this is not visible to the normal UI
    /// </summary>
    public class WesternClassicalAlbumSet : PopularMusicAlbumSet
    {
        public WesternClassicalAlbumSet(MusicDb db, MusicOptions musicOptions, IEnumerable<MusicFile> musicFiles, TaskItem taskItem)
            : base(db, musicOptions, MusicStyles.WesternClassical, musicFiles, taskItem)
        {
            this.ArtistName = OpusType == OpusType.Collection ? "Various Composers" : MusicOptions.ReplaceAlias(FirstFile.Musician);
            this.ArtistName = OpusType == OpusType.Collection ?
                "Various Composers"
                : MusicOptions.ReplaceAlias(FirstFile.GetArtistName() ?? FirstFile.Musician);
            this.AlbumName = FirstFile.GetAlbumName() ?? FirstFile.OpusName;
            this.YearNumber = FirstFile.GetYear() ?? 0;
        }
    }
}
