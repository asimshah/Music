using Fastnet.Music.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public class PlaylistInformation //: IMessageParcel
    {
        //public string PlaylistModificationUid { get; set; }

        public string Title { get; set; }
        //public int PlaylistSequence { get; set; }
        public long PlaylistId { get; set; }
        public long PlaylistItemId { get; set; }
        public long PlaylistSubItemId { get; set; }
        public string CoverArtUrl { get; set; } // only used for cover art in the audio-player angular component
        public long MusicFileId { get; set; } // normally the original music file but may also be a substitute (e.g. due MaxSampleRate)
        public string AudioProperties { get; set; }
    }
}
