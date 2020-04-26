using Fastnet.Core;
using Fastnet.Music.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fastnet.Music.Data
{
    public static class __xxx
    {
        private static Regex inan = new Regex(@"[^a-zA-Z0-9\p{L}]", RegexOptions.IgnoreCase);
        private static Regex splitToWords = new Regex(@"(\b[^\s]+\b)", RegexOptions.IgnoreCase);
    }

    public class SearchKey
    {
        public long Key { get; set; }
        public string Name { get; set; }
    }
    public class TrackKey : SearchKey
    {
        public int? Number { get; set; }
    }
    public interface IQueryResult
    {

    }
    internal class ArtistQueryResult : IQueryResult
    {
        //public SearchKey Artist { get; set; }
        public IEnumerable<SearchKey> Artists { get; set; }
        //public IEnumerable<SearchKey> Works { get; set; }
    }
    internal class WorkQueryResult //: SearchResult
    {
        public SearchKey Artist { get; set; }
        public SearchKey Work { get; set; }
    }
    internal class TrackQueryResult //: SearchResult
    {
        public SearchKey Artist { get; set; }
        public SearchKey Work { get; set; }
        public TrackKey Track { get; set; }
    }
    internal class CompositionQueryResult
    {
        public SearchKey Composer { get; set; }
        public SearchKey Composition { get; set; }
    }

    internal class CompositionPerformanceQueryResult
    {
        public SearchKey Composer { get; set; }
        public SearchKey Composition { get; set; }
        public SearchKey Performance { get; set; }
    }
    internal class CompositionPerformanceMovementQueryResult //: SearchResult
    {
        public SearchKey Composer { get; set; }
        public SearchKey Composition { get; set; }
        public SearchKey Performance { get; set; }
        public TrackKey Movement { get; set; }
    }
    internal interface IndianClassicalQueryResult : IQueryResult
    {
        IEnumerable<SearchKey> Artists { get; set; }
    }
    internal class ICArtistQueryResult : IndianClassicalQueryResult
    {
        //public SearchKey Artist { get; set; }
        public IEnumerable<SearchKey> Artists { get; set; }
        //public IEnumerable<SearchKey> Works { get; set; }
    }
    internal class RagaQueryResult : ICArtistQueryResult //, IndianClassicalQueryResult // IQueryResult
    {
        //public SearchKey Artist { get; set; }
        public SearchKey Raga { get; set; }
        public RagaQueryResult(RagaPerformance rp) : this(rp.Artist, rp.Raga)
        {

        }
        public RagaQueryResult(IEnumerable<Artist> artists, Raga raga)
        {
            Artists = artists.Select(a => new SearchKey { Key = a.Id, Name = a.Name });
            Raga = new SearchKey { Key = raga.Id, Name = raga.Name };
        }
        private RagaQueryResult(Artist artist, Raga raga) : this(new Artist[] { artist }, raga)
        {
        }
    }
    internal class RagaPerformanceQueryResult : RagaQueryResult, IndianClassicalQueryResult
    {
        public SearchKey Performance { get; set; }
        public RagaPerformanceQueryResult(RagaPerformance rp) : base(rp)
        {
            Performance = new SearchKey { Key = rp.Performance.Id, Name = rp.Performance.GetAllPerformersCSV() };
        }
        public RagaPerformanceQueryResult(IEnumerable<Artist> artists, Raga raga, Performance performance) : base(artists, raga)
        {
            Performance = new SearchKey { Key = performance.Id, Name = performance.GetAllPerformersCSV() };
        }
    }

    internal class RagaPerformanceMovementQueryResult : RagaPerformanceQueryResult, IndianClassicalQueryResult
    {
        public TrackKey Movement { get; set; }
        public RagaPerformanceMovementQueryResult(RagaPerformance rp, Track track) : base(rp)
        {
            Movement = new TrackKey { Key = track.Id, Name = track.Title, Number = track.Number };
        }

    }
    public interface ISearchResult
    {

    }
    public class PopularResult : ISearchResult
    {
        public SearchKey Artist { get; set; }
        public bool ArtistIsMatched { get; set; } // meaning all works therefore match
        public List<WorkResult> Works { get; set; }
    }
    public class WorkResult
    {
        public SearchKey Work { get; set; }
        public bool WorkIsMatched { get; set; } // meaning all tracks therefore match
        public List<TrackResult> Tracks { get; set; }
    }
    public class TrackResult
    {
        public TrackKey Track { get; set; }
    }
    public class WesternClassicalResult : ISearchResult
    {
        public SearchKey Composer { get; set; }
        public bool ComposerIsMatched { get; set; } // meaning all compositions therefore match
        public List<CompositionResult> Compositions { get; set; }
    }
    public class IndianClassicalResult : ISearchResult
    {
        public IEnumerable<SearchKey> Artists { get; set; }
        public bool ArtistIsMatched { get; set; } // meaning at least one of the artis is matched
        public IEnumerable<RagaResult> Ragas { get; set; }
    }
    public class RagaResult 
    {
        public SearchKey Raga { get; set; }
        public bool RagaIsMatched { get; set; }
        public IEnumerable<PerformanceResult> Performances { get; set; }
    }
    public class CompositionResult
    {
        public SearchKey Composition { get; set; }
        public bool CompositionIsMatched { get; set; } // meaning all performances therefore match
        public List<PerformanceResult> Performances { get; set; }
    }
    public class PerformanceResult 
    {
        public SearchKey Performance { get; set; }
        public bool PerformanceIsMatched { get; set; } // meaning all performances therefore match
        public IEnumerable<TrackResult> Movements { get; set; }
    }
    public abstract class CatalogueSearcher
    {
        protected static NaturalStringComparer naturalComparer = new NaturalStringComparer();
        protected MusicOptions Options { get; private set; }
        protected MusicStyles MusicStyle { get; private set; }
        protected MusicDb MusicDb { get; private set; }
        protected bool prefixMatch;
        protected ILogger log;
        public static CatalogueSearcher GetSearcher(MusicOptions options, MusicStyles style, MusicDb musicDb, ILogger log = null)
        {
            CatalogueSearcher cs = null;
            switch (style)
            {
                case MusicStyles.Popular:
                    cs = new PopularSearcher();
                    break;
                case MusicStyles.WesternClassical:
                    cs = new WesternClassicalSearcher();
                    break;
                case MusicStyles.IndianClassical:
                    cs = new IndianClassicalSearcher();
                    break;
            }
            cs.Options = options;
            cs.MusicStyle = style;
            cs.MusicDb = musicDb;
            cs.log = log;
            return cs;
        }
        protected abstract (bool prefixMode, IEnumerable<ISearchResult> searchResults) SearchCatalogue(string searchText);
        public (bool prefixMode, IEnumerable<ISearchResult> searchResults) Search(string searchText)
        {
            prefixMatch = false;
            if (this.Options.SearchPrefixLength > 0 && searchText.Length <= this.Options.SearchPrefixLength)
            {
                prefixMatch = true;
            }
            var loweredSearch = searchText.ToLower().Trim();
            return SearchCatalogue(loweredSearch);
        }
        private bool prefixMatchAnyWord(string text, string search)
        {
            var parts = text.ToWords();
            return parts.Any(w => w.StartsWithIgnoreAccentsAndCase(search));
        }
        internal IEnumerable<ArtistQueryResult> GetMatchingArtists(string loweredSearch)
        {
            log?.Debug($"searching for artist {loweredSearch} in style {MusicStyle.ToString()}");
            var artists = MusicDb.ArtistStyles
                .Where(x1 => x1.StyleId == MusicStyle)
                .Select(x2 => x2.Artist).ToArray();
            if (prefixMatch)
            {
                artists = artists.Where(x => prefixMatchAnyWord(x.Name, loweredSearch)).ToArray();
                log?.Debug($"found {artists.Count()} with prefix {loweredSearch}");
            }
            else
            {
                artists = artists
                    .Where(x => x.Name.Contains(loweredSearch, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols))
                    .ToArray();
                log?.Debug($"found {artists.Count()} containing {loweredSearch}");
            }

            return artists.Select(x => new ArtistQueryResult
            {
                Artists = new SearchKey[]
                    {  new SearchKey {
                        Key = x.Id,
                        Name = x.Name
                    }
                },
            });
        }
    }
}
