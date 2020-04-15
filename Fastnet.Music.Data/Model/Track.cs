using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fastnet.Music.Data
{
    public class Track : EntityBase, INameParsing
    {
        public override long Id { get; set; }
        public int Number { get; set; }
        /// <summary>
        /// Valid if this track is a movement in some performance
        /// </summary>
        public int MovementNumber { get; set; }
        [Required, MaxLength(256)]
        public string Title { get; set; }
        [MaxLength(256)]
        public string AlphamericTitle { get; set; }
        [MaxLength(ILengthConstants.MaxCompressedNameLength)]
        public string CompressedName { get; set; }
        [Required, MaxLength(256)]
        public string CompositionName { get; set; } // only if WesternClassical
        [Required, MaxLength(256)]
        public string OriginalTitle { get; set; }
        [Obsolete]
        public string MbidName { get; set; }
        [MaxLength(256)]
        public string IdTagName { get; set; }
        public string UserProvidedName { get; set; }
        public Guid UID { get; set; }
        public LibraryParsingStage NumberParsingStage { get; set; }
        public LibraryParsingStage ParsingStage { get; set; }
        public virtual ICollection<MusicFile> MusicFiles { get; } = new HashSet<MusicFile>();
        public long WorkId { get; set; }
        public virtual Work Work { get; set; }
        public long? PerformanceId { get; set; }
        public virtual Performance Performance { get; set; }
        public DateTimeOffset LastModified { get; set; }
        [Timestamp]
        public byte[] Timestamp { get; set; }
        public override string ToString()
        {
            return $"{Number}: {Title}";
        }
    }
}
