using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Data
{
    internal class PerformanceResult 
    {
        public SearchKey Performance { get; set; }
        public bool PerformanceIsMatched { get; set; } // meaning all performances therefore match
        public List<TrackResult> Movements { get; set; } = new List<TrackResult>();
    }
}
