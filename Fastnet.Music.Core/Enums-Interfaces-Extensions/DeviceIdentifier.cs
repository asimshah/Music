using System;

namespace Fastnet.Music.Core
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [Obsolete]
    public class DeviceIdentifier
    {
        public long DeviceId { get; set; } // pk of Device in the music database
        public string HostMachine { get; set; }
        public string MACAddress { get; set; } // used for Logitech
        public AudioDeviceType Type { get; set; }
        public string DeviceName { get; set; } // Name (not DisplayName) of Device in the music database
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
