using System.Collections.Generic;

namespace Fastnet.Music.Metatools
{
    public class WesternClassicalMusicTags : MusicTags
    {
        public string Composer { get; set; }
        public string Composition { get; set; }
        public string Orchestra { get; set; }
        public string Conductor { get; set; }
        public int MovementNumber { get; set; }
        public IEnumerable<string> Performers { get; set; }
    }
}
