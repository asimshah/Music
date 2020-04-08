﻿using System.Collections.Generic;

namespace Fastnet.Music.Data
{
    public class Playlist : IIdentifier
    {
        public long Id { get; set; }
        public PlaylistType Type { get; set; }
        public string Name { get; set; }
        public string ModificationUid { get; set; } // some guid, changes with any kind of change to the playlist
        public virtual ICollection<PlaylistItem> Items { get; } = new HashSet<PlaylistItem>();
    }
}
