using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Fridays_Adventure.Audio
{
    /// <summary>
    /// Plays pre-cached WAV sound effects through a single low-latency NAudio
    /// WaveOutEvent + MixingSampleProvider so multiple sounds can overlap
    /// smoothly with no choppiness or per-call disk I/O.
    /// </summary>
    internal static class ProceduralSfx
    {
        // ── Sound name constants ─────────────────────────────────────────────
        public const string Jump           = "jump";
        public const string Berry          = "berry";
        public const string Coin           = "coin";
        public const string Stomp          = "stomp";
        public const string Attack         = "attack";
        public const string Ice            = "ice";
        public const string Freeze         = "freeze";
        public const string BreakWall      = "breakwall";
        public const string Hurt           = "hurt";
        public const string SeaStone       = "seastone";
        public const string Sink           = "sink";
        public const string Heal           = "heal";
        public const string IntroAmbient   = "introambient";
        public const string VictoryFanfare = "victoryfanfare";

        // ── SMB3 / Mega Man style SFX constants ──────────────────────────────
        /// <summary>Short sting played when a boss arena intro card slides in (Mega Man style).</summary>
        public const string BossIntro  = "boss_intro";
        /// <summary>Fanfare played when a boss is fully defeated.</summary>
        public const string BossDefeat = "boss_defeat";
        /// <summary>Short chime played when a level is cleared (SMB3 goal card style).</summary>
        public const string LevelClear = "levelclear";
        /// <summary>Power-up pickup sound (SMB3 item sound style).</summary>
        public const string Powerup    = "powerup";
        /// <summary>
        /// Short ascending arpeggio played on the SMB3-style level intro card.
        /// Team 1 (Game Director) — Idea 1: level entry card audio sting.
        /// </summary>
        public const string LevelIntro = "levelintro";

        // All SFX are normalised to this format so the mixer never has to
        // deal with mixed sample-rates or channel counts.
        private static readonly WaveFormat MixerFormat =
            WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

        private static readonly string SfxDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Assets", "SfxCache");

        private static readonly Dictionary<string, CachedSound> _cache =
            new Dictionary<string, CachedSound>(StringComparer.OrdinalIgnoreCase);

        private static MixingSampleProvider _mixer;
        private static WaveOutEvent         _waveOut;

        /// <summary>
        /// Pre-decodes all WAV files into memory and starts the mixer output.
        /// Call once at startup from AudioManager.LoadAll().
        /// </summary>
        public static void Preload()
        {
            _mixer   = new MixingSampleProvider(MixerFormat) { ReadFully = true };
            _waveOut = new WaveOutEvent { DesiredLatency = 75, NumberOfBuffers = 3 };
            _waveOut.Init(_mixer);
            _waveOut.Play();

            // Core gameplay SFX
            string[] all = { Jump, Berry, Coin, Stomp, Attack, Ice, Freeze,
                             BreakWall, Hurt, SeaStone, Sink, Heal,
                             IntroAmbient, VictoryFanfare,
                             // SMB3/Mega Man style SFX (graceful fallback if files are absent)
                             BossIntro, BossDefeat, LevelClear, Powerup };
            foreach (string name in all)
                EnsureLoaded(name);
        }

        private static void EnsureLoaded(string soundName)
        {
            if (_cache.ContainsKey(soundName)) return;
            string path = Path.Combine(SfxDir, soundName + ".wav");
            if (!File.Exists(path)) return;
            try { _cache[soundName] = new CachedSound(path, MixerFormat); }
            catch (Exception ex)
            {
                // Log the failure so SFX loading issues are visible in the debugger log.
                Systems.DebugLogger.LogError($"ProceduralSfx.EnsureLoaded({soundName})", ex);
            }
        }

        /// <summary>
        /// Submits a sound to the mixer — instant, non-blocking, supports overlap.
        /// </summary>
        public static void Play(string soundName, float volume = 1f)
        {
            if (string.IsNullOrEmpty(soundName) || volume <= 0f || _mixer == null) return;
            if (!_cache.TryGetValue(soundName, out CachedSound sound)) return;
            _mixer.AddMixerInput(new CachedSoundSampleProvider(sound, volume));
        }
    }
}
