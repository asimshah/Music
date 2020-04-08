using Fastnet.Music.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;

namespace Fastnet.Music.Data
{
    public class Work : IIdentifier, INameParsing, IPlayable
    {        
        public long Id { get; set; }
        public Guid UID { get; set; }
        [Required, MaxLength(ILengthConstants.MaxWorkNameLength)]
        public string Name { get; set; }
        [MaxLength(ILengthConstants.MaxWorkNameLength)]
        public string AlphamericName { get; set; }
        [MaxLength(ILengthConstants.MaxCompressedNameLength)]
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
        public virtual IEnumerable<Artist> Artists => ArtistWorkList.Select(aw => aw.Artist);
        public virtual ICollection<ArtistWork> ArtistWorkList { get; } = new HashSet<ArtistWork>();
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
            return $"{Name} {Tracks.Count} tracks";
        }
        private Artist GetArtist()
        {
            if(ArtistWorkList.Count > 1)
            {
                Debug.WriteLine($"Warning:: GetArtist() but there are multiple ({ArtistWorkList.Count}) artists");
            }
            return ArtistWorkList.Select(aw => aw.Artist).FirstOrDefault();
        }
        public string GetArtistNames()
        {
            return string.Join(", ", Artists.Select(x => x.Name));
        }
        public string GetArtistIds()
        {
            return string.Join(", ", Artists.Select(x => x.Id.ToString()));
        }
    }
}
