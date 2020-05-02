using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Data
{
    internal class IndianClassicalRagaResult 
    {
        public SearchKey Raga { get; set; }
        public bool RagaIsMatched { get; set; }
        public IEnumerable<PerformanceResult> Performances { get; set; } = Enumerable.Empty<PerformanceResult>();
    }
}
