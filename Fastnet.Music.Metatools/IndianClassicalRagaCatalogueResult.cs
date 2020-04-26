using Fastnet.Music.Data;
using System.Collections.Generic;

namespace Fastnet.Music.Metatools
{
    public class IndianClassicalRagaCatalogueResult : BaseCatalogueResult
    {
        public string RagaDescr { get; }
        public string RagaName { get;  } = string.Empty;
        public string PerformanceDescr { get;  } = string.Empty;
        public string PerformersCSV { get; set; } = string.Empty;
        public int PerformanceMovementCount { get; set; }
        public IndianClassicalRagaCatalogueResult(BaseMusicSet set, CatalogueStatus status, IEnumerable<Artist> artists, Raga raga, Performance performance) : base(set, status)
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
}
