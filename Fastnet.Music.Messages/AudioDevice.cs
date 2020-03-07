using Fastnet.Core;
using Fastnet.Music.Core;
using System;

namespace Fastnet.Music.Messages
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class AudioCapability
    {
        /// <summary>
        /// Hz * 1000, 0 if no max
        /// </summary>
        public int MaxSampleRate { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class AudioDevice : MessageBase
    {
        /// <summary>
        /// primary key in music database
        /// </summary>
        //[JsonProperty(PropertyName = "id")]
        public long Id { get; set; }
        /// <summary>
        /// string key that uniquely identifies the device (a guid in practice)
        /// </summary>
        //[JsonProperty(PropertyName = "key")]
        public string Key { get; set; }
        /// <summary>
        /// 
        /// </summary>
        //[JsonProperty(PropertyName = "type")]
        public AudioDeviceType Type { get; set; }
        /// <summary>
        /// Allows a user to enable of disable an audio device
        /// </summary>
        //[JsonProperty(PropertyName = "enabled")]
        public bool Enabled { get; set; }
        /// <summary>
        /// Friendly name for the device
        /// </summary>
        //[JsonProperty(PropertyName = "displayName")]
        public string DisplayName { get; set; }
        /// <summary>
        /// system name for the device
        /// </summary>
        //[JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        /// <summary>
        /// MACAddress if required (e.g. for Logitech devices)
        /// </summary>
        //[JsonProperty(PropertyName = "macAddress")]
        public string MACAddress { get; set; }
        /// <summary>
        /// Url for the Music player agent for this device
        /// </summary>
        //[JsonProperty(PropertyName = "url")]
        public string Url { get; set; }
        /// <summary>
        /// possibly not used???
        /// </summary>
        //[JsonProperty(PropertyName = "isDefault")]
        public bool IsDefault { get; set; }
        /// <summary>
        /// possibly not used???
        /// </summary>
        //[JsonProperty(PropertyName = "capability")]
        public AudioCapability Capability { get; set; }
        /// <summary>
        /// machine name for the computer which has the device (or from which an Apollo agent controls LMS)
        /// </summary>
        //[JsonProperty(PropertyName = "hostMachine")]
        public string HostMachine { get; set; }
        /// <summary>
        /// true, if the user can reposition music (not presently available for logitech devices)
        /// </summary>
        //[JsonProperty(PropertyName = "canReposition")]
        public bool CanReposition { get; set; }
        /// <summary>
        /// SignalR connection id, used only when the audio device type is Browser
        /// </summary>
        public string ConnectionId { get; set; }
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
            var y = obj as AudioDevice;
            if (!string.IsNullOrWhiteSpace(Key))
            {
                return Key.Equals(y.Key, StringComparison.InvariantCultureIgnoreCase);
            }
            return this.Name.Equals(y.Name, StringComparison.InvariantCultureIgnoreCase) && this.Type.Equals(y.Type);// && this.IsDefault.Equals(y.IsDefault);
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
                hash = hash * 31 + Name.GetHashCode();
                hash = hash * 31 + Type.GetHashCode();
                //hash = hash * 31 + IsDefault.GetHashCode();
                return hash;
            }
        }
        public override string ToString()
        {
            return $"{Type.ToString()}, {Name}, {MACAddress}";
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
