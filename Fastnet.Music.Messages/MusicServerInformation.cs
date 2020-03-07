using Fastnet.Core;

namespace Fastnet.Music.Messages
{
    /// <summary>
    /// this is the info about the music server (i.e. the (single) music web site on the LAN
    /// </summary>
    public class MusicServerInformation : MessageBase
    {
        /// <summary>
        /// url to talk to the music server
        /// </summary>
        public string Url { get; set; }
    }
}
