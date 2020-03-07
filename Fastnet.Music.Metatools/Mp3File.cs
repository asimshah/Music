using Fastnet.Music.Core;
using Fastnet.Music.MediaTools;
using System.IO;

namespace Fastnet.Music.Metatools
{
    public class Mp3File : AudioFile
    {
        public Mp3File(MusicOptions musicOptions, MusicStyles style, FileInfo fi) : base(musicOptions, style, fi)
        {
        }
        public override AudioProperties GetAudioProperties()
        {
            var mt = new Mp3Tools(File.FullName);
            var ap = new AudioProperties
            {
                Duration = mt.Duration.TotalMilliseconds,
                AverageBitRate = mt.AverageBitRate,
                MaximumBitRate = mt.MaximumBitRate,
                MinimumBitRate = mt.MinimumBitRate,
                BitsPerSample = mt.BitsPerSample,
                SampleRate = mt.SampleRate,
                Mode = mt.Channels == 1 ? ChannelMode.Mono : ChannelMode.Stereo
            };
            return ap;
        }
    }
}
