using Fastnet.Music.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fastnet.Music.Data
{
    public interface ILengthConstants
    {
        const int MaxWorkNameLength = 256;
    }
    public class Work : ILengthConstants, INameParsing, IPlayable
    {
        
        public long Id { get; set; }
        public Guid UID { get; set; }
        [Required, MaxLength(ILengthConstants.MaxWorkNameLength)]
        public string Name { get; set; }
        [MaxLength(ILengthConstants.MaxWorkNameLength)]
        public string AlphamericName { get; set; }
        [MaxLength(16)]
        public string CompressedName { get; set; }
        public bool IsMultiPart { get; set; }
        public int PartNumber { get; set; }
        [MaxLength(64)]
        public string PartName { get; set; }
        public OpusType Type { get; set; }
        [Required, MaxLength(ILengthConstants.MaxWorkNameLength)]
        public string OriginalName { get; set; }
        [Obsolete]
        [MaxLength(128)]
        public string MbidName { get; set; }
        [MaxLength(128)]
        public string IdTagName { get; set; }
        [MaxLength(128)]
        public string UserProvidedName { get; set; }
        [MaxLength(ILengthConstants.MaxWorkNameLength)]
        public string DisambiguationName { get; set; } // used when copies are made for external systems, e.g for alexa, for phones
        public LibraryParsingStage ParsingStage { get; set; }
        public MusicStyles StyleId { get; set; }
        public long ArtistId { get; set; }
        public virtual Artist Artist { get; set; }

        public string Mood { get; set; }
        public int Year { get; set; }
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
