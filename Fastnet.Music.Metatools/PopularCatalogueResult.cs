using Fastnet.Music.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    public class HindiFilmsCatalogueResult : PopularCatalogueResult
    {
        public HindiFilmsCatalogueResult(BaseMusicSet set, CatalogueStatus status, Work work) : base(set, status, work)
        {
        }
    }
    public class PopularCatalogueResult : BaseCatalogueResult
    {
        public int WorkTrackCount { get; set; }
        public string TrackContent { get; set; } = String.Empty;

        public string WorkDescr { get; set; } = String.Empty;
        public string WorkName { get; set; } = String.Empty;
        public PopularCatalogueResult(BaseMusicSet set, CatalogueStatus status, Work work) : base(set, status)
        {
            WorkDescr = work.ToIdent();
            WorkName = work?.Name ?? string.Empty;
            WorkTrackCount = work.Tracks?.Count() ?? 0;
            TrackContent = GetTrackContent(work);
            SetArtists(work.Artists);
        }
        private string GetTrackContent(Work album)
        {
            var strings = new List<string>();
            var musicFiles = album.Tracks.First().MusicFiles;
            foreach (var mf in musicFiles)
            {
                var text = $"{mf.Encoding}{(mf.IsGenerated ? " (generated)" : string.Empty)}";
                strings.Add(text);
            }
            return $"({string.Join(", ", strings)})";
        }
        public override string ToString()
        {
            return $"{ArtistDescr} {ArtistNames}, {WorkDescr} {WorkName}, {WorkTrackCount} tracks {TrackContent}";
        }
    }
}
