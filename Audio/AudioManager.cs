using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NAudio.Wave;

namespace Fridays_Adventure.Audio
{
    /// <summary>
    /// Music manager backed by NAudio WaveOutEvent for smooth, low-latency MP3
    /// playback. Playlist advancement is event-driven (PlaybackStopped) rather
    /// than polled so Tick() is a no-op kept only for API compatibility.
    /// </summary>
    public sealed class AudioManager
    {
        private WaveOutEvent   _musicOut;
        private AudioFileReader _musicReader;
        private readonly SynchronizationContext _syncCtx;

        private readonly Dictionary<string, List<string>> _playlists =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "overworld",   new List<string> { "music_overworld1.mp3"                                    } },
                { "combat",      new List<string> { "music_combat1.mp3",      "music_combat2.mp3"             } },
                { "island",      new List<string> { "music_island1.mp3",      "music_island2.mp3"             } },
                { "boss",        new List<string> { "music_boss1.mp3",        "music_boss2.mp3"               } },
                // ── Sequel moods ────────────────────────────────────────────────
                { "hub",         new List<string> { "music_hub1.mp3",         "music_hub2.mp3"                } },
                { "exploration", new List<string> { "music_exploration1.mp3", "music_exploration2.mp3"        } },
                { "event",       new List<string> { "music_event1.mp3",       "music_event2.mp3"              } },
                { "theme",       new List<string> { "music_theme1.mp3",       "music_theme2.mp3"              } },
                { "finale",      new List<string> { "music_finale1.mp3",      "music_finale2.mp3"             } },
            };

        private static readonly Dictionary<string, string> _firstTracks =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "overworld",   "music_overworld1.mp3"   },
                { "combat",      "music_combat1.mp3"      },
                { "island",      "music_island1.mp3"      },
                { "boss",        "music_boss1.mp3"        },
                // ── Sequel moods ────────────────────────────────────────────────
                { "hub",         "music_hub1.mp3"         },
                { "exploration", "music_exploration1.mp3"  },
                { "event",       "music_event1.mp3"       },
                { "theme",       "music_theme1.mp3"       },
                { "finale",      "music_finale1.mp3"      },
            };

        private readonly Random _rng = new Random((int)DateTime.Now.Ticks);
        private string _currentMood;
        private string _lastMood;
        private string _lastPlayed;
        private string _currentTrack;
        private bool   _isPlaying;
        private int  _musicVolume  = 80;
        private int  _sfxVolume    = 80;
        private bool _musicEnabled = true;
        private bool _sfxEnabled   = true;

        private static string AudioPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio");

        /// <summary>
        /// Root folder for lyrical MP3 tracks used at startup and during the
        /// every-2-level rotation. Separate from Assets\Audio so the two
        /// libraries stay clearly partitioned.
        /// </summary>
        private static string LyricalPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Music", "Lyrical");

        public int    MusicVolume      => _musicVolume;
        public int    SfxVolume        => _sfxVolume;
        public bool   MusicEnabled     => _musicEnabled;
        public bool   SfxEnabled       => _sfxEnabled;
        public string CurrentMood      => _currentMood;
        public string CurrentTrack     => _currentTrack;
        public bool   AudioSystemReady => true;  // NAudio WaveOutEvent is always available

        public AudioManager()
        {
            _syncCtx = SynchronizationContext.Current ?? new SynchronizationContext();
        }

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
                if (!_playlists.ContainsKey(kv.Key))
                    _playlists[kv.Key] = new List<string>();

                // Keep a fallback snapshot of built-in/default playlist for this mood.
                var fallback = new List<string>(_playlists[kv.Key]);

                var tracks = kv.Value.Split(',');
                _playlists[kv.Key].Clear();
                foreach (string raw in tracks)
                {
                    string t = (raw ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(t)) continue;

                    // Only keep tracks that still exist on disk.
                    string path = Path.Combine(AudioPath, t);
                    if (File.Exists(path))
                        _playlists[kv.Key].Add(t);
                }

                // If save data was stale/corrupted, restore defaults so music still plays.
                if (_playlists[kv.Key].Count == 0)
                    _playlists[kv.Key].AddRange(fallback);

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

        public void LoadAll() { ProceduralSfx.Preload(); }
        public void Prewarm() { }

        private void AdvancePlaylist()
        {
            _isPlaying    = false;
            _currentTrack = null;
            string mood   = _lastMood;
            _currentMood  = null;
            if (mood != null && _musicEnabled)
                PlayMood(mood);
        }

        // Playlist advancement is now driven by PlaybackStopped — Tick kept for API compatibility.
        public void Tick(float dt) { }

        public void PlayMood(string mood)
        {
            if (!_musicEnabled) return;
            if (_currentMood == mood && _isPlaying) return;
            _currentMood = mood;
            _lastMood    = mood;
            string track = PickTrack(mood);
            if (track != null) PlayFile(track);
        }

        public void PlayMusic(string fileName, bool loop = false)
        {
            if (!_musicEnabled) return;
            PlayFile(fileName);
        }

        private string PickTrack(string mood)
        {
            if (!_playlists.TryGetValue(mood, out var list) || list.Count == 0) return null;
            if (list.Count == 1) return list[0];
            string pick;
            int attempts = 0;
            do { pick = list[_rng.Next(list.Count)]; attempts++; }
            while (pick == _lastPlayed && attempts < 10);
            return pick;
        }

        private void PlayFile(string fileName)
        {
            // Dispose previous track before starting a new one
            StopMusic();

            string path = Path.Combine(AudioPath, fileName);
            if (!File.Exists(path)) return;

            try
            {
                _musicReader      = new AudioFileReader(path);
                _musicReader.Volume = _musicEnabled ? _musicVolume / 100f : 0f;
                _musicOut         = new WaveOutEvent { DesiredLatency = 200 };
                _musicOut.PlaybackStopped += OnPlaybackStopped;
                _musicOut.Init(_musicReader);
                _musicOut.Play();

                _lastPlayed   = fileName;
                _currentTrack = fileName;
                _isPlaying    = true;
            }
            catch (Exception ex)
            {
                // Log the failure so audio issues are visible in the debugger log.
                Systems.DebugLogger.LogError($"AudioManager.PlayFile({fileName})", ex);
                _musicOut?.Dispose();   _musicOut   = null;
                _musicReader?.Dispose(); _musicReader = null;
            }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            // Only auto-advance on natural end (not when we called StopMusic)
            if (!_isPlaying || e.Exception != null) return;
            // Marshal to the UI thread so AdvancePlaylist creates objects safely
            _syncCtx.Post(_ => AdvancePlaylist(), null);
        }

        public void StopMusic()
        {
            _isPlaying    = false;   // cleared before Stop() to suppress OnPlaybackStopped
            _currentTrack = null;

            var prevOut    = _musicOut;
            var prevReader = _musicReader;
            _musicOut    = null;
            _musicReader = null;

            if (prevOut != null)
            {
                // Unsubscribe before Stop() so the callback cannot re-enter
                // during the synchronous waveOutReset triggered by Stop().
                prevOut.PlaybackStopped -= OnPlaybackStopped;
                prevOut.Stop();
            }

            // Defer COM-backed disposal to a later message-pump cycle so the
            // NAudio callback thread that fired PlaybackStopped can finish
            // unwinding before the underlying RCW is released.
            if (prevOut != null || prevReader != null)
            {
                _syncCtx.Post(_ =>
                {
                    prevOut?.Dispose();
                    prevReader?.Dispose();
                }, null);
            }
        }

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
            else ApplyMusicVolume();
        }

        public void SetSfxEnabled(bool on) => _sfxEnabled = on;

        public void PlayOverworld()   => PlayMood("overworld");
        public void PlayCombat()      => PlayMood("combat");
        public void PlayIsland()      => PlayMood("island");
        public void PlayBoss()        => PlayMood("boss");
        // ── Sequel convenience methods ───────────────────────────────────────
        public void PlayHub()         => PlayMood("hub");
        public void PlayExploration() => PlayMood("exploration");
        public void PlayEvent()       => PlayMood("event");
        public void PlayTheme()       => PlayMood("theme");
        public void PlayFinale()      => PlayMood("finale");

        public void ContinueOrPlay(string mood)
        {
            if (!_musicEnabled) return;
            if (_isPlaying) return;
            PlayMood(mood);
        }

        public void PlaySpecificTrack(string mood, string fileName)
        {
            if (!_musicEnabled) return;
            _currentMood = mood;
            PlayFile(fileName);
        }

        private void Beep(string sound) { if (_sfxEnabled) ProceduralSfx.Play(sound, _sfxVolume / 100f); }

        public void BeepJump()           => Beep(ProceduralSfx.Jump);
        public void BeepAttack()         => Beep(ProceduralSfx.Attack);
        public void BeepIce()            => Beep(ProceduralSfx.Ice);
        public void BeepHurt()           => Beep(ProceduralSfx.Hurt);
        public void BeepFreeze()         => Beep(ProceduralSfx.Freeze);
        public void BeepSink()           => Beep(ProceduralSfx.Sink);
        public void BeepBreak()          => Beep(ProceduralSfx.BreakWall);
        public void BeepStomp()          => Beep(ProceduralSfx.Stomp);
        public void BeepCoin()           => Beep(ProceduralSfx.Coin);
        public void BeepBerry()          => Beep(ProceduralSfx.Berry);
        public void BeepHeal()           => Beep(ProceduralSfx.Heal);
        public void BeepSeaStone()       => Beep(ProceduralSfx.SeaStone);
        public void PlayVictoryFanfare() => Beep(ProceduralSfx.VictoryFanfare);
        public void PlayIntroAmbient()   => Beep(ProceduralSfx.IntroAmbient);
        /// <summary>Plays SMB3-style fireball launch with a fallback attack click layer.</summary>
        public void BeepFireball()       { Beep(ProceduralSfx.FireballShot); Beep(ProceduralSfx.Attack); }

        // ── SMB3 / Mega Man style SFX helpers ────────────────────────────────
        /// <summary>Plays the Mega Man-style boss intro sting.</summary>
        public void BeepBossIntro()  => Beep(ProceduralSfx.BossIntro);
        /// <summary>Plays the boss defeat fanfare.</summary>
        public void BeepBossDefeat() => Beep(ProceduralSfx.BossDefeat);
        /// <summary>Plays the SMB3-style level clear chime.</summary>
        public void BeepLevelClear() => Beep(ProceduralSfx.LevelClear);
        /// <summary>Plays the SMB3-style power-up pickup sound.</summary>
        public void BeepPowerup()    => Beep(ProceduralSfx.Powerup);
        /// <summary>
        /// Plays the SMB3-style world-entry jingle when the level intro card appears.
        /// Team 1 (Game Director) — Idea 1: level entry card with audio sting.
        /// </summary>
        public void BeepLevelIntro() => Beep(ProceduralSfx.LevelIntro);
    }
}
