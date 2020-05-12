using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Data
{
    internal class IndianClassicalRagaResult 
    {
        public SearchKey Raga { get; set; }
        public bool RagaIsMatched { get; set; }
        public List<PerformanceResult> Performances { get; set; } = new List<PerformanceResult>();
    }
}
