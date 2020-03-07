using Fastnet.Music.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Fastnet.Music.Data
{
    public class MusicFile
    {
        public long Id { get; set; }
        public MusicStyles Style { get; set; }
        [MaxLength(256)]
        public string DiskRoot { get; set; }
        [MaxLength(256)]
        public string StylePath { get; set; }
        [MaxLength(512)]
        public string OpusPath { get; set; }
        [MaxLength(2048)]
        public string File { get; set; }
        public long FileLength { get; set; }
        public DateTimeOffset FileLastWriteTimeUtc { get; set; }
        [StringLength(36)]
        public string Uid { get; set; } // from Guid.NewGuid().ToString()
        //
        [MaxLength(128)]
        public string Musician { get; set; }
        public ArtistType MusicianType { get; set; }
        public OpusType OpusType { get; set; }
        public bool IsMultiPart { get; set; }
        [MaxLength(128)]
        public string OpusName { get; set; } // album, composition, etc
        [MaxLength(64)]
        public string PartName { get; set; }
        public int PartNumber { get; set; }
        [MaxLength(256)]
        public string Title { get => Path.GetFileNameWithoutExtension(File); }
        public EncodingType Encoding { get; set; }
        public ChannelMode Mode { get; set; }
        public double? Duration { get; set; } // total milliseconds
        public int? BitsPerSample { get; set; }
        public int? SampleRate { get; set; } // divide by 1000 for Hz
        public int? MinimumBitRate { get; set; }
        public int? MaximumBitRate { get; set; }
        public double? AverageBitRate { get; set; }
        public bool IsGenerated { get; set; }
        public bool IsFaulty { get; set; }
        public DateTimeOffset LastPlayedAt { get; set; }
        public DateTimeOffset LastCataloguedAt { get; set; }
        //
        public MusicFileParsingStage ParsingStage { get; set; }
        //[Obsolete]
        //public bool DeletedInSource { get; set; }
        //[Obsolete]
        //public bool IsModified { get; set; }

        public virtual ICollection<IdTag> IdTags { get; } = new HashSet<IdTag>();
        public string Mood { get; set; }
        public long? TrackId { get; set; }
        public virtual Track Track { get; set; }
        //
        [Timestamp]
        public byte[] Timestamp { get; set; }
        public override string ToString()
        {
            return this.File;
        }
    }
}
