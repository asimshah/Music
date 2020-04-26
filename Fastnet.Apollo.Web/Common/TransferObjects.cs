using Fastnet.Core;
using Fastnet.Music.Core;
using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Apollo.Web
{
    public class ParametersDTO
    {
        public string Version {get; set;}
        public string BrowserKey { get; set; }
        public string AppName { get; set; }
        public bool IsMobile { get; set; }
        public bool IsIpad { get; set; }
        public string Browser { get; set; }
        public string ClientIPAddress { get; set; }
        public int CompactLayoutWidth { get; set; }
        public StyleDTO[] Styles { get; set; }
    }
    public class StyleDTO
    {
        public MusicStyles Id { get; set; }
        public bool Enabled { get; set; }
        public string DisplayName { get; set; }
    }
    public class ArtistDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Lastname { get; set; }
        public string DisplayName { get; set; }
        public ArtistType ArtistType { get; set; }
        public IEnumerable<MusicStyles> Styles { get; set; }
        public int WorkCount { get; set; }
        public int SinglesCount { get; set; }
        public int CompositionCount { get; set; }
        public int RagaCount { get; set; }
        public int PerformanceCount { get; set; }
        public MetadataQuality Quality { get; set; }
        public string ImageUrl { get; set; }
    }
    public class PerformanceDTO
    {
        public long Id { get; set; }
        public string Performers { get; set; }
        public string DisplayName { get; set; }
        public int Year { get; set; }
        public string AlbumName { get; set; }
        public string AlbumCoverArt { get; set; }
        public int MovementCount { get; set; }
        public TrackDTO[] Movements { get; set; }
        public string FormattedDuration { get; set; }
    }
    public class CompositionDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public PerformanceDTO[] Performances { get; set; }
    }
    public class WorkDTO
    {
        public long Id { get; set; }
        public long ArtistId => ArtistIdList.First(); // temporary til Ui can cope with multiple artists for a work
        public IEnumerable<long> ArtistIdList { get; set; }
        public OpusType OpusType { get; set; }
        public string Name { get; set; }
        public int Year { get; set; }
        public int TrackCount { get; set; }
        public string CoverArtUrl { get; set; }
        public TrackDTO[] Tracks { get; set; }
        public string FormattedDuration { get; set; }
    }
    public class TrackDTO
    {
        public long Id { get; set; }
        public long WorkId { get; set; }
        public long ArtistId { get; set; }
        public int Number { get; set; }
        public string Title { get; set; }
        public string DisplayName { get; set; }
        public int MusicFileCount { get; set; }
        public MetadataQuality NumberQuality { get; set; }
        public MetadataQuality TitleQuality { get; set; }
        public IEnumerable<MusicFileDTO> MusicFiles { get; set; }
    }
    public class MusicFileDTO
    {
        public long Id { get; set; }
        public EncodingType Encoding { get; set; }
        public int? BitRate { get; set; }
        public int? BitsPerSample { get; set; }
        public double? SampleRate { get; set; }
        public double? Duration { get; set; }
        public string FormattedDuration { get; set; }
        public string AudioProperties { get; set; }
        public int Rank { get; set; }
        public bool IsFaulty { get; set; }
    }
    public class TrackDetail
    {
        public long Id { get; set; }
        public int Number { get; set; }
        public string Title { get; set; }
        public int MusicFileCount { get; set; }
        public IEnumerable<MusicFileDetail> MusicFiles { get; set; }
    }
    public class PerformanceDetails : IOpusDetails
    {
        public long Id { get; set; }
        public string OpusName { get; set; }
        public IEnumerable<TrackDetail> TrackDetails { get; set; }
        public string ArtistName { get; set; }
        public string CompressedArtistName { get; set; }
        public string CompressedOpusName { get; set; }
        public string CompressedPerformanceName { get; set; }
    }
    public class WorkDetails : IOpusDetails
    {
        public long Id { get; set; }
        public string OpusName { get; set; }
        public IEnumerable<TrackDetail> TrackDetails { get; set; }
        public string ArtistName { get; set; }
        public string CompressedArtistName { get; set; }
        public string CompressedOpusName { get; set; }
    }
    public class MusicFileDetail
    {
        public long Id { get; set; }
        public bool IsFaulty { get; set; }
        public string File { get; set; }
        public long FileLength { get; set; }
        public string FileLastWriteTimeString { get; set; }
    }
    public class PlaylistItemDTO
    {
        public long Id { get; set; }
        public bool NotPlayableOnCurrentDevice { get; set; }
        public PlaylistRuntimeItemType Type { get; set; }
        public PlaylistPosition Position { get; set; }
        public int Sequence { get; set; }
        //public string Title { get; set; }
        public IEnumerable<string> Titles { get; set; }
        public string AudioProperties { get; set; }
        public int SampleRate { get; set; }
        public string CoverArtUrl { get; set; }
        public long PlaylistSubItemId { get; set; } //??
        public double TotalTime { get; set; }
        public string FormattedTotalTime { get; set; }
        public bool IsSubitem { get; set; }
        public IEnumerable<PlaylistItemDTO> SubItems { get; set; }
    }
    public class DeviceStatusDTO
    {
        public string Key { get; set; }
        public PlaylistPosition PlaylistPosition { get; set; }
        public PlayerStates State { get; set; }
        public float Volume { get; set; }
        public double CurrentTime { get; set; }
        public double TotalTime { get; set; }
        public double RemainingTime { get; set; }
        public string FormattedCurrentTime { get; set; }
        public string FormattedTotalTime { get; set; }
        public string FormattedRemainingTime { get; set; }
        public int CommandSequence { get; set; }
    }
    public class PlaylistUpdateDTO
    {
        public string DeviceKey { get; set; }
        public string DisplayName { get; set; }
        public IEnumerable<PlaylistItemDTO> Items { get; set; }
    }










}
