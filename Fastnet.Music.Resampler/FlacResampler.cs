using NAudio.Lame;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Threading.Tasks;

namespace Fastnet.Music.Resampler
{
    public class FlacResampler
    {
        public async Task Resample(string srcFile, string destFile)
        {

            var wavFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(destFile), $"{System.IO.Path.GetFileNameWithoutExtension(destFile)}.wav");
            if (System.IO.File.Exists(wavFile))
            {
                System.IO.File.Delete(wavFile);
            }
            using (var reader = new AudioFileReader(srcFile))
            {
                var resampler = new WdlResamplingSampleProvider(reader, 44100);
                WaveFileWriter.CreateWaveFile16(wavFile, resampler);
            }
            using (var reader = new WaveFileReader(wavFile))
            {
                if (System.IO.File.Exists(destFile))
                {
                    System.IO.File.Delete(destFile);
                }
                using (var writer = new LameMP3FileWriter(destFile, reader.WaveFormat, LAMEPreset.EXTREME))
                {
                    //reader.CopyTo(writer);
                    await reader.CopyToAsync(writer);
                    await writer.FlushAsync();
                }
            }
            System.IO.File.Delete(wavFile);
        }
    }
}
