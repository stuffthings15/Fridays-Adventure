using System;
using System.IO;
using System.Media;

namespace Fridays_Adventure.Audio
{
    /// <summary>
    /// Generates and plays synthesised sound effects entirely in memory —
    /// no audio files required.  Every sound is built once at startup from
    /// mathematical waveforms (sine chirps, arpeggios, AM synthesis) and
    /// stored as a standard PCM WAV byte array ready for SoundPlayer.
    /// </summary>
    internal static class ProceduralSfx
    {
        private const int   Rate  = 22050;
        private const float Pi2   = (float)(2.0 * Math.PI);

        // ── Pre-built sounds (generated once, reused every play) ─────────────
        public static readonly byte[] Jump      = BuildJump();
        public static readonly byte[] Berry     = BuildBerry();
        public static readonly byte[] Coin      = BuildCoin();
        public static readonly byte[] Stomp     = BuildStomp();
        public static readonly byte[] Attack    = BuildAttack();
        public static readonly byte[] Ice       = BuildIce();
        public static readonly byte[] Freeze    = BuildFreeze();
        public static readonly byte[] BreakWall = BuildBreakWall();
        public static readonly byte[] Hurt      = BuildHurt();
        public static readonly byte[] SeaStone  = BuildSeaStone();
        public static readonly byte[] Sink      = BuildSink();
        public static readonly byte[] Heal      = BuildHeal();
        public static readonly byte[] IntroAmbient = BuildIntroAmbient();

        // ── Playback ──────────────────────────────────────────────────────────
        /// <summary>Fire-and-forget: plays on a thread-pool thread so the
        /// game loop is never blocked.</summary>
        public static void Play(byte[] wav, float volume = 1f)
        {
            if (wav == null || volume <= 0f) return;
            byte[] data = volume < 0.99f ? ScaleVolume(wav, volume) : wav;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    using (var ms = new MemoryStream(data))
                    using (var sp = new SoundPlayer(ms))
                        sp.PlaySync();
                }
                catch { /* audio device unavailable — silently skip */ }
            });
        }

        // ── Volume helper ─────────────────────────────────────────────────────
        private static byte[] ScaleVolume(byte[] wav, float vol)
        {
            byte[] copy = (byte[])wav.Clone();
            for (int i = 44; i + 1 < copy.Length; i += 2)
            {
                short s       = (short)(copy[i] | (copy[i + 1] << 8));
                short scaled  = (short)(s * vol);
                copy[i]     = (byte)(scaled & 0xFF);
                copy[i + 1] = (byte)((scaled >> 8) & 0xFF);
            }
            return copy;
        }

        // ── Core synthesiser ──────────────────────────────────────────────────
        /// <summary>Synthesise <paramref name="ms"/> milliseconds of audio
        /// using <paramref name="fn"/>(t) where t is time in seconds.</summary>
        private static byte[] Synth(int ms, Func<float, float> fn)
        {
            int n     = Rate * ms / 1000;
            short[] s = new short[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float v = fn(t);
                s[i] = (short)(Math.Max(-1f, Math.Min(1f, v)) * 32767);
            }
            return Wav(s);
        }

        /// <summary>Multi-note arpeggio: each pitch plays for
        /// <paramref name="noteMs"/> ms with exponential decay.</summary>
        private static byte[] Arpeggio(float[] freqs, int noteMs, float amp)
        {
            int     spn = Rate * noteMs / 1000;
            short[] buf = new short[freqs.Length * spn];
            for (int n = 0; n < freqs.Length; n++)
                for (int i = 0; i < spn; i++)
                {
                    float t   = (float)i / Rate;
                    float env = (float)Math.Exp(-5.0 * i / spn);
                    float val = (float)Math.Sin(Pi2 * freqs[n] * t) * env * amp;
                    buf[n * spn + i] =
                        (short)(Math.Max(-1f, Math.Min(1f, val)) * 32767);
                }
            return Wav(buf);
        }

        // ── WAV header builder ────────────────────────────────────────────────
        private static byte[] Wav(short[] samples)
        {
            int db   = samples.Length * 2;
            byte[] b = new byte[44 + db];

            W4(b,  0, "RIFF");  Wi(b,  4, 36 + db); W4(b, 8, "WAVE");
            W4(b, 12, "fmt ");  Wi(b, 16, 16);
            Ws(b, 20, 1);       Ws(b, 22, 1);          // PCM, Mono
            Wi(b, 24, Rate);    Wi(b, 28, Rate * 2);
            Ws(b, 32, 2);       Ws(b, 34, 16);          // BlockAlign, BitsPerSample
            W4(b, 36, "data");  Wi(b, 40, db);

            for (int i = 0; i < samples.Length; i++)
            {
                b[44 + i * 2]     = (byte)(samples[i] & 0xFF);
                b[44 + i * 2 + 1] = (byte)((samples[i] >> 8) & 0xFF);
            }
            return b;
        }

        private static void W4(byte[] b, int o, string s)
            { for (int i = 0; i < 4; i++) b[o + i] = (byte)s[i]; }
        private static void Wi(byte[] b, int o, int v)
            { BitConverter.GetBytes(v).CopyTo(b, o); }
        private static void Ws(byte[] b, int o, short v)
            { BitConverter.GetBytes(v).CopyTo(b, o); }

        // ── Waveform primitives ───────────────────────────────────────────────
        // Linear-frequency chirp from f0 → f1 over duration T
        private static float Chirp(float f0, float f1, float T, float t)
            => (float)Math.Sin(Pi2 * (f0 * t + (f1 - f0) / (2f * T) * t * t));

        // Pure sine at hz
        private static float Sin(float hz, float t)
            => (float)Math.Sin(Pi2 * hz * t);

        // Exponential decay: 1 → ~0 over T seconds
        private static float Env(float t, float T)
            => (float)Math.Exp(-5.0 * t / T);

        // ── Individual sound designs ──────────────────────────────────────────

        // Jump — rising chirp 200 → 700 Hz, quick decay ("boing")
        private static byte[] BuildJump()
            => Synth(85, t => Chirp(200f, 700f, 0.085f, t) * Env(t, 0.085f));

        // Berry collect — SMB3-style ka-ching: metallic ascending arpeggio with harmonics
        private static byte[] BuildBerry()
        {
            float[] freqs = { 988f, 1319f, 1568f, 2093f };   // B5 E6 G6 C7
            const int noteMs = 38;
            int   spn = Rate * noteMs / 1000;
            short[] buf = new short[freqs.Length * spn];
            for (int n = 0; n < freqs.Length; n++)
                for (int i = 0; i < spn; i++)
                {
                    float t   = (float)i / Rate;
                    float env = (float)Math.Exp(-9.0 * i / spn);
                    float val = ((float)Math.Sin(Pi2 * freqs[n] * t)
                               + 0.35f * (float)Math.Sin(Pi2 * freqs[n] * 2 * t)
                               + 0.12f * (float)Math.Sin(Pi2 * freqs[n] * 3 * t))
                               * env * 0.78f;
                    buf[n * spn + i] = (short)(Math.Max(-1f, Math.Min(1f, val)) * 32767);
                }
            return Wav(buf);
        }

        // Coin collect — 3-note arpeggio, slightly lower than berry
        private static byte[] BuildCoin()
            => Arpeggio(new[] { 392f, 523f, 659f }, 38, 0.75f);

        // Health pickup — warm ascending triad (C5 E5 G5)
        private static byte[] BuildHeal()
            => Arpeggio(new[] { 523f, 659f, 784f }, 55, 0.62f);

        // Stomp — descending chirp 200 → 60 Hz + sub-bass sine ("thump")
        private static byte[] BuildStomp()
            => Synth(95, t =>
                (Chirp(200f, 60f, 0.095f, t) + 0.4f * Sin(75f, t)) * Env(t, 0.095f));

        // Attack — two harmonics 220 + 440 Hz ("thwack")
        private static byte[] BuildAttack()
            => Synth(70, t =>
                (Sin(220f, t) + 0.5f * Sin(440f, t)) * Env(t, 0.07f));

        // Ice wall — ascending crystalline arpeggio
        private static byte[] BuildIce()
            => Arpeggio(new[] { 880f, 1100f, 1320f, 1760f }, 38, 0.55f);

        // Flash freeze — descending chirp 900 → 180 Hz ("whoomp")
        private static byte[] BuildFreeze()
            => Synth(230, t => Chirp(900f, 180f, 0.23f, t) * Env(t, 0.23f) * 0.8f);

        // Break wall — low bass cluster 55 + 80 + 160 Hz ("BOOM")
        private static byte[] BuildBreakWall()
            => Synth(140, t =>
                (Sin(80f, t) + Sin(55f, t) + 0.4f * Sin(160f, t))
                * Env(t, 0.14f) * 0.6f);

        // Hurt — descending chirp 420 → 110 Hz ("ouch")
        private static byte[] BuildHurt()
            => Synth(210, t => Chirp(420f, 110f, 0.21f, t) * Env(t, 0.21f));

        // Sea stone — two close frequencies create a 6 Hz beating drone (eerie)
        private static byte[] BuildSeaStone()
            => Synth(400, t =>
                (Sin(88f, t) + Sin(94f, t)) * 0.45f * Env(t, 0.40f));

        // Sink — amplitude-modulated low sine mimics bubbling water
        private static byte[] BuildSink()
            => Synth(300, t =>
                Sin(90f, t)
                * (float)Math.Abs(Math.Sin(Pi2 * 8f * t))
                * Env(t, 0.30f));

        // Intro ambient — 8-second loopable ocean-adventure pad (A minor)
        // All frequencies are integer multiples of 0.125 Hz (= 1/8 Hz), so every
        // voice completes an exact integer number of cycles in 8 seconds.  The
        // 0.5-second fade-in/out at the loop boundary eliminates any residual click.
        private static byte[] BuildIntroAmbient()
        {
            const int   DurMs   = 8000;
            const float DurSec  = 8f;
            const float FadeLen = 0.5f;

            return Synth(DurMs, t =>
            {
                // Fade at loop boundaries so SoundPlayer.PlayLooping() restarts silently
                float fade = t < FadeLen         ? t / FadeLen
                           : t > DurSec - FadeLen ? (DurSec - t) / FadeLen
                           : 1f;

                // Volume swell: 0.125 Hz = exactly one full cycle in 8 s
                // sin(2π * 0.125 * 0) = sin(2π * 0.125 * 8) = 0 → same level at loop point
                float swell = 0.70f + 0.30f * Sin(0.125f, t);

                // Ambient pad — open-fifth power chord (A2 + E3 + A3)
                float pad = 0.28f * Sin(110f, t)   // A2  (880 cycles in 8 s)
                          + 0.20f * Sin(165f, t)   // E3  (1320)
                          + 0.14f * Sin(220f, t);  // A3  (1760)

                // Melodic pluck — one note per 2 seconds, 50 ms attack + natural decay
                float noteT   = t % 2f;
                float noteEnv = (float)Math.Exp(-2.5f * noteT)
                              * Math.Min(1f, noteT / 0.05f);
                float[] mel   = { 264f, 330f, 264f, 220f };  // C4 E4 C4 A3
                float melody  = 0.18f * Sin(mel[(int)(t / 2f) % 4], t) * noteEnv;

                return (pad + melody) * swell * fade * 0.58f;
            });
        }
    }
}
