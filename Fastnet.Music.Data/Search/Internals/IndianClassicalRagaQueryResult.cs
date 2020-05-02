using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Data
{
    internal class IndianClassicalRagaQueryResult : IndianClassicalArtistQueryResult 
    {
        public SearchKey Raga { get; set; }
        //public IndianClassicalRagaQueryResult(RagaPerformance rp) : this(rp.Artist, rp.Raga)
        //{

        //}
        public IndianClassicalRagaQueryResult(IEnumerable<Artist> artists, Raga raga)
        {
            Artists = artists.Select(a => new SearchKey { Key = a.Id, Name = a.Name });
            Raga = new SearchKey { Key = raga.Id, Name = raga.Name };
        }
        private IndianClassicalRagaQueryResult(Artist artist, Raga raga) : this(new Artist[] { artist }, raga)
        {
        }
    }
}
