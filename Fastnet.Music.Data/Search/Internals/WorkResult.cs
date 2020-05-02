using System.Collections.Generic;

namespace Fastnet.Music.Data
{
    internal class WorkResult
    {
        public SearchKey Work { get; set; }
        public bool WorkIsMatched { get; set; } // meaning all tracks therefore match
        public List<TrackResult> Tracks { get; set; }
    }
}
