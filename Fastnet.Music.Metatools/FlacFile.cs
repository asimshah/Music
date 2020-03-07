using Fastnet.Music.Core;
using Fastnet.Music.MediaTools;
using System.IO;

namespace Fastnet.Music.Metatools
{
    public class FlacFile : AudioFile
    {
        public FlacFile(MusicOptions musicOptions, MusicStyles style, FileInfo fi) : base(musicOptions, style, fi)
        {
        }

        public override AudioProperties GetAudioProperties()
        {
            using (var ft = new FlacTools(File.FullName))
            {
                var ap = new AudioProperties
                {
                    Duration = ft.GetDuration().TotalMilliseconds,
                    BitsPerSample = ft.GetBitsPerSample(),
                    SampleRate = (int)ft.StreamInfo.SampleRateHz,
                    Mode = ft.StreamInfo.Channels == 1 ? ChannelMode.Mono : ChannelMode.Stereo
                };
                return ap;
            }
        }
    }
}
