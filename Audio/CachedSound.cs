using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Fridays_Adventure.Audio
{
    /// <summary>
    /// A WAV file decoded into memory as IEEE-float samples at a fixed target format.
    /// Loading once at startup means zero disk I/O and zero decoding cost at play-time.
    /// </summary>
    internal sealed class CachedSound
    {
        public readonly float[]    AudioData;
        public readonly WaveFormat WaveFormat;

        public CachedSound(string filePath, WaveFormat targetFormat)
        {
            using (var reader = new AudioFileReader(filePath))
            {
                ISampleProvider source = reader;

                // Mono → stereo
                if (reader.WaveFormat.Channels == 1 && targetFormat.Channels == 2)
                    source = new MonoToStereoSampleProvider(source);

                // Resample to target rate when needed
                if (source.WaveFormat.SampleRate != targetFormat.SampleRate)
                    source = new WdlResamplingSampleProvider(source, targetFormat.SampleRate);

                var samples = new List<float>();
                var buf     = new float[4096];
                int read;
                while ((read = source.Read(buf, 0, buf.Length)) > 0)
                    for (int i = 0; i < read; i++) samples.Add(buf[i]);

                AudioData  = samples.ToArray();
                WaveFormat = targetFormat;
            }
        }
    }

    /// <summary>
    /// Feeds one play-through of a <see cref="CachedSound"/> into a mixer.
    /// Finished when all samples have been read.
    /// </summary>
    internal sealed class CachedSoundSampleProvider : ISampleProvider
    {
        private readonly CachedSound _sound;
        private readonly float       _volume;
        private int                  _position;

        public CachedSoundSampleProvider(CachedSound sound, float volume)
        {
            _sound  = sound;
            _volume = volume;
        }

        public WaveFormat WaveFormat => _sound.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int remaining = _sound.AudioData.Length - _position;
            int toRead    = remaining < count ? remaining : count;
            for (int i = 0; i < toRead; i++)
                buffer[offset + i] = _sound.AudioData[_position + i] * _volume;
            _position += toRead;
            return toRead;
        }
    }
}
