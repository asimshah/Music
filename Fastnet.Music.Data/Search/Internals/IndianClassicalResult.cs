using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Data
{
    internal class IndianClassicalResult : ISearchResult
    {
        public IEnumerable<SearchKey> Artists { get; set; } = Enumerable.Empty<SearchKey>();
        public bool ArtistIsMatched { get; set; } // meaning at least one of the artis is matched
        public List<IndianClassicalRagaResult> Ragas { get; set; } = new List<IndianClassicalRagaResult>();
        internal IndianClassicalRagaResult GetRagaResult(SearchKey ragaKey, bool isMatched = false)
        {
            var rr = Ragas.SingleOrDefault(x => x.Raga.Key == ragaKey.Key);
            if (rr == null)
            {
                rr = new IndianClassicalRagaResult { Raga = ragaKey, RagaIsMatched = isMatched };
                /*Ragas = */Ragas.Add(rr);
            }
            return rr;
        }
    }
}
