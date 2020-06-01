using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fastnet.Music.Data
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Note: tried to do this using IEquatable on ArtistSet (to avoid having this comaparer)
    /// but the Distinct call always failed somewhere inside Linq
    /// </summary>
    public class ArtistSetComparer : IEqualityComparer<ArtistSet>
    {
        public bool Equals(ArtistSet x, ArtistSet y)
        {
            //Debug.WriteLine($"comparing {x} with {y}");
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            if(x.ArtistIds.Count() != y.ArtistIds.Count())
            {
                return false;
            }
            for(int i = 0; i < x.ArtistIds.Count(); ++i)
            {
                var xid = x.ArtistIds.Skip(i).First();
                var yid = y.ArtistIds.Skip(i).First();
                //Debug.WriteLine($"comparing {xid} with {yid}");
                if (xid != yid)
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(ArtistSet obj)
        {
            //Debug.WriteLine($"getting hashcode for {obj}");            
            unchecked
            {
                int hash = 17;
                foreach (var n in obj.ArtistIds)
                {
                    hash = 31 * hash + n.GetHashCode();
                }
                return hash;
            }
        }

    }
    public class ArtistSet //: IEquatable<ArtistSet>

    {
        private IEnumerable<Artist> artists = Enumerable.Empty<Artist>();

        public IEnumerable<long> ArtistIds { get; } = Enumerable.Empty<long>();
        /// <summary>
        /// Only available if constructed using Artists (as opposed to Artist Ids)
        /// (this is a bit odd: is there a better way?)
        /// </summary>
        public IEnumerable<Artist> Artists
        {
            get
            {
                if(artists.Count() == 0)
                {
                    throw new Exception($"Artist Entities are not available. Check correct constructor was used");
                }
                return this.artists;
            }
            private set
            {
                this.artists = value;
            }
        }

        public ArtistSet(params long[] ids): this(ids.AsEnumerable())
        {
            //ArtistIds = ids.OrderBy(x => x);
        }
        public ArtistSet(IEnumerable<long> ids)
        {
            ArtistIds = ids.OrderBy(x => x);
        }
        public ArtistSet(params Artist[] artists) : this(artists.AsEnumerable())
        {
            //this.Artists = artists.OrderBy(x => x.Id);
        }
        public ArtistSet(IEnumerable<Artist> artists) : this(artists.Select(a => a.Id))
        {
            this.Artists = artists.OrderBy(x => x.Id);
        }
        public bool Matches(IEnumerable<long> ids)
        {
            return ArtistIds.SequenceEqual(ids.OrderBy(x => x));
        }
        public override string ToString()
        {
            return $"[{string.Join(", ", ArtistIds)}]";
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
