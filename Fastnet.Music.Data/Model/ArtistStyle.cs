using Fastnet.Music.Core;

namespace Fastnet.Music.Data
{
    /// <summary>
    /// Remember: a composer may be in WesternClassical as well as Opera
    /// </summary>
    public class ArtistStyle
    {
        public long ArtistId { get; set; }
        public virtual Artist Artist { get; set; }
        public MusicStyles StyleId { get; set; }
        //[Obsolete("remove Style")]
        //public virtual Style Style { get; set; }
    }
}
