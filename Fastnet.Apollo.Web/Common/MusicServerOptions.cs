using Fastnet.Music.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public class DeviceDisplayName
    {
        public string MACAddress { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool DisableOnCreate { get; set; }
    }
    public class MusicServerOptions
    {
        public int Port { get; set; }
        public PlayIntervals Intervals { get; set; }
        public int CompactLayoutWidth { get; set; }
        public int KeepAliveInterval { get; set; } // set 0 to switch off keep alive
        public DeviceDisplayName[] DisplayNames { get; set; }
        /// <summary>
        /// a number of ServerInformationBroadcastIntervals (of which the default is 10000ms), e.g 30, ie. 6 * 5, for every five minutes
        /// </summary>
        public int MulticastReportInterval { get; set; }
        public MusicServerOptions()
        {
            Intervals = new PlayIntervals();
            KeepAliveInterval = 20 * 1000;
            DisplayNames = new DeviceDisplayName[0];
            MulticastReportInterval = 6 * 5; //every 5 minutes
        }
    }
}
