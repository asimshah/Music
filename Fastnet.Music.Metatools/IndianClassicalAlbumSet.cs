using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System.Collections.Generic;

namespace Fastnet.Music.Metatools
{
    public class IndianClassicalAlbumSet : PopularMusicAlbumSet
    {
        internal IndianClassicalAlbumSet(MusicDb db, MusicOptions musicOptions,  IEnumerable<MusicFile> musicFiles, TaskItem taskItem)
            : base(db, musicOptions, MusicStyles.IndianClassical, musicFiles, taskItem)
        {
            //this.ArtistName = OpusType == OpusType.Collection ? "Various Artists" : MusicOptions.ReplaceAlias(FirstFile.Musician);
            this.ArtistName = OpusType == OpusType.Collection ?
                "Various Artists"
                : MusicOptions.ReplaceAlias(FirstFile.GetArtistName() ?? FirstFile.Musician);
            this.AlbumName = FirstFile.GetAlbumName() ?? FirstFile.OpusName;

        }

    }
}
