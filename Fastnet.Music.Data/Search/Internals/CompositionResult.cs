using System.Collections.Generic;

namespace Fastnet.Music.Data
{
    internal class CompositionResult
    {
        public SearchKey Composition { get; set; }
        public bool CompositionIsMatched { get; set; } // meaning all performances therefore match
        public List<PerformanceResult> Performances { get; set; }
    }
}
