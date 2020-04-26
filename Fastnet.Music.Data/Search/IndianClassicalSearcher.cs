using Fastnet.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fastnet.Music.Data
{
    public enum IndianClassicalMatchType
    {
        Artist,
        Raga,
        Performance,
        Movement
    }
    public class IndianClassicalSearcher : CatalogueSearcher
    {
        protected override (bool prefixMode, IEnumerable<ISearchResult> searchResults) SearchCatalogue(string loweredSearch)
        {
            //var artists = GetMatchingArtists(loweredSearch).ToArray();
            //var ragaQueryResults = Enumerable.Empty<RagaQueryResult>();// new RagaQueryResult[0];
            //var performanceQueryResults = Enumerable.Empty<RagaPerformanceQueryResult>(); //new RagaPerformanceQueryResult[0];
            //var movementQueryResults = Enumerable.Empty<RagaPerformanceMovementQueryResult>();// new RagaPerformanceMovementQueryResult[0];
            //if (!prefixMatch)
            //{
            //    ragaQueryResults = GetMatchingRagas(loweredSearch).ToArray();
            //    performanceQueryResults = GetMatchingPerformances(loweredSearch).ToArray();
            //    movementQueryResults = GetMatchingMovements(loweredSearch).ToArray();
            //}
            var finalList = new List<IndianClassicalResult>();

            var queryResults = GetAllMatches(loweredSearch);
            foreach (var qr in queryResults.OfType<ICArtistQueryResult>())
            {
                finalList.Add(new IndianClassicalResult { Artists = qr.Artists, ArtistIsMatched = true });
            }
            static IndianClassicalResult GetICR(List<IndianClassicalResult> finalList, IEnumerable<SearchKey> artists)
            {
                //var icr = finalList.SingleOrDefault(x => x.Artists.Select(a => a.Key).SequenceEqual(artists.Select(z => z.Key), new LambdaEqualityComparer<long>((l, r) => l == r)));
                var icr = finalList.SingleOrDefault(x => x.Artists.Select(a => a.Key).SequenceEqual(artists.Select(z => z.Key)));
                if (icr == null)
                {
                    icr = new IndianClassicalResult { Artists = artists, ArtistIsMatched = false };
                    finalList.Add(icr);
                }
                return icr;
            }
            foreach (var rqr in queryResults.OfType<RagaQueryResult>())
            {
                var icr = GetICR(finalList, rqr.Artists);
                if (icr.ArtistIsMatched == false)
                {
                    icr.Ragas.Append(new RagaResult { Raga = rqr.Raga, RagaIsMatched = true });
                }
            }
            foreach (var rpqr in queryResults.OfType<RagaPerformanceQueryResult>())
            {
                var icr = GetICR(finalList, rpqr.Artists);
                var rr = icr.Ragas.SingleOrDefault(x => x.Raga.Key == rpqr.Raga.Key);
                if (rr == null)
                {
                    rr = new RagaResult { Raga = rpqr.Raga, RagaIsMatched = false };
                    icr.Ragas.Append(rr);
                }
                if (rr.RagaIsMatched == false)
                {
                    rr.Performances.Append(new PerformanceResult { Performance = rpqr.Performance, PerformanceIsMatched = true });
                }
            }
            return (prefixMatch, finalList);
        }
        //private void AddMatchedPerformancess(List<IndianClassicalResult> finalList, IEnumerable<RagaPerformanceQueryResult> performanceQueryResults)
        //{
        //    var idsForMatchedRagas = finalList.SelectMany(l => l.Ragas.Select(x => x.Raga.Key));
        //    var t1 = performanceQueryResults.Where(r => idsForMatchedRagas.Contains(r.Raga.Key));
        //    performanceQueryResults = performanceQueryResults.Except(t1);
        //    //performanceQueryResults now contains performances that matched with artists that did not match and ragas that did not match
        //    var performanceMatches = performanceQueryResults.GroupBy(k => new { artistKey = k.Artists.First().Key, ragaKey = k.Raga.Key, Artist = k.Artists.First(), k.Raga })
        //        .Select(g => new { g.Key.Artist, g.Key.Raga, list = g });
        //    foreach (var m in performanceMatches)
        //    {
        //        var a = finalList.SingleOrDefault(fl => fl.Artist.Key == m.Artist.Key);
        //        if (a != null)
        //        {
        //            var r = a.Ragas.SingleOrDefault(x => x.Raga.Key == m.Raga.Key);
        //            if (r != null)
        //            {
        //                if (r.RagaIsMatched == false)
        //                {
        //                    r.Performances = r.Performances.Union(
        //                        m.list.Select(x => new PerformanceResult
        //                        {
        //                            PerformanceIsMatched = true,
        //                            Performance = x.Performance,
        //                            Movements = Enumerable.Empty<TrackResult>()
        //                        }));
        //                }
        //            }
        //            else
        //            {
        //                var rr = new RagaResult
        //                {
        //                    RagaIsMatched = false,
        //                    Raga = m.Raga,
        //                    Performances = m.list.Select(m => new PerformanceResult { PerformanceIsMatched = true, Performance = m.Performance, Movements = Enumerable.Empty<TrackResult>() })
        //                };
        //                a.Ragas = a.Ragas.Append(rr);
        //            }
        //        }
        //        else
        //        {
        //            var icr = new IndianClassicalResult
        //            {
        //                Artist = m.Artist,//  x.list.First().Artist,
        //                ArtistIsMatched = false,
        //                Ragas = m.list.Select(r => new RagaResult
        //                {
        //                    RagaIsMatched = true,
        //                    Raga = r.Raga,
        //                    Performances = new List<PerformanceResult>(
        //                        new PerformanceResult[]
        //                        {
        //                            new PerformanceResult { PerformanceIsMatched = true, Performance = r.Performance, Movements = Enumerable.Empty<TrackResult>() }
        //                        }
        //                    )
        //                })
        //            };
        //            finalList.Add(icr);
        //        }
        //    }
        //}
        //private void AddMatchedRagas(List<IndianClassicalResult> finalList, IEnumerable<RagaQueryResult> ragaQueryResults)
        //{
        //    var idsForMatchedArtists = finalList.Select(l => l.Artist.Key);
        //    var t1 = ragaQueryResults.Where(r => idsForMatchedArtists.Contains(r.Artists.First().Key));
        //    ragaQueryResults = ragaQueryResults.Except(t1);
        //    // ragaqueryresults now contains ragas that matched with artists that did not match
        //    var ragaMatches = ragaQueryResults.GroupBy(k => new { k.Artists.First().Key, Artist = k.Artists.First() }).Select(g => new { g.Key.Artist, list = g });
        //    foreach (var m in ragaMatches)
        //    {
        //        var a = finalList.SingleOrDefault(fl => fl.Artist.Key == m.Artist.Key);
        //        if (a != null)
        //        {
        //            if (a.ArtistIsMatched == false)
        //            {
        //                a.Ragas = a.Ragas.Union(m.list.Select(x => new RagaResult { RagaIsMatched = true, Raga = x.Raga, Performances = Enumerable.Empty<PerformanceResult>() }));
        //            }
        //        }
        //        else
        //        {
        //            var icr = new IndianClassicalResult
        //            {
        //                Artist = m.Artist,//  x.list.First().Artist,
        //                ArtistIsMatched = false,
        //                Ragas = m.list.Select(r => new RagaResult { RagaIsMatched = true, Raga = r.Raga, Performances = Enumerable.Empty<PerformanceResult>() })
        //            };
        //            finalList.Add(icr);
        //        }
        //    }
        //}

        private IQueryable<RagaQueryResult> GetMatchingRagas(string loweredSearch)
        {
            // rewrite this to rplist becuase we need all the artists that have performed this raga
            var alphamericSearch = loweredSearch.ToAlphaNumerics();
            var rplist = MusicDb.Ragas.Where(c => c.AlphamericName.Contains(alphamericSearch))
                .Join(MusicDb.RagaPerformances, a => a.Id, rp => rp.RagaId, (a, b) => b);
            return rplist.Select(rp => new RagaQueryResult(rp));
        }
        private IEnumerable<RagaPerformanceQueryResult> GetMatchingPerformances(string loweredSearch)
        {
            var alphamericSearch = loweredSearch.ToAlphaNumerics();
            var rplist = MusicDb.RagaPerformances.Where(rp => rp.Performance.AlphamericPerformers.Contains(alphamericSearch));
            return rplist.Select(x => new RagaPerformanceQueryResult(x));
        }
        private IEnumerable<RagaPerformanceMovementQueryResult> GetMatchingMovements(string loweredSearch)
        {
            var alphamericSearch = loweredSearch.ToAlphaNumerics();
            return MusicDb.Tracks.Where(t => t.Performance.StyleId == MusicStyle && t.AlphamericTitle.Contains(alphamericSearch))
                .Join(MusicDb.RagaPerformances, a => a.Performance.Id, rp => rp.Performance.Id, (a, rp) => new { track = a, rp })
                .Select(r => new RagaPerformanceMovementQueryResult(r.rp, r.track));
        }
        private IEnumerable<IndianClassicalQueryResult> GetAllMatches(string loweredSearch)
        {
            var list = new List<IndianClassicalQueryResult>();
            var alphamericSearch = loweredSearch.ToAlphaNumerics();
            var query = MusicDb.RagaPerformances
                .Where(rp => rp.Artist.AlphamericName.Contains(alphamericSearch))
                .Select(x => new { Type = IndianClassicalMatchType.Artist, RagaPerformance = x })
                .Union(MusicDb.RagaPerformances
                    .Where(rp => rp.Raga.AlphamericName.Contains(alphamericSearch))
                    .Select(x => new { Type = IndianClassicalMatchType.Raga, RagaPerformance = x })
                )
                .Union(MusicDb.RagaPerformances
                    .Where(rp => rp.Performance.AlphamericPerformers.Contains(alphamericSearch))
                    .Select(x => new { Type = IndianClassicalMatchType.Performance, RagaPerformance = x })
                )
                .Union(MusicDb.RagaPerformances
                    .Where(rp => rp.Performance.Movements.Any(m => m.AlphamericTitle.Contains(alphamericSearch)))
                    .Select(x => new { Type = IndianClassicalMatchType.Movement, RagaPerformance = x })
                )
                .Distinct().OrderBy(x => x.Type);
            // removes ragas, performances and movements that are for a matched artist
            foreach (var q in query.Where(x => x.Type == IndianClassicalMatchType.Artist).ToArray())
            {
                var removable = query.Where(x => x.Type != IndianClassicalMatchType.Artist
                    && x.RagaPerformance.ArtistId == q.RagaPerformance.ArtistId);
                query.Except(removable);
            }
            // removes performances and movements that are for a matched raga
            foreach (var q in query.Where(x => x.Type == IndianClassicalMatchType.Raga).ToArray())
            {
                var removable = query.Where(x => (x.Type != IndianClassicalMatchType.Artist
                    && x.Type != IndianClassicalMatchType.Raga)
                    && x.RagaPerformance.RagaId == q.RagaPerformance.RagaId);
                query.Except(removable);
            }
            // removes movements that are for a matched performance
            foreach (var q in query.Where(x => x.Type == IndianClassicalMatchType.Performance).ToArray())
            {
                var removable = query.Where(x => (x.Type != IndianClassicalMatchType.Artist
                    && x.Type != IndianClassicalMatchType.Raga
                    && x.Type != IndianClassicalMatchType.Performance)
                    && x.RagaPerformance.PerformanceId == q.RagaPerformance.PerformanceId);
                query.Except(removable);
            }
            var performanceGroups = query.GroupBy(k => k.RagaPerformance.Performance).Select(g => g);
            foreach (var pg in performanceGroups)
            {
                var matchTypes = pg.Select(x => x.Type);
                var performance = pg.Key;
                var artists = pg.Select(x => x.RagaPerformance.Artist);
                var ragas = pg.Select(x => x.RagaPerformance.Raga);
                Debug.Assert(matchTypes.Count() == 1);
                Debug.Assert(ragas.Count() == 1);
                switch (matchTypes.First())
                {
                    case IndianClassicalMatchType.Artist:
                        list.Add(new ICArtistQueryResult { Artists = artists.Select(a => new SearchKey { Key = a.Id, Name = a.Name }) });
                        break;
                    case IndianClassicalMatchType.Raga:
                        list.Add(new RagaQueryResult(artists, ragas.First()));
                        break;
                    case IndianClassicalMatchType.Performance:
                        list.Add(new RagaPerformanceQueryResult(artists, ragas.First(), performance));
                        break;
                    case IndianClassicalMatchType.Movement:
                        //????
                        break;
                }
            }
            return list;
        }
    }
}
