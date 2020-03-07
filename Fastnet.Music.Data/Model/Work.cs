using Fastnet.Music.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fastnet.Music.Data
{
    public class Work : INameParsing, IPlayable
    {
        public long Id { get; set; }
        public Guid UID { get; set; }
        [Required, MaxLength(128)]
        public string Name { get; set; }
        [MaxLength(128)]
        public string AlphamericName { get; set; }
        [MaxLength(16)]
        public string CompressedName { get; set; }
        public bool IsMultiPart { get; set; }
        public int PartNumber { get; set; }
        [MaxLength(64)]
        public string PartName { get; set; }
        public OpusType Type { get; set; }
        [Required, MaxLength(128)]
        public string OriginalName { get; set; }
        [Obsolete]
        [MaxLength(128)]
        public string MbidName { get; set; }
        [MaxLength(128)]
        public string IdTagName { get; set; }
        [MaxLength(128)]
        public string UserProvidedName { get; set; }
        [MaxLength(128)]
        public string DisambiguationName { get; set; } // used when copies are made for external systems, e.g for alexa, for phones
        public LibraryParsingStage ParsingStage { get; set; }
        public MusicStyles StyleId { get; set; }
        //[Obsolete("get rid of Style table")]
        //public virtual Style Style { get; set; }
        public long ArtistId { get; set; }
        public virtual Artist Artist { get; set; }
        //public bool HasDefaultCover { get; set; }
        public string Mood { get; set; }
        public int Year { get; set; }
        //public long CoverChecksum { get; set; }
        //public byte[] CoverData { get; set; }
        //[MaxLength(64)]
        //public string CoverMimeType { get; set; }
        //public DateTimeOffset CoverDateTime { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public virtual Image Cover { get; set; }
        public virtual ICollection<Track> Tracks { get; } = new HashSet<Track>();
        [Timestamp]
        public byte[] Timestamp { get; set; }

        IEnumerable<Track> IPlayable.Tracks => Tracks;
        public override string ToString()
        {
            return $"{Artist.Name}: {Name} {Tracks.Count} tracks";
        }
    }
}
