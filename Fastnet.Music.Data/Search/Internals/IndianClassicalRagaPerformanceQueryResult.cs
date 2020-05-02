using System.Collections.Generic;

namespace Fastnet.Music.Data
{
    internal class IndianClassicalRagaPerformanceQueryResult : IndianClassicalRagaQueryResult, IndianClassicalQueryResult
    {
        public SearchKey Performance { get; set; }
        //public IndianClassicalRagaPerformanceQueryResult(RagaPerformance rp) : base(rp)
        //{
        //    Performance = new SearchKey { Key = rp.Performance.Id, Name = rp.Performance.GetAllPerformersCSV() };
        //}
        public IndianClassicalRagaPerformanceQueryResult(IEnumerable<Artist> artists, Raga raga, Performance performance) : base(artists, raga)
        {
            Performance = new SearchKey { Key = performance.Id, Name = performance.GetAllPerformersCSV() };
        }
    }
}
