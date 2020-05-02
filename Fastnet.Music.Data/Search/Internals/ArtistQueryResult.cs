using System.Collections.Generic;

namespace Fastnet.Music.Data
{
    internal class ArtistQueryResult : IQueryResult
    {
        public IEnumerable<SearchKey> Artists { get; set; }
    }
}
