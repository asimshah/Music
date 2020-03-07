using Fastnet.Music.Core;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastnet.Music.Data
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// This is a work or a track that is in a playlist
    /// </summary>
    public class PlaylistItem
    {
        public long Id { get; set; }
        public PlaylistItemType Type { get; set; }
        public string Title { get; set; }
        public int Sequence { get; set; }
        public long ItemId { get; set; }
        public long MusicFileId { get; set; } // pk of a music file, only valid if Type == Track
        [NotMapped]
        public Track Track { get; set; }
        [NotMapped]
        public MusicFile MusicFile { get; set; }
        [NotMapped]
        public Work Work { get; set; }
        [NotMapped]
        public Performance Performance { get; set; }
        public long PlaylistId { get; set; }
        public virtual Playlist Playlist { get; set; }
        //public virtual ICollection<PlaylistSubItem> SubItems { get; } = new HashSet<PlaylistSubItem>(); // only if Type is Work
    }
    ///// <summary>
    ///// This is a track that is in a PlaylistItem of type Work
    ///// </summary>
    //public class PlaylistSubItem
    //{
    //    public long Id { get; set; }
    //    public string Title { get; set; }
    //    public int Sequence { get; set; }
    //    public long TrackId { get; set; } // pk  a track
    //    public long MusicFileId { get; set; } // pk of a the chosen music file (from those in the track)
    //    public bool CurrentlyPlaying { get; set; }
    //    public long PlaylistItemId { get; set; }
    //    public virtual PlaylistItem PlaylistItem { get; set; }
    //}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
