using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

namespace Fridays_Adventure.Audio
{
    /// <summary>
    /// Music and SFX manager backed by NAudio (WaveOutEvent + AudioFileReader).
    /// NAudio streams MP3 through a properly-sized ring buffer on a dedicated audio
    /// thread, eliminating the stutter and inter-track gaps that mciSendString caused.
    /// </summary>
    public sealed class AudioManager
    {
        // ── Playlists (mood → ordered list of filenames) ─────────────────────
        private readonly Dictionary<string, List<string>> _playlists =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "overworld", new List<string> { "music_overworld1.mp3", "music_grandlinefog1.mp3" } },
                { "combat",    new List<string> { "music_combat2.mp3",    "music_combat1.mp3"    } },
                { "island",    new List<string> { "music_island2.mp3",    "music_island1.mp3"    } },
                { "boss",      new List<string> { "music_boss2.mp3",      "music_boss1.mp3"      } },
            };

        private static readonly Dictionary<string, string> _firstTracks =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "overworld", "music_overworld1.mp3" },
                { "combat",    "music_combat2.mp3"    },
                { "island",    "music_island2.mp3"    },
                { "boss",      "music_boss2.mp3"      },
            };

        private readonly Random _rng       = new Random();
        private string          _currentMood;
        private string          _lastPlayed;
        private string          _currentTrack;   // filename, kept for API compat

        // ── NAudio objects (main-thread owned; disposed before reassignment) ──
        private WaveOutEvent    _waveOut;
        private AudioFileReader _musicReader;

        // ── Thread-safe end-of-track signal ──────────────────────────────────
        // NAudio fires PlaybackStopped on its audio thread; we set this flag and
        // consume it on the game-loop thread inside Tick() — no locks needed
        // because reads/writes of bool are atomic on x86/x64.
        private volatile bool _trackEndedSignal;

        // ── Volume ───────────────────────────────────────────────────────────
        private int  _musicVolume  = 80;   // 0–100
        private int  _sfxVolume    = 80;
        private bool _musicEnabled = true;
        private bool _sfxEnabled   = true;

        private static string AudioPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio");

        // ── Public accessors (unchanged surface API) ──────────────────────────
        public int    MusicVolume     => _musicVolume;
        public int    SfxVolume       => _sfxVolume;
        public bool   MusicEnabled    => _musicEnabled;
        public bool   SfxEnabled      => _sfxEnabled;
        public string CurrentMood     => _currentMood;
        public bool   AudioSystemReady => true;   // NAudio needs no pre-warm

        public IReadOnlyList<string> GetPlaylist(string mood)
        {
            if (_playlists.TryGetValue(mood, out var list)) return list;
            return new List<string>();
        }

        public bool IsTrackInPlaylist(string mood, string file)
        {
            if (!_playlists.TryGetValue(mood, out var list)) return false;
            return list.Contains(file);
        }

        public IEnumerable<string> MoodNames => _playlists.Keys;

        public void AddTrack(string mood, string fileName)
        {
            if (!_playlists.ContainsKey(mood))
                _playlists[mood] = new List<string>();
            if (!_playlists[mood].Contains(fileName))
                _playlists[mood].Add(fileName);
        }

        public bool RemoveTrack(string mood, string fileName)
        {
            if (!_playlists.TryGetValue(mood, out var list)) return false;
            if (list.Count <= 1) return false;
            return list.Remove(fileName);
        }

        public void ApplySavedPlaylists(Dictionary<string, string> saved)
        {
            foreach (var kv in saved)
            {
                if (string.IsNullOrEmpty(kv.Value)) continue;
                var tracks = kv.Value.Split(',');
                if (tracks.Length == 0) continue;
                if (!_playlists.ContainsKey(kv.Key))
                    _playlists[kv.Key] = new List<string>();
                _playlists[kv.Key].Clear();
                foreach (string t in tracks)
                    if (!string.IsNullOrEmpty(t)) _playlists[kv.Key].Add(t);
                EnsureLyricalFirst(kv.Key);
            }
        }

        private void EnsureLyricalFirst(string mood)
        {
            if (!_playlists.TryGetValue(mood, out var list)) return;
            if (!_firstTracks.TryGetValue(mood, out string first)) return;
            if (!list.Contains(first))
                list.Insert(0, first);
            else if (list[0] != first)
            { list.Remove(first); list.Insert(0, first); }
        }

        // No-ops kept so callers don't need changes
        public void LoadAll() { }
        public void Prewarm() { }

        // ── Game-loop tick ────────────────────────────────────────────────────
        /// <summary>Called each frame. Advances to the next track when one ends.</summary>
        public void Tick(float dt)
        {
            if (!_trackEndedSignal) return;
            _trackEndedSignal = false;
            if (_musicEnabled && _currentMood != null)
                PlayMood(_currentMood);
        }

        // ── Music playback ────────────────────────────────────────────────────
        public void PlayMood(string mood)
        {
            if (!_musicEnabled) return;
            if (_currentMood == mood && IsPlaying()) return;
            _currentMood = mood;
            string track = PickTrack(mood);
            if (track != null) PlayFile(track);
        }

        private bool IsPlaying() =>
            _waveOut != null && _waveOut.PlaybackState == PlaybackState.Playing;

        public void PlayMusic(string fileName, bool loop = false)
        {
            if (!_musicEnabled) return;
            PlayFile(fileName);
        }

        private string PickTrack(string mood)
        {
            if (!_playlists.TryGetValue(mood, out var list) || list.Count == 0) return null;
            if (list.Count == 1) return list[0];
            if (_lastPlayed == null) return list[0];
            string pick;
            int attempts = 0;
            do { pick = list[_rng.Next(list.Count)]; attempts++; }
            while (pick == _lastPlayed && attempts < 10);
            return pick;
        }

        private void PlayFile(string fileName)
        {
            DisposePlayer();                 // stop & release current track first

            string path = Path.Combine(AudioPath, fileName);
            if (!File.Exists(path)) return;

            _lastPlayed   = fileName;
            _currentTrack = fileName;

            try
            {
                _musicReader = new AudioFileReader(path)
                {
                    Volume = _musicEnabled ? _musicVolume / 100f : 0f
                };

                // DesiredLatency 200 ms: buffer large enough to survive GC pauses
                // without audible stutter, small enough for responsive Stop().
                _waveOut = new WaveOutEvent { DesiredLatency = 200 };
                _waveOut.PlaybackStopped += OnPlaybackStopped;
                _waveOut.Init(_musicReader);
                _waveOut.Play();
            }
            catch
            {
                DisposePlayer();
            }
        }

        /// <summary>
        /// Called on NAudio's internal audio thread when a track finishes.
        /// Only sets a volatile flag — no heap allocation, no lock.
        /// </summary>
        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            _trackEndedSignal = true;
        }

        public void StopMusic()
        {
            DisposePlayer();
            _trackEndedSignal = false;
            _currentTrack     = null;
        }

        private void DisposePlayer()
        {
            if (_waveOut != null)
            {
                _waveOut.PlaybackStopped -= OnPlaybackStopped; // unsubscribe before Stop
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }
            if (_musicReader != null)
            {
                _musicReader.Dispose();
                _musicReader = null;
            }
        }

        // ── Volume ────────────────────────────────────────────────────────────
        public void SetMusicVolume(int pct)
        {
            _musicVolume = Math.Max(0, Math.Min(100, pct));
            ApplyMusicVolume();
        }

        public void SetSfxVolume(int pct)
        {
            _sfxVolume = Math.Max(0, Math.Min(100, pct));
        }

        private void ApplyMusicVolume()
        {
            if (_musicReader != null)
                _musicReader.Volume = _musicEnabled ? _musicVolume / 100f : 0f;
        }

        public void SetMusicEnabled(bool on)
        {
            _musicEnabled = on;
            if (!on) StopMusic();
            else     ApplyMusicVolume();
        }

        public void SetSfxEnabled(bool on) => _sfxEnabled = on;

        // ── Named-mood shortcuts ──────────────────────────────────────────────
        public void PlayOverworld() => PlayMood("overworld");
        public void PlayCombat()    => PlayMood("combat");
        public void PlayIsland()    => PlayMood("island");
        public void PlayBoss()      => PlayMood("boss");

        // ── SFX file playback (unchanged) ────────────────────────────────────
        public void PlaySfx(string fileName)
        {
            if (!_sfxEnabled) return;
            string path = Path.Combine(AudioPath, fileName);
            if (!File.Exists(path)) return;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    using (var sp = new System.Media.SoundPlayer(path))
                        sp.PlaySync();
                }
                catch { }
            });
        }

        // ── Procedural SFX ────────────────────────────────────────────────────
        public void BeepJump()     => ProceduralSfx.Play(ProceduralSfx.Jump,      _sfxVolume / 100f);
        public void BeepAttack()   => ProceduralSfx.Play(ProceduralSfx.Attack,    _sfxVolume / 100f);
        public void BeepIce()      => ProceduralSfx.Play(ProceduralSfx.Ice,       _sfxVolume / 100f);
        public void BeepHurt()     => ProceduralSfx.Play(ProceduralSfx.Hurt,      _sfxVolume / 100f);
        public void BeepFreeze()   => ProceduralSfx.Play(ProceduralSfx.Freeze,    _sfxVolume / 100f);
        public void BeepSink()     => ProceduralSfx.Play(ProceduralSfx.Sink,      _sfxVolume / 100f);
        public void BeepBreak()    => ProceduralSfx.Play(ProceduralSfx.BreakWall, _sfxVolume / 100f);
        public void BeepStomp()    => ProceduralSfx.Play(ProceduralSfx.Stomp,     _sfxVolume / 100f);
        public void BeepCoin()     => ProceduralSfx.Play(ProceduralSfx.Coin,      _sfxVolume / 100f);
        public void BeepBerry()    => ProceduralSfx.Play(ProceduralSfx.Berry,     _sfxVolume / 100f);
        public void BeepHeal()     => ProceduralSfx.Play(ProceduralSfx.Heal,      _sfxVolume / 100f);
        public void BeepSeaStone() => ProceduralSfx.Play(ProceduralSfx.SeaStone,  _sfxVolume / 100f);
    }
}
