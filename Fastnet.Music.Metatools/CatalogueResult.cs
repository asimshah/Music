using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    public class CatalogueResult
    {
        public IMusicSet MusicSet { get; set; }
        public Type MusicSetType /*{ get; set; }*/ => MusicSet.GetType();
        public CatalogueStatus Status { get; set; }
       // private Artist Artist { get; set; }
        public long ArtistId { get; set; }
        public ArtistType ArtistType { get; set; }
        public string ArtistDescr { get; set; } = String.Empty;
        public string ArtistName { get; set; } = String.Empty;
        //private Work Work { get; set; }
        public string WorkDescr { get; set; } = String.Empty;
        public string WorkName { get; set; } = String.Empty;
        //public IEnumerable<Track> Tracks { get; set; }
        //private Composition Composition { get; set; }
        public string CompositionDescr { get; set; } = String.Empty;
        public string CompositionName { get; set; } = String.Empty;
        //private Performance Performance { get; set; }
        public string PerformanceDescr { get; set; } = String.Empty;
        public string PerformersCSV { get; set; } = String.Empty;
        public int PerformanceMovementCount { get; set; }
        public int WorkTrackCount { get; set; }
        public string TrackContent { get; set; } = String.Empty;
        /// <summary>
        /// Possibly non null for PopularMusicAlbumSet or WesternClassicalAlbumSet
        /// (when resampling task is required)
        /// </summary>
        public TaskItem TaskItem { get; set; }
        private static CatalogueResult Create(IMusicSet set, CatalogueStatus status, Artist artist/*, Composition composition, Performance performance*/)
        {
            var cr = new CatalogueResult
            {
                MusicSet = set,
                Status = status,
            };
            cr.ArtistId = artist?.Id ?? 0;
            cr.ArtistType = artist?.Type ?? ArtistType.Artist;
            cr.ArtistDescr = $"[A-{cr.ArtistId}]";
            cr.ArtistName = artist?.Name ?? string.Empty;

            //var work = performance.Movements?.Select(m => m.Work).First();
            //cr.WorkTrackCount = work.Tracks?.Count() ?? 0;
            //cr.TrackContent = GetTrackContent(work);
            //cr.WorkDescr = $"[W-{work?.Id ?? 0}]";
            //cr.WorkName = $"[W-{work?.Name ?? string.Empty}]";

            //cr.CompositionDescr = $"[C-{composition?.Id ?? 0}]";
            //cr.CompositionName = composition?.Name ?? string.Empty;
            //cr.PerformanceDescr = $"[P-{performance?.Id ?? 0}]";
            //cr.PerformersCSV = performance.GetAllPerformersCSV();
            //cr.PerformanceMovementCount = performance.Movements?.Count ?? 0;
            return cr;
        }
        public static CatalogueResult Create(IMusicSet set, CatalogueStatus status, Performance performance)
        {
            var  cr = CatalogueResult.Create(set, status, performance.Composition.Artist/*, performance.Composition, performance*/);
            var work = performance.Movements?.Select(m => m.Work).First();
            var composition = performance.Composition;

            cr.WorkTrackCount = work.Tracks?.Count() ?? 0;
            cr.TrackContent = GetTrackContent(work);
            cr.WorkDescr = $"[W-{work?.Id ?? 0}]";
            cr.WorkName = $"[W-{work?.Name ?? string.Empty}]";

            cr.CompositionDescr = $"[C-{composition?.Id ?? 0}]";
            cr.CompositionName = composition?.Name ?? string.Empty;
            cr.PerformanceDescr = $"[P-{performance?.Id ?? 0}]";
            cr.PerformersCSV = performance.GetAllPerformersCSV();
            cr.PerformanceMovementCount = performance.Movements?.Count ?? 0;
            return cr;
        }
        public static CatalogueResult Create(IMusicSet set, CatalogueStatus status, Work work)
        {
            var cr = CatalogueResult.Create(set, status, work.Artists.First());
            cr.WorkTrackCount = work.Tracks?.Count() ?? 0;
            cr.TrackContent = GetTrackContent(work);
            cr.WorkDescr = $"[W-{work?.Id ?? 0}]";
            cr.WorkName = $"[W-{work?.Name ?? string.Empty}]";
            return cr;
        }
        private static string GetTrackContent(Work album)
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
    }
}
