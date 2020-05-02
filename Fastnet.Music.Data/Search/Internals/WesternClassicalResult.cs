using System.Collections.Generic;

namespace Fastnet.Music.Data
{
    internal class WesternClassicalResult : ISearchResult
    {
        public SearchKey Composer { get; set; }
        public bool ComposerIsMatched { get; set; } // meaning all compositions therefore match
        public List<CompositionResult> Compositions { get; set; }
    }
}
