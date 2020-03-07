using Fastnet.Core;
using Fastnet.Music.Core;

namespace Fastnet.Music.Messages
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class PlayerCommand : MessageBase
    {
        public PlayerCommands Command { get; set; }
        //[Obsolete]
        //public DeviceIdentifier Identifier { get; set; }
        public string DeviceKey { get; set; }
        public int CommandSequenceNumber { get; set; }
        public string StreamUrl { get; set; }
        public EncodingType EncodingType { get; set; }
        //public string MusicFileUid { get; set; }
        //public long MusicFileId { get; set; }
        //public long PlaylistId { get; set; }
        //public long PlaylistItemId { get; set; }
        //public long PlaylistSubItemId { get; set; }
        public float Position { get; set; } // percentage
        public float Volume { get; set; } // 0.0 - 1.0
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
