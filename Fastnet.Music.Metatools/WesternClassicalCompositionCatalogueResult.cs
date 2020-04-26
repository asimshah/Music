using Fastnet.Music.Data;
using System;

namespace Fastnet.Music.Metatools
{
    public class WesternClassicalCompositionCatalogueResult : BaseCatalogueResult
    {
        public string CompositionDescr { get; set; } = String.Empty;
        public string CompositionName { get; set; } = String.Empty;
        public string PerformanceDescr { get; set; } = String.Empty;
        public string PerformersCSV { get; set; } = String.Empty;
        public int PerformanceMovementCount { get; set; }
        public WesternClassicalCompositionCatalogueResult(BaseMusicSet set, CatalogueStatus status, Performance performance) : base(set, status)
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
                return $"{ArtistDescr} {ArtistNames}, {CompositionDescr} {CompositionName}, {PerformanceMovementCount} movements, {PerformanceDescr} \"{PerformersCSV}\"";
            }
        }
    }
}
