using System;

namespace Fastnet.Music.Core
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [Obsolete]
    public enum SelectionCriteria
    {
        [Obsolete]
        SampleRateAbove44100,
        AllFlac
    }
    [Obsolete]
    public enum ConversionQuality
    {
        VBR0, // the highest VBR setting, ~245kbps
        VBR4, // good quality but smaller files, ~165kbps
        CBR320, // the best possible for mp3, constant high bit rate throughout        
    }
    /// <summary>
    /// Defines source and target disk roots for a conversion
    /// e.g. D:\Music\flac to D:\Music\flac-vbr
    /// This allows for the same artist/album in different source roots to be converted
    /// and kept separate.
    /// </summary>
    [Obsolete]
    public class RootMap
    {
        /// <summary>
        /// a disk source such as D:\Music\flac
        /// </summary>
        public string SourceRoot { get; set; }
        /// <summary>
        /// a disk target such as D:\Music\flac-vbr
        /// </summary>
        public string TargetRoot { get; set; }
    }
    [Obsolete]
    public class MusicConversion
    {
        public bool Disable { get; set; }
        public SelectionCriteria Criterion { get; set; }
        [Obsolete]
        public ConversionQuality Quality { get; set; }
        /// <summary>
        /// Must match one of the MusicOption sources (even if disabled)
        /// (because the music file path is replicated below the root)
        /// Make sure that different qualities use different target roots
        /// </summary>
        public string TargetRoot { get; set; }
        public RootMap[] Map { get; set; }

    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
