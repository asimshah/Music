using Fastnet.Music.Data;
using System.Collections.Generic;

namespace Fastnet.Apollo.Web
{
    /// <summary>
    /// run time playlist info - i.e. not dependent on MusicDb instance
    /// </summary>
    public class PlaylistRuntime
    {
        public long Id { get; set; }
        public PlaylistType Type { get; set; }
        public string Name { get; set; }
        public List<PlaylistItemRuntime> Items { get; set; }
        public override string ToString()
        {
            if(Type == PlaylistType.DeviceList)
            {
                return $"(automatic device playlist)";
            }
            return $"{Type}, {Name}";
        }
    }
}
