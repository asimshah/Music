﻿using Fastnet.Music.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Fastnet.Music.Data
{
    // * Important *
    // cannot use a private constructor because of lazy loading
    public class ArtistWork : IManyToManyIdentifier
    {
        public long ArtistId { get; set; }
        public virtual Artist Artist { get; set; }
        public long WorkId { get; set; }
        public virtual Work Work { get; set; }
        public string ToIdent()
        {
            return IManyToManyIdentifier.ToIdent(Artist, Work);
        }
        public override string ToString()
        {
            return $"[A-{ArtistId}+W-{WorkId}]";
        }
    }
    public class Artist : IIdentifier, ILengthConstants, INameParsing
    {
        public long Id { get; set; }
        public Guid UID { get; set; }
        [MaxLength(ILengthConstants.MaxArtistNameLength)]
        public string Name { get; set; }
        [MaxLength(ILengthConstants.MaxCompressedNameLength)]
        public string CompressedName { get; set; }
        public ArtistType Type { get; set; }
        [MaxLength(ILengthConstants.MaxArtistNameLength)]
        public string OriginalName { get; set; }
        [Obsolete]
        [MaxLength(128)]
        public string MbidName { get; set; }
        [Obsolete]
        [MaxLength(128)]
        public string IdTagName { get; set; }
        [MaxLength(ILengthConstants.MaxArtistNameLength)]
        public string UserProvidedName { get; set; }
        public LibraryParsingStage ParsingStage { get; set; }

        public DateTimeOffset LastModified { get; set; }
        public virtual Image Portrait { get; set; }
        public virtual ICollection<ArtistStyle> ArtistStyles { get; } = new HashSet<ArtistStyle>();
        public virtual ICollection<ArtistWork> ArtistWorkList { get; } = new HashSet<ArtistWork>();
        //public virtual ICollection<Work> Works { get; } = new HashSet<Work>();
        public virtual IEnumerable<Work> Works => ArtistWorkList.Select(aw => aw.Work);
        public virtual ICollection<Composition> Compositions { get; } = new HashSet<Composition>();
        [Timestamp]
        public byte[] Timestamp { get; set; }
        public override string ToString()
        {
            return $"{Name}: {Works.Count()} works, {Compositions.Count} compositions";
        }
    }

}
