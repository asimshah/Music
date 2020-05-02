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
        private bool PrefixMatchAnyWord(string text, string search)
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
                artists = artists.Where(x => PrefixMatchAnyWord(x.Name, loweredSearch)).ToArray();
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
