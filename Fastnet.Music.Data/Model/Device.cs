using Fastnet.Music.Core;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastnet.Music.Data
{
    public class Device : EntityBase // IIdentifier
    {
        public override long Id { get; set; }
        public string KeyName { get; set; }
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public AudioDeviceType Type { get; set; }
        public string HostMachine { get; set; }
        public string PlayerUrl { get; set; }
        /// <summary>
        /// MAC address for logitech, some string id for others
        /// </summary>
        public string MACAddress { get; set; }
        public bool IsDefaultOnHost { get; set; }
        public bool IsDisabled { get; set; }
        public bool CanReposition { get; set; }
        public int MaxSampleRate { get; set; }
        public DateTimeOffset LastSeenDateTime { get; set; }
        public float Volume { get; set; } // range 0.0% to 100.0% 
        public long? PlaylistId { get; set; }
        public virtual Playlist Playlist { get; set; }
        [NotMapped]
        public string ConnectionId { get; set; }
        [NotMapped]
        public bool IsAvailable
        {
            get
            {
                return !IsDisabled && (DateTimeOffset.Now - LastSeenDateTime) < TimeSpan.FromSeconds(60);
            }
        }
        public override string ToString()
        {
            return $"{ToIdent()} {DisplayName} (key: {KeyName}) [phys: {MACAddress} on {HostMachine}]";
        }
    }

}
