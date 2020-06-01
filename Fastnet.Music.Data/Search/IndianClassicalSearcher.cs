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
            static IndianClassicalResult getIndianClassicalResult(List<IndianClassicalResult> finalList, IEnumerable<SearchKey> artists)
            {
                var keys = artists.Select(z => z.Key).OrderBy(n => n);
                var icr = finalList.SingleOrDefault(x => x.Artists.Select(a => a.Key).OrderBy(n => n).SequenceEqual(keys));
                if (icr == null)
                {
                    icr = new IndianClassicalResult { Artists = artists, ArtistIsMatched = false };
                    finalList.Add(icr);
                }
                return icr;
            }
            var finalList = new List<IndianClassicalResult>();
            var queryResults = GetAllMatches(loweredSearch);
            foreach (var qr in queryResults.Where(x => x.GetType() == typeof(IndianClassicalArtistQueryResult)).Cast<IndianClassicalArtistQueryResult>())
            {
                finalList.Add(new IndianClassicalResult { Artists = qr.Artists, ArtistIsMatched = true });
            }
            foreach (var rqr in queryResults.Where(x => x.GetType() == typeof(IndianClassicalRagaQueryResult)).Cast<IndianClassicalRagaQueryResult>())
            {
                var icr = getIndianClassicalResult(finalList, rqr.Artists);
                if (icr.ArtistIsMatched == false)
                {
                    var _ = icr.GetRagaResult(rqr.Raga, true);
                }
            }
            foreach (var rpqr in queryResults.Where(x => x.GetType() == typeof(IndianClassicalRagaPerformanceQueryResult)).Cast<IndianClassicalRagaPerformanceQueryResult>())
            {
                var icr = getIndianClassicalResult(finalList, rpqr.Artists);
                var rr = icr.GetRagaResult(rpqr.Raga);
                if (rr.RagaIsMatched == false)
                {
                    /*rr.Performances = */rr.Performances.Add(new PerformanceResult { Performance = rpqr.Performance, PerformanceIsMatched = true });
                }
            }
            foreach (var rpqr in queryResults.Where(x => x.GetType() == typeof(IndianClassicalRagaPerformanceMovementQueryResult)).Cast<IndianClassicalRagaPerformanceMovementQueryResult>())
            {
                var icr = getIndianClassicalResult(finalList, rpqr.Artists);
                var rr = icr.GetRagaResult(rpqr.Raga);
                var pr = rr.Performances.SingleOrDefault(x => x.Performance.Key == rpqr.Performance.Key);
                if (pr == null)
                {
                    pr = new PerformanceResult { Performance = rpqr.Performance, PerformanceIsMatched = false };
                    /*rr.Performances = */rr.Performances.Add(pr);
                }
                if (pr.PerformanceIsMatched == false)
                {
                    foreach (var m in rpqr.Movements)
                    {
                        /*pr.Movements = */pr.Movements.Add(new TrackResult { Track = m });
                    }
                }
            }
            //finalList.OrderBy(x => x.Artists.Count())
            return (prefixMatch, finalList.OrderBy(x => x.Artists.Count()));
        }
#pragma warning disable IDE1006 // Naming Styles
        class _SearchResult
#pragma warning restore IDE1006 // Naming Styles
        {
            public IndianClassicalMatchType Type;
            public RagaPerformance RagaPerformance;
            public Track Movement;
        }
        private IEnumerable<IndianClassicalQueryResult> GetAllMatches(string loweredSearch)
        {
            var list = new List<IndianClassicalQueryResult>();
            var list2 = new Dictionary<string, IndianClassicalQueryResult>();
            var alphamericSearch = loweredSearch.ToAlphaNumerics();
            //var rpList = MusicDb.RagaPerformances.ToArray();
            var q1 = MusicDb.RagaPerformances
                .Where(rp => rp.Artist.AlphamericName.Contains(alphamericSearch))
                .Select(x => new _SearchResult { Type = IndianClassicalMatchType.Artist, RagaPerformance = x }).ToList();
            var q2 = MusicDb.RagaPerformances
                    .Where(rp => rp.Raga.AlphamericName.Contains(alphamericSearch))
                    .Select(x => new _SearchResult { Type = IndianClassicalMatchType.Raga, RagaPerformance = x });
            var q3 = MusicDb.RagaPerformances
                    .Where(rp => rp.Performance.AlphamericPerformers.Contains(alphamericSearch))
                    .Select(x => new _SearchResult { Type = IndianClassicalMatchType.Performance, RagaPerformance = x });
            var q4 = MusicDb.RagaPerformances
                    .SelectMany(rp => rp.Performance.Movements, (rp, m) => new _SearchResult { Type = IndianClassicalMatchType.Movement, RagaPerformance = rp, Movement = m })
                    .Where(x => x.Movement.AlphamericTitle.Contains(alphamericSearch));
            if (q1.Count() > 0)
            {
                // we have matched an artist, so we should also match other artists that this one has jointly performed with
                // as in jugalbandi's
                var artists = q1.Select(x => x.RagaPerformance.ArtistId).Distinct();
                foreach (var id in artists.ToArray())
                {
                    // for each matched artist id
                    var performances = MusicDb.RagaPerformances.Where(x => x.ArtistId == id).Select(x => x.Performance);
                    foreach (var performance in performances)
                    {
                        var possibleJoint = MusicDb.RagaPerformances.Where(x => x.Performance == performance);
                        var jointRps = possibleJoint.Where(x => x.ArtistId != id);//.Select(x => x.ArtistId);
                        foreach (var rp in jointRps)
                        {
                            q1.Add(new _SearchResult { Type = IndianClassicalMatchType.Artist, RagaPerformance = rp });
                        }
                    }
                }
            }
            var query = q1
                .Union(q2.ToArray())
                .Union(q3.ToArray())
                .Union(q4.ToArray())
                .Distinct().OrderBy(x => x.Type).AsEnumerable();

            var itemsToRemove = new List<_SearchResult>();
            // removes ragas, performances and movements that are for a matched artist
            foreach (var q in query.Where(x => x.Type == IndianClassicalMatchType.Artist)/*.ToArray()*/)
            {
                var removable = query.Where(x => x.Type != IndianClassicalMatchType.Artist
                    && x.RagaPerformance.ArtistId == q.RagaPerformance.ArtistId);
                itemsToRemove.AddRange(removable);
                //query = query.Except(removable);
            }
            // removes performances and movements that are for a matched raga
            foreach (var q in query.Where(x => x.Type == IndianClassicalMatchType.Raga)/*.ToArray()*/)
            {
                var removable = query.Where(x => (x.Type != IndianClassicalMatchType.Artist
                    && x.Type != IndianClassicalMatchType.Raga)
                    && x.RagaPerformance.RagaId == q.RagaPerformance.RagaId);
                itemsToRemove.AddRange(removable);
                //query = query.Except(removable);
            }
            // removes movements that are for a matched performance
            foreach (var q in query.Where(x => x.Type == IndianClassicalMatchType.Performance)/*.ToArray()*/)
            {
                var removable = query.Where(x => (x.Type != IndianClassicalMatchType.Artist
                    && x.Type != IndianClassicalMatchType.Raga
                    && x.Type != IndianClassicalMatchType.Performance)
                    && x.RagaPerformance.PerformanceId == q.RagaPerformance.PerformanceId);
                itemsToRemove.AddRange(removable);
                //query = query.Except(removable);
            }
            query = query.Except(itemsToRemove);
            var performanceGroups = query.GroupBy(k => k.RagaPerformance.Performance).Select(g => g);
            foreach (var pg in performanceGroups)
            {
                var matchTypes = pg.Select(x => x.Type).Distinct();
                var performance = pg.Key;
                var artists = pg.Select(x => x.RagaPerformance.Artist).Distinct();
                var raga = pg.Select(x => x.RagaPerformance.Raga).Distinct().Single();
                var movements = pg.Where(x => x.Movement != null).Select(x => x.Movement).Distinct();//.Single();
                if (matchTypes.Count() > 1)
                {
                    log.Information($"multiple match types found - interesting???????????");
                }
                var artistIdKey = string.Join(",", artists.Select(a => a.Id).OrderBy(x => x));
                var artistsAreMatchedKey = $"{IndianClassicalMatchType.Artist}|{artistIdKey}";
                var ragaIsMatchedKey = $"{IndianClassicalMatchType.Raga}|{artistIdKey}|{raga.Id}";
                //var ragaIsMatchedKey = $"{IndianClassicalMatchType.Raga}|{artistIdKey}|{raga?.Id.ToString() ?? string.Empty}";
                var performanceIsMatchedKey = $"{IndianClassicalMatchType.Performance}|{artistIdKey}|{raga.Id}|{performance.Id}";
                var movementsAreMatchedKey = $"{IndianClassicalMatchType.Movement}|{artistIdKey}|{raga.Id}|{performance.Id}|{(string.Join(",", movements.Select(a => a.Id).OrderBy(x => x)))}";
                foreach (var mt in matchTypes)
                {

                    switch (mt)
                    {
                        case IndianClassicalMatchType.Artist:
                            if (!list2.ContainsKey(artistsAreMatchedKey))
                            {
                                list2.Add(artistsAreMatchedKey, new IndianClassicalArtistQueryResult { Artists = artists.Select(a => new SearchKey { Key = a.Id, Name = a.Name }) });
                            }
                            list.Add(new IndianClassicalArtistQueryResult { Artists = artists.Select(a => new SearchKey { Key = a.Id, Name = a.Name }) });
                            break;
                        case IndianClassicalMatchType.Raga:
                            if (!list2.ContainsKey(artistsAreMatchedKey) && !list2.ContainsKey(ragaIsMatchedKey))
                            {
                                list2.Add(ragaIsMatchedKey, new IndianClassicalRagaQueryResult(artists, raga));
                            }
                            list.Add(new IndianClassicalRagaQueryResult(artists, raga));
                            break;
                        case IndianClassicalMatchType.Performance:
                            if (!list2.ContainsKey(artistsAreMatchedKey) && !list2.ContainsKey(ragaIsMatchedKey) && !list2.ContainsKey(performanceIsMatchedKey))
                            {
                                list2.Add(performanceIsMatchedKey, new IndianClassicalRagaPerformanceQueryResult(artists, raga, performance));
                            }
                            list.Add(new IndianClassicalRagaPerformanceQueryResult(artists, raga, performance));
                            break;
                        case IndianClassicalMatchType.Movement:
                            if (!list2.ContainsKey(artistsAreMatchedKey) && !list2.ContainsKey(ragaIsMatchedKey) && !list2.ContainsKey(performanceIsMatchedKey) && !list2.ContainsKey(movementsAreMatchedKey))
                            {
                                list2.Add(movementsAreMatchedKey, new IndianClassicalRagaPerformanceMovementQueryResult(artists, raga, performance, movements));
                            }
                            list.Add(new IndianClassicalRagaPerformanceMovementQueryResult(artists, raga, performance, movements));
                            break;
                    }
                }
            }
            return list2.Values.AsEnumerable();
        }
    }
}
