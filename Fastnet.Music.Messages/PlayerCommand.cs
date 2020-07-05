using Fastnet.Core;
using Fastnet.Music.Core;

namespace Fastnet.Music.Messages
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class PlayerCommand : MessageBase
    {
        public PlayerCommands Command { get; set; }
        public string DeviceKey { get; set; }
        public int CommandSequenceNumber { get; set; }
        public string StreamUrl { get; set; }
        public EncodingType EncodingType { get; set; }
        public float Position { get; set; } // percentage
        public float Volume { get; set; } // 0.0 - 1.0
        public override string ToString()
        {
            return $"{Command} [{DeviceKey}] position {Position.ToString("#0.0")} volume {Volume.ToString("0.0")} streamurl {StreamUrl}";
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
