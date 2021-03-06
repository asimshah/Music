﻿using Fastnet.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fastnet.Music.Data
{
    public static class wcs_extensions
    {
        //public static string ToCSV(this ICollection<Artist> performers)
        //{
        //    return string.Join(", ", performers.Select(x => x.Name).ToArray());
        //}
        //public static string ToSortable(this string text)
        //{
        //    var words = text.Split(' ');
        //    for(int i = 0;i < words.Length;++i)
        //    //foreach(var word in words)
        //    {
        //        var isNumeric = int.TryParse(words[i], out int n);
        //        if(isNumeric)
        //        {
        //            words[i] = $"00000{words[i]}";
        //        }
        //    }
        //    return string.Join("", words);
        //}
    }
    public class WesternClassicalSearcher : CatalogueSearcher
    {
        private static NaturalStringComparer naturalComparer = new NaturalStringComparer();
        protected override (bool prefixMode, IEnumerable<ISearchResult> searchResults) SearchCatalogue(string loweredSearch)
        {
            // get artists that match the searchText
            var artists = GetMatchingArtists(loweredSearch).ToArray();
            var compositionQueryResults = new CompositionQueryResult[0];
            var performanceQueryResults = new PerformanceQueryResult[0];
            var movementQueryResults = new MovementQueryResult[0];
            if (!prefixMatch)
            {
                // get compositions that match the searchText
                compositionQueryResults = GetMatchingCompositions(loweredSearch).ToArray();
                performanceQueryResults = GetMatchingPerformaces(loweredSearch).ToArray();
                movementQueryResults = GetMatchingMovements(loweredSearch).ToArray();
            }
            var finalList = new List<WesternClassicalResult>();
            finalList.AddRange(artists.Select(a => new WesternClassicalResult { Composer = a.Artist, ComposerIsMatched = true }));
            // eliminate compositions whose artists are already in the artists list (as all their compositions are matched by implication)
            var artistKeys = finalList.Select(l => l.Composer.Key);
            var t1 = compositionQueryResults.Where(w => artistKeys.Contains(w.Composer.Key));
            compositionQueryResults = compositionQueryResults.Except(t1).ToArray();
            // add compositions for artists that were not matched
            foreach (var cqr in compositionQueryResults)
            {
                // check if this artist has already been added
                var wcr = finalList.SingleOrDefault(x => x.Composer.Key == cqr.Composer.Key);
                if (wcr == null)
                {
                    wcr = new WesternClassicalResult
                    {
                        Composer = cqr.Composer,
                        ComposerIsMatched = false,
                        Compositions = new List<CompositionResult>()
                    };
                    finalList.Add(wcr);
                }
                wcr.Compositions.Add(new CompositionResult
                {
                    Composition = cqr.Composition,
                    CompositionIsMatched = true
                });
            }
            // eliminate performanes where the composition or the artist is already in the list is already in the list
            var t2 = performanceQueryResults.Where(p => artistKeys.Contains(p.Composer.Key));
            performanceQueryResults = performanceQueryResults.Except(t2).ToArray();
            var compositionKeys = finalList.Where(l => l.Compositions != null).SelectMany(l => l.Compositions.Select(c => c.Composition.Key));
            t2 = performanceQueryResults.Where(x => compositionKeys.Contains(x.Composition.Key));
            performanceQueryResults = performanceQueryResults.Except(t2).ToArray();
            foreach (var pqr in performanceQueryResults)
            {
                var wcr = finalList.SingleOrDefault(x => x.Composer.Key == pqr.Composer.Key);
                if (wcr == null)
                {
                    wcr = new WesternClassicalResult
                    {
                        Composer = pqr.Composer,
                        ComposerIsMatched = false,
                        Compositions = new List<CompositionResult>()
                    };
                    finalList.Add(wcr);
                }
                var cr = wcr.Compositions.SingleOrDefault(x => x.Composition.Key == pqr.Composition.Key);
                if (cr == null)
                {
                    cr = new CompositionResult
                    {
                        Composition = pqr.Composition,
                        CompositionIsMatched = false,
                        Performances = new List<PerformanceResult>()
                    };
                    wcr.Compositions.Add(cr);
                }
                cr.Performances.Add(new PerformanceResult
                {
                    Performance = pqr.Performance,
                    PerformanceIsMatched = true
                });
            }
            var m2 = movementQueryResults.Where(m => artistKeys.Contains(m.Composer.Key));
            movementQueryResults = movementQueryResults.Except(m2).ToArray();
            m2 = movementQueryResults.Where(x => compositionKeys.Contains(x.Composition.Key));
            movementQueryResults = movementQueryResults.Except(m2).ToArray();
            var performanceKeys = finalList
                .Where(x => x.Compositions != null)
                .SelectMany(x => x.Compositions
                    .Where(x2 => x2.Performances != null)
                    .SelectMany(z => z.Performances.Select(p => p.Performance.Key)));
            m2 = movementQueryResults.Where(x => performanceKeys.Contains(x.Performance.Key));
            movementQueryResults = movementQueryResults.Except(m2).ToArray();
            foreach (var mr in movementQueryResults)
            {
                var wcr = finalList.SingleOrDefault(x => x.Composer.Key == mr.Composer.Key);
                if (wcr == null)
                {
                    wcr = new WesternClassicalResult
                    {
                        Composer = mr.Composer,
                        ComposerIsMatched = false,
                        Compositions = new List<CompositionResult>()
                    };
                    finalList.Add(wcr);
                }
                var cr = wcr.Compositions.SingleOrDefault(x => x.Composition.Key == mr.Composition.Key);
                if (cr == null)
                {
                    cr = new CompositionResult
                    {
                        Composition = mr.Composition,
                        CompositionIsMatched = false,
                        Performances = new List<PerformanceResult>()
                    };
                    wcr.Compositions.Add(cr);

                    var pr = wcr.Compositions.SelectMany(x => x.Performances).SingleOrDefault(x => x.Performance.Key == mr.Performance.Key);
                    if (pr == null)
                    {
                        pr = new PerformanceResult
                        {
                            Performance = mr.Performance,
                            PerformanceIsMatched = false,
                            Movements = new List<TrackResult>()
                        };
                        cr.Performances.Add(pr);
                    }
                    pr.Movements.Add(new TrackResult { Track = mr.Movement });
                }
            }

            SortNaturalResults(finalList);
            return (prefixMatch, finalList);
        }
        //protected void SortResults(List<WesternClassicalResult> results)
        //{
        //    //var results = list.Cast<WesternClassicalResult>() as List<WesternClassicalResult>;
        //    results.Sort((l, r) =>
        //    {
        //        return string.Compare(l.Composer.Name, r.Composer.Name, true);
        //    });
        //    results.ForEach((wcr) =>
        //    {
        //        if(wcr.Compositions != null)
        //        {
        //            wcr.Compositions.Sort((l, r) =>
        //            {
        //                return string.Compare(l.Composition.Name, r.Composition.Name, true);
        //            });
        //            wcr.Compositions.ForEach((cr) =>
        //            {
        //                if(cr.Performances != null)
        //                {
        //                    cr.Performances.Sort((l, r) =>
        //                    {
        //                        return string.Compare(l.Performance.Name, r.Performance.Name, true);
        //                    });
        //                }
        //            });
        //        }
        //    });
        //}
        protected void SortNaturalResults(List<WesternClassicalResult> results)
        {
            //var results = list.Cast<WesternClassicalResult>() as List<WesternClassicalResult>;
            results.Sort((l, r) =>
            {
                //var result = string.Compare(l.Composer.Name, r.Composer.Name, true);
                var result = l.Composer.Name.CompareIgnoreAccentsAndCase(r.Composer.Name);
                //Debug.WriteLine($"SortNaturalResults: left {l.Composer.Name}, right {r.Composer.Name}, right {result}");
                return result;// string.Compare(l.Composer.Name, r.Composer.Name, true);
            });
            results.ForEach((wcr) =>
            {
                if (wcr.Compositions != null)
                {
                    wcr.Compositions.Sort((l, r) => naturalComparer.Compare(l.Composition.Name, r.Composition.Name));
                    wcr.Compositions.ForEach((cr) =>
                    {
                        if (cr.Performances != null)
                        {
                            cr.Performances.Sort((l, r) =>
                            {
                                return string.Compare(l.Performance.Name, r.Performance.Name, true);
                            });
                        }
                    });
                }
            });
        }
        private IEnumerable<PerformanceQueryResult> GetMatchingPerformaces(string loweredSearch)
        {
            //var performances = MusicDb.Performances.Where(p => p.Performers.ToAlphaNumerics().ToLower().Contains(loweredSearch.ToAlphaNumerics()));
            var performances = MusicDb.Performances.Where(p => p.AlphamericPerformers.Contains(loweredSearch.ToAlphaNumerics()));
            return performances.Select(x => new PerformanceQueryResult
            {
                Composer = new SearchKey
                {
                    Key = x.Composition.Artist.Id,
                    Name = x.Composition.Artist.Name
                },
                Composition = new SearchKey
                {
                    Key = x.Composition.Id,
                    Name = x.Composition.Name
                },
                Performance = new SearchKey
                {
                    Key = x.Id,
                    Name = x.Performers //.ToCSV()
                }
            });
        }
        private IQueryable<CompositionQueryResult> GetMatchingCompositions(string loweredSearch)
        {
            //var compositions = MusicDb.Compositions.Where(c => c.Name.ToAlphaNumerics().ToLower().Contains(loweredSearch.ToAlphaNumerics()));
            var compositions = MusicDb.Compositions.Where(c => c.AlphamericName.Contains(loweredSearch.ToAlphaNumerics()));
            return compositions.Select(x => new CompositionQueryResult
            {
                Composer = new SearchKey
                {
                    Key = x.Artist.Id,
                    Name = x.Artist.Name
                },
                Composition = new SearchKey
                {
                    Key = x.Id,
                    Name = x.Name
                }
            });
        }
        private IEnumerable<MovementQueryResult> GetMatchingMovements(string loweredSearch)
        {
            var tracks = MusicDb.Tracks.Where(x1 => x1.Work.StyleId == MusicStyle);//.ToArray();

            //tracks = tracks.Where(x => x.Title.ToAlphaNumerics().ToLower().Contains(loweredSearch.ToAlphaNumerics())).ToArray();
            tracks = tracks.Where(x => x.AlphamericTitle.Contains(loweredSearch.ToAlphaNumerics()));//.ToArray();
            return tracks.Select(x => new MovementQueryResult
            {
                //Composer = new SearchKey
                //{
                //    Key = x.Work.Artist.Id,
                //    Name = x.Work.Artist.Name
                //},
                Composer = new SearchKey
                {
                    Key = x.Performance.Composition.Artist.Id,
                    Name = x.Performance.Composition.Artist.Name
                },
                Composition = new SearchKey
                {

                    Key = x.Performance.Composition.Id,
                    Name = x.Performance.Composition.Name
                },
                Performance = new SearchKey
                {
                    Key = x.Performance.Id,
                    Name = x.Performance.Performers //.ToCSV()
                },
                Movement = new TrackKey
                {
                    Key = x.Id,
                    Name = x.Title,
                    Number = (x.Work.Type == Core.OpusType.Singles || x.Work.Type == Core.OpusType.Collection ? new int?() : x.Number)
                }
            });
        }
    }
}
