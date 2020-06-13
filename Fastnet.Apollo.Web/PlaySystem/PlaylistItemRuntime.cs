using System.Collections.Generic;

namespace Fastnet.Apollo.Web
{
    public class PlaylistItemRuntime
    {
        //public long Id { get; set; }
        public PlaylistRuntimeItemType Type { get; set; }
        public PlaylistPosition Position { get; set; }
        //public string Title { get; set; }
        public IEnumerable<string> Titles { get; set; }
        //public int Sequence { get; set; }
        public bool NotPlayableOnCurrentDevice { get; set; }
        //public long ItemId { get; set; }
        public long MusicFileId { get; set; }
        public string AudioProperties { get; set; }
        public int SampleRate { get; set; }
        public double TotalTime { get; set; }
        public string FormattedTotalTime { get; set; }
        public string CoverArtUrl { get; set; }
        //public Track Track { get; set; }
        //public MusicFile MusicFile { get; set; }
        //public Work Work { get; set; }
        public IEnumerable<PlaylistItemRuntime> SubItems { get; set; }
        public override string ToString()
        {
            return $"{Position}: {Type}, {string.Join("|", Titles)}";
        }
    }
}
