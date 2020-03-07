using Fastnet.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Messages
{
    /// <summary>
    /// this is info about each web player (i.e. a web player site on the LAN of which there can be many)
    /// </summary>
    public class PlayerInformation : MessageBase
    {
        /// <summary>
        /// url to talk to this web player
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<AudioDevice> AudioDevices { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public PlayerInformation()
        {
            AudioDevices = new AudioDevice[0];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var y = obj as PlayerInformation;
            bool r = MachineName.Equals(y.MachineName, StringComparison.CurrentCultureIgnoreCase) && Url.Equals(y.Url, StringComparison.CurrentCultureIgnoreCase);
            if (r)
            {
                if (AudioDevices.Count() == y.AudioDevices.Count())
                {
                    r = AudioDevices.Except(y.AudioDevices).Count() == 0;
                }
                else
                {
                    r = false;
                }
            }
            return r;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + MachineName.GetHashCode();
                hash = hash * 31 + Url.GetHashCode();
                foreach (var ad in AudioDevices)
                {
                    hash = hash * 31 + ad.GetHashCode();
                }
                return hash;
            }
        }
    }
    //[Obsolete]
    //public class WebPlayerInformation : PlayerInformation
    //{

    //}
}
