
using Fastnet.Core;
using System;
using System.Collections.Generic;

namespace Fastnet.Music.Messages
{

    /// <summary>
    /// regular multicast from the music server destined for all participants
    /// </summary>
    [Obsolete]
    public class AllPointsBulletin : MessageBase
    {
        /// <summary>
        /// 
        /// </summary>
        public MusicServerInformation ServerInformation { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<PlayerInformation> WebPlayers { get; set; }
    }
}
