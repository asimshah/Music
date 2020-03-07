using Fastnet.Music.Core;

namespace Fastnet.Music.Metatools
{
    public class AudioProperties
    {
        public ChannelMode Mode { get; set; }
        public double Duration { get; set; }
        public int BitsPerSample { get; set; }
        public int SampleRate { get; set; }
        public int? MinimumBitRate { get; set; } = null;
        public int? MaximumBitRate { get; set; } = null;
        public double? AverageBitRate { get; set; } = null;
    }
}
