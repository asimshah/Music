using System;
using System.Collections.Generic;

namespace Fastnet.Music.Data
{
    public class Playlist : EntityBase // IIdentifier
    {
        public override long Id { get; set; }
        public PlaylistType Type { get; set; }
        public string Name { get; set; }
        //public string ModificationUid { get; set; } // some guid, changes with any kind of change to the playlist
        public DateTimeOffset LastModified { get; set; }
        public virtual ICollection<PlaylistItem> Items { get; } = new HashSet<PlaylistItem>();
    }
}
