using Fastnet.Music.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fastnet.Music.Data
{
    public class Artist : INameParsing
    {
        public long Id { get; set; }
        public Guid UID { get; set; }
        [MaxLength(128)]
        public string Name { get; set; }
        [MaxLength(16)]
        public string CompressedName { get; set; }
        public ArtistType Type { get; set; }
        [MaxLength(128)]
        public string OriginalName { get; set; }
        [Obsolete]
        [MaxLength(128)]
        public string MbidName { get; set; }
        [MaxLength(128)]
        public string IdTagName { get; set; }
        [MaxLength(128)]
        public string UserProvidedName { get; set; }
        public LibraryParsingStage ParsingStage { get; set; }
        //public long ImageChecksum { get; set; }
        //public byte[] ImageData { get; set; }
        //[MaxLength(64)]
        //public string ImageMimeType { get; set; }
        //public DateTimeOffset ImageDateTime { get; set; }
        //public bool HasDefaultImage { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public virtual Image Portrait { get; set; }
        public virtual ICollection<ArtistStyle> ArtistStyles { get; } = new HashSet<ArtistStyle>();
        public virtual ICollection<Work> Works { get; } = new HashSet<Work>();
        public virtual ICollection<Composition> Compositions { get; } = new HashSet<Composition>();
        [Timestamp]
        public byte[] Timestamp { get; set; }
        public override string ToString()
        {
            return $"{Name}: {Works.Count} works, {Compositions.Count} compositions";
        }
    }

}
