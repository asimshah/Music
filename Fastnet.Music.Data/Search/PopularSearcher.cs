using Fastnet.Core;
using Fastnet.Music.Core;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Data
{
    public class PopularSearcher : CatalogueSearcher
    {
        protected override (bool prefixMode, IEnumerable<ISearchResult> searchResults) SearchCatalogue(string loweredSearch)
        {
            //loweredSearch = loweredSearch.Trim();
            var artists = GetMatchingArtists(loweredSearch).ToArray();
            var works = new WorkQueryResult[0];
            var tracks = new TrackQueryResult[0];
            if (!prefixMatch)
            {
                works = GetMatchingWorks(loweredSearch).ToArray();
                tracks = GetMatchingTracks(loweredSearch).ToArray();
            }
            var finalList = new List<PopularResult>();
            finalList.AddRange(artists.Select(a => new PopularResult { Artist = a.Artists.First(), ArtistIsMatched = true }));
            // eliminate works whose artists are already in the artists list (as all their works are already present)
            var artistKeys = finalList.Select(l => l.Artist.Key);// artists.Select(a => a.Artist.Key);
            var t1 = works.Where(w => artistKeys.Contains(w.Artist.Key));
            works = works.Except(t1).ToArray();
            // add remaining works as partially matched artists
            foreach (var w in works)
            {
                var sr = finalList.SingleOrDefault(x => x.Artist.Key == w.Artist.Key);
                if (sr == null)
                {
                    sr = new PopularResult
                    {
                        Artist = w.Artist,
                        ArtistIsMatched = false,
                        Works = new List<WorkResult>()
                    };
                    finalList.Add(sr);
                }
                sr.Works.Add(new WorkResult
                {
                    Work = w.Work,
                    WorkIsMatched = true
                });
            }
            // eliminate tracks where the work is already in the list, or the artist is already in the list
            var t2 = tracks.Where(t => artistKeys.Contains(t.Artist.Key));
            tracks = tracks.Except(t2).ToArray();
            var workKeys = finalList.Where(l => l.Works != null).SelectMany(l => l.Works.Select(w => w.Work.Key));
            t2 = tracks.Where(t => workKeys.Contains(t.Work.Key));
            tracks = tracks.Except(t2).ToArray();
            foreach (var t in tracks)
            {
                var sr = finalList.SingleOrDefault(x => x.Artist.Key == t.Artist.Key);
                if (sr == null)
                {
                    sr = new PopularResult
                    {
                        Artist = t.Artist,
                        ArtistIsMatched = false,
                        Works = new List<WorkResult>()
                    };
                    finalList.Add(sr);
                }
                var wr = sr.Works.SingleOrDefault(x => x.Work.Key == t.Work.Key);
                if (wr == null)
                {
                    wr = new WorkResult
                    {
                        Work = t.Work,
                        WorkIsMatched = false,
                        Tracks = new List<TrackResult>()
                    };
                    sr.Works.Add(wr);
                }
                wr.Tracks.Add(new TrackResult { Track = t.Track });

            }
            SortResults(finalList);
            return (prefixMatch, finalList);
        }
        /*protected */internal void SortResults(List<PopularResult> results)
        {
            results.Sort((left, right) =>
            {
                return string.Compare(left.Artist.Name, right.Artist.Name, true);
            });
            results.ForEach((sr) =>
            {
                if (sr.Works != null)
                {
                    sr.Works.Sort((left, right) =>
                    {
                        return string.Compare(left.Work.Name, right.Work.Name, true);
                    });
                    sr.Works.ForEach((wr) =>
                    {
                        if (wr.Tracks != null)
                        {
                            wr.Tracks.Sort((left, right) =>
                            {
                                var ln = left.Track.Number ?? 0;
                                var rn = right.Track.Number ?? 0;
                                if (ln > 0 && rn > 0)
                                {
                                    return ln.CompareTo(rn);
                                }
                                else
                                {
                                    return string.Compare(left.Track.Name, right.Track.Name);
                                }
                            });
                        }
                    });
                }
            });
        }
        private IEnumerable<WorkQueryResult> GetMatchingWorks(string loweredSearch)
        {
            var works = MusicDb.Works
                .Where(x1 => x1.StyleId == MusicStyle).ToArray();
            works = works.Where(x => x.AlphamericName.Contains(loweredSearch.ToAlphaNumerics()))
                .ToArray();
            return works.Select(x => new WorkQueryResult
            {
                Artist = new SearchKey
                {
                    //Key = x.Artist.Id,
                    //Name = x.Artist.Name

                    // **URGENT** this is not right as only the first artist is set as matching - but what is the alternative?
                    Key = x.Artists.First().Id,
                    Name = x.Artists.First().Name
                },
                Work = new SearchKey
                {
                    Key = x.Id,
                    Name = x.Name
                }
            });
        }
        private IEnumerable<TrackQueryResult> GetMatchingTracks(string loweredSearch)
        {
            var tracks = MusicDb.Tracks
                .Where(x1 => x1.Work.StyleId == MusicStyle).ToArray();;
            tracks = tracks.Where(x => x.AlphamericTitle.ToLower().Contains(loweredSearch.ToAlphaNumerics())).ToArray();
            return tracks.Select(x => new TrackQueryResult
            {
                Artist = new SearchKey
                {
                    //Key = x.Work.Artist.Id,
                    //Name = x.Work.Artist.Name

                    // **URGENT** this is not right as only the first artist is set as matching - but what is the alternative?
                    Key = x.Work.Artists.First().Id,
                    Name = x.Work.Artists.First().Name
                },
                Work = new SearchKey
                {
                    Key = x.Work.Id,
                    Name = x.Work.Name
                },
                Track = new TrackKey
                {
                    Key = x.Id,
                    Name = x.Title,
                    Number = (x.Work.Type == OpusType.Singles || x.Work.Type == OpusType.Collection ? new int?() : x.Number)
                }
            });
        }
    }
}
