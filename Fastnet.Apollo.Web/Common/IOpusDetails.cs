using System.Collections.Generic;

namespace Fastnet.Apollo.Web
{
    public interface IOpusDetails
    {
        long Id { get; /*set;*/ }
        string ArtistName { get; }
        string CompressedArtistName { get; }
        string OpusName { get; /*set; */}
        string CompressedOpusName { get; }
        IEnumerable<TrackDetail> TrackDetails { get; /*set;*/ }
    }
}