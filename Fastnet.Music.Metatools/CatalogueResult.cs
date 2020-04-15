using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    public abstract class CatalogueResultBase
    {
        private IMusicSet set;

        public IMusicSet MusicSet { get; private set; }
        public Type MusicSetType  => MusicSet.GetType();
        public CatalogueStatus Status { get; private set; }
        public string ArtistDescr { get; set; } = String.Empty;
        public string ArtistNames { get; set; } = String.Empty;
        public List<long> ArtistIdListForNotification { get; set; } = new List<long>();
        public CatalogueResultBase(IMusicSet set, CatalogueStatus status)
        {
            MusicSet = set;
            Status = status;
            //SetArtists(artists);
        }

        protected void SetArtists(IEnumerable<Artist> artists)
        {
            ArtistIdListForNotification.AddRange(artists.Where(a => a.Type != ArtistType.Various).Select(a => a.Id));
            ArtistDescr = string.Join(string.Empty, artists.Select(x => x.ToIdent())).Replace("][", ",");
            ArtistNames = artists.Select(x => x.Name).ToCSV();
        }
        protected void SetArtist(Artist artist)
        {
            if (artist.Type != ArtistType.Various)
            {
                ArtistIdListForNotification.Add(artist.Id);
            }
            ArtistDescr = artist.ToIdent();
        }
    }
    public class PopularCatalogueResult : CatalogueResultBase
    {
        [Obsolete("is this obsolete?")]
        public TaskItem ResampleTaskItem { get; set; }
        public int WorkTrackCount { get; set; }
        public string TrackContent { get; set; } = String.Empty;

        public string WorkDescr { get; set; } = String.Empty;
        public string WorkName { get; set; } = String.Empty;
        public PopularCatalogueResult(IMusicSet set, CatalogueStatus status, Work work, TaskItem resampleTask = null) : base(set, status)
        {
            ResampleTaskItem = resampleTask;
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
            return $"{ArtistDescr} {ArtistNames}, {WorkName} {WorkDescr}, {WorkTrackCount} tracks {TrackContent}";
        }
    }
    public class WesternClassicalAlbumCatalogueResult : PopularCatalogueResult
    {
        public WesternClassicalAlbumCatalogueResult(IMusicSet set, CatalogueStatus status, Work work, TaskItem resampleTask = null) : base(set, status, work, resampleTask)
        {
        }
    }
    public class IndianClassicalAlbumCatalogueResult : PopularCatalogueResult
    {
        public IndianClassicalAlbumCatalogueResult(IMusicSet set, CatalogueStatus status, Work work, TaskItem resampleTask = null) : base(set, status, work, resampleTask)
        {
        }
    }
    public class WesternClassicalCompositionCatalogueResult : CatalogueResultBase
    {
        public string CompositionDescr { get; set; } = String.Empty;
        public string CompositionName { get; set; } = String.Empty;
        public string PerformanceDescr { get; set; } = String.Empty;
        public string PerformersCSV { get; set; } = String.Empty;
        public int PerformanceMovementCount { get; set; }
        public WesternClassicalCompositionCatalogueResult(IMusicSet set, CatalogueStatus status, Performance performance) : base(set, status)
        {
            var composition = performance.Composition;
            CompositionDescr = composition.ToIdent();
            CompositionName = composition?.Name ?? string.Empty;
            PerformanceDescr = performance.ToIdent();
            PerformersCSV = performance.GetAllPerformersCSV();
            PerformanceMovementCount = performance.Movements?.Count ?? 0;
            SetArtist(composition.Artist);
        }
        public override string ToString()
        {
            if (PerformanceMovementCount == 0)
            {
                return $"{CompositionDescr} {CompositionName}, {PerformanceDescr} \"{PerformersCSV}\" has no movements";
            }
            else
            {
                return $"{ArtistDescr} {ArtistNames}, {CompositionName} {CompositionDescr}, {PerformanceMovementCount} movements, {PerformanceDescr} \"{PerformersCSV}\"";
            }
        }
    }
    public class IndianClassicalRagaCatalogueResult : CatalogueResultBase
    {
        public string RagaDescr { get; }
        public string RagaName { get;  } = string.Empty;
        public string PerformanceDescr { get;  } = string.Empty;
        public string PerformersCSV { get; set; } = string.Empty;
        public int PerformanceMovementCount { get; set; }
        public IndianClassicalRagaCatalogueResult(IMusicSet set, CatalogueStatus status, IEnumerable<Artist> artists, Raga raga, Performance performance) : base(set, status)
        {
            RagaDescr = raga.ToIdent();
            RagaName = raga.Name;
            PerformanceDescr = performance.ToIdent();
            PerformersCSV = performance.GetAllPerformersCSV();
            PerformanceMovementCount = performance.Movements?.Count ?? 0;
            SetArtists(artists);
        }
        public override string ToString()
        {
            if (PerformanceMovementCount == 0)
            {
                return $"{RagaDescr} {RagaName}, {PerformanceDescr} \"{PerformersCSV}\" has no movements";
            }
            else
            {
                return $"{ArtistDescr} {ArtistNames}, {RagaDescr} {RagaName}, {PerformanceMovementCount} movements, {PerformanceDescr} \"{PerformersCSV}\"";
            }
        }

    }
    //public class CatalogueResult : CatalogueResultBase
    //{
    //    public long ArtistId { get; set; }
    //    public ArtistType ArtistType { get; set; }
    //    public string ArtistDescr { get; set; } = String.Empty;
    //    public string ArtistName { get; set; } = String.Empty;
    //    public string WorkDescr { get; set; } = String.Empty;
    //    public string WorkName { get; set; } = String.Empty;
    //    public string CompositionDescr { get; set; } = String.Empty;
    //    public string CompositionName { get; set; } = String.Empty;
    //    public string PerformanceDescr { get; set; } = String.Empty;
    //    public string PerformersCSV { get; set; } = String.Empty;
    //    public int PerformanceMovementCount { get; set; }
    //    public int WorkTrackCount { get; set; }
    //    public string TrackContent { get; set; } = String.Empty;
    //    /// <summary>
    //    /// Possibly non null for PopularMusicAlbumSet or WesternClassicalAlbumSet
    //    /// (when resampling task is required)
    //    /// </summary>
    //    public TaskItem TaskItem { get; set; }
    //    private static CatalogueResult Create(IMusicSet set, CatalogueStatus status, Artist artist/*, Composition composition, Performance performance*/)
    //    {
    //        var cr = new CatalogueResult
    //        {
    //            MusicSet = set,
    //            Status = status,
    //        };
    //        cr.ArtistId = artist?.Id ?? 0;
    //        cr.ArtistType = artist?.Type ?? ArtistType.Artist;
    //        cr.ArtistDescr = $"[A-{cr.ArtistId}]";
    //        cr.ArtistName = artist?.Name ?? string.Empty;

    //        return cr;
    //    }
    //    public static CatalogueResult Create(IMusicSet set, CatalogueStatus status, Performance performance)
    //    {
    //        var  cr = CatalogueResult.Create(set, status, performance.Composition.Artist/*, performance.Composition, performance*/);
    //        var work = performance.Movements?.Select(m => m.Work).First();
    //        var composition = performance.Composition;

    //        cr.WorkTrackCount = work.Tracks?.Count() ?? 0;
    //        cr.TrackContent = GetTrackContent(work);
    //        cr.WorkDescr = $"[W-{work?.Id ?? 0}]";
    //        cr.WorkName = $"[W-{work?.Name ?? string.Empty}]";

    //        cr.CompositionDescr = $"[C-{composition?.Id ?? 0}]";
    //        cr.CompositionName = composition?.Name ?? string.Empty;
    //        cr.PerformanceDescr = $"[P-{performance?.Id ?? 0}]";
    //        cr.PerformersCSV = performance.GetAllPerformersCSV();
    //        cr.PerformanceMovementCount = performance.Movements?.Count ?? 0;
    //        return cr;
    //    }
    //    public static CatalogueResult Create(IMusicSet set, CatalogueStatus status, Work work)
    //    {
    //        var cr = CatalogueResult.Create(set, status, work.Artists.First());
    //        cr.WorkTrackCount = work.Tracks?.Count() ?? 0;
    //        cr.TrackContent = GetTrackContent(work);
    //        cr.WorkDescr = $"[W-{work?.Id ?? 0}]";
    //        cr.WorkName = $"[W-{work?.Name ?? string.Empty}]";
    //        return cr;
    //    }
    //    private static string GetTrackContent(Work album)
    //    {
    //        var strings = new List<string>();
    //        var musicFiles = album.Tracks.First().MusicFiles;
    //        foreach (var mf in musicFiles)
    //        {
    //            var text = $"{mf.Encoding}{(mf.IsGenerated ? " (generated)" : string.Empty)}";
    //            strings.Add(text);
    //        }
    //        return $"({string.Join(", ", strings)})";
    //    }
    //}
}
