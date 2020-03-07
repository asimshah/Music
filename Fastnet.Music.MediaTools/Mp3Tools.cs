using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;

namespace Fastnet.Music.MediaTools
{
    public class Mp3Tools
    {
        private string filePath = String.Empty;
        public TimeSpan Duration;
        public int SampleRate;
        public int MinimumBitRate;
        public int MaximumBitRate;
        public int BitsPerSample;
        public double AverageBitRate;
        public int Channels { get; set; }
        public Mp3Tools(string path)
        {
            this.filePath = path;
            Read();
        }

        private void Read()
        {
            using (var reader = new Mp3FileReader(this.filePath, false))
            {
                var ap = reader.GetAudioProperties();
                Duration = ap.Duration;
                SampleRate = ap.SampleRate;
                MinimumBitRate = ap.MinimumBitRate;
                MaximumBitRate = ap.MaximumBitRate;
                AverageBitRate = ap.AverageBitRate;
                BitsPerSample = ap.BitsPerSample;
                Channels = ap.Channels;

            }

        }
    }
}
