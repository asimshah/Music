using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    public abstract class BaseCatalogueResult
    {
        //private IMusicSet set;

        public BaseMusicSet MusicSet { get; private set; }
        public Type MusicSetType  => MusicSet.GetType();
        public CatalogueStatus Status { get; private set; }
        public string ArtistDescr { get; set; } = String.Empty;
        public string ArtistNames { get; set; } = String.Empty;
        public List<long> ArtistIdListForNotification { get; set; } = new List<long>();
        public BaseCatalogueResult(BaseMusicSet set, CatalogueStatus status)
        {
            MusicSet = set;
            Status = status;
            //SetArtists(artists);
        }

        protected void SetArtists(IEnumerable<Artist> artists)
        {
            ArtistIdListForNotification.AddRange(artists.Where(a => a.Type != ArtistType.Various).Select(a => a.Id));
            ArtistDescr = string.Join(string.Empty, artists.Select(x => x.ToIdent())).Replace("][", ",");
            ArtistNames = artists.Select(x => x.Name).ToCSV();
        }
        protected void SetArtist(Artist artist)
        {
            if (artist.Type != ArtistType.Various)
            {
                ArtistIdListForNotification.Add(artist.Id);
            }
            ArtistDescr = artist.ToIdent();
            ArtistNames = artist.Name;
        }
    }
}
