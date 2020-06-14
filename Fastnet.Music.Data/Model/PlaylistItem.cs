using Fastnet.Music.Core;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastnet.Music.Data
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// This is a work or a track that is in a playlist
    /// </summary>
    public class PlaylistItem : EntityBase
    {
        public override long Id { get; set; }
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
        public override string ToString()
        {
            return $"{ToIdent()} {Type}, ({Sequence}) {Title}";
        }

    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
