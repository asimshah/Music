using System.Collections.Generic;

namespace Fastnet.Music.Data
{
    internal class PopularResult : ISearchResult
    {
        public SearchKey Artist { get; set; }
        public bool ArtistIsMatched { get; set; } // meaning all works therefore match
        public List<WorkResult> Works { get; set; }
    }
}
