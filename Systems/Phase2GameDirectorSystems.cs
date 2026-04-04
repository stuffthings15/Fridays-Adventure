// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 1: Game Director
// Feature: Director Systems Pack
// Purpose: Implements remaining Phase 2 Team 1 feature services.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Seasonal world theme resolver.
    /// </summary>
    /// <remarks>PHASE 2 - Team 1: Seasonal World Themes</remarks>
    public static class SeasonalWorldThemesSystem
    {
        /// <summary>Returns the active seasonal theme key for current UTC month.</summary>
        /// <remarks>PHASE 2 - Team 1: Seasonal World Themes</remarks>
        public static string CurrentTheme()
        {
            int m = DateTime.UtcNow.Month;
            if (m == 12 || m <= 2) return "winter_tide";
            if (m <= 5) return "spring_harbor";
            if (m <= 8) return "summer_storm";
            return "autumn_regatta";
        }
    }

    /// <summary>
    /// Speed-run timer service.
    /// </summary>
    /// <remarks>PHASE 2 - Team 1: Speed Run Timer</remarks>
    public static class SpeedRunTimerSystem
    {
        /// <summary>Current elapsed seconds while timer is active.</summary>
        public static float ElapsedSeconds { get; private set; }

        /// <summary>True when timer is actively running.</summary>
        public static bool Running { get; private set; }

        /// <summary>Starts or resumes speed-run timer.</summary>
        /// <remarks>PHASE 2 - Team 1: Speed Run Timer</remarks>
        public static void Start() => Running = true;

        /// <summary>Stops speed-run timer.</summary>
        /// <remarks>PHASE 2 - Team 1: Speed Run Timer</remarks>
        public static void Stop() => Running = false;

        /// <summary>Resets timer to zero and stops it.</summary>
        /// <remarks>PHASE 2 - Team 1: Speed Run Timer</remarks>
        public static void Reset()
        {
            ElapsedSeconds = 0f;
            Running = false;
        }

        /// <summary>Updates elapsed time when running.</summary>
        /// <remarks>PHASE 2 - Team 1: Speed Run Timer</remarks>
        public static void Tick(float dt)
        {
            if (!Running) return;
            ElapsedSeconds += Math.Max(0f, dt);
        }

        /// <summary>Returns timer formatted as mm:ss.fff.</summary>
        /// <remarks>PHASE 2 - Team 1: Speed Run Timer</remarks>
        public static string Formatted()
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0f, ElapsedSeconds));
            return ts.ToString(@"mm\:ss\.fff");
        }
    }

    /// <summary>
    /// Soundtrack mixer balance model.
    /// </summary>
    /// <remarks>PHASE 2 - Team 1: Soundtrack Mixer</remarks>
    public static class SoundtrackMixerSystem
    {
        /// <summary>Current mix profile key.</summary>
        public static string Profile { get; private set; } = "balanced";

        /// <summary>Sets soundtrack mix profile.</summary>
        /// <remarks>PHASE 2 - Team 1: Soundtrack Mixer</remarks>
        public static void SetProfile(string profile)
        {
            if (string.IsNullOrWhiteSpace(profile)) return;
            Profile = profile.Trim().ToLowerInvariant();
        }

        /// <summary>Returns available soundtrack mix profiles.</summary>
        /// <remarks>PHASE 2 - Team 1: Soundtrack Mixer</remarks>
        public static IReadOnlyList<string> Profiles() => new[] { "balanced", "bass_boost", "ambient_focus", "dialog_focus" };
    }

    /// <summary>
    /// Cheats menu flag registry.
    /// </summary>
    /// <remarks>PHASE 2 - Team 1: Cheats Menu</remarks>
    public static class CheatsMenuSystem
    {
        private static readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["infinite_lives"] = false,
            ["double_damage"] = false,
            ["invulnerable"] = false,
            ["unlock_all_levels"] = false,
        };

        /// <summary>Toggles a cheat flag and returns updated state.</summary>
        /// <remarks>PHASE 2 - Team 1: Cheats Menu</remarks>
        public static bool Toggle(string key)
        {
            if (!_flags.ContainsKey(key)) return false;
            _flags[key] = !_flags[key];
            return _flags[key];
        }

        /// <summary>Returns current cheat states as display lines.</summary>
        /// <remarks>PHASE 2 - Team 1: Cheats Menu</remarks>
        public static IReadOnlyList<string> Status()
            => _flags.OrderBy(kv => kv.Key).Select(kv => kv.Key + "=" + kv.Value).ToList();
    }

    /// <summary>
    /// Demo mode sequencer.
    /// </summary>
    /// <remarks>PHASE 2 - Team 1: Demo Mode</remarks>
    public static class DemoModeSystem
    {
        /// <summary>Returns scripted demo action text for frame index.</summary>
        /// <remarks>PHASE 2 - Team 1: Demo Mode</remarks>
        public static string ScriptAt(int frame)
        {
            int f = Math.Max(0, frame) % 240;
            if (f < 60) return "Run Right";
            if (f < 90) return "Jump";
            if (f < 130) return "Attack";
            if (f < 180) return "Dodge";
            return "Pause";
        }
    }

    /// <summary>
    /// Replay system recording metadata helper.
    /// </summary>
    /// <remarks>PHASE 2 - Team 1: Replay System</remarks>
    public static class ReplaySystemPhase2
    {
        private static readonly string ReplayMetaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "phase2-replay-meta.log");

        /// <summary>Appends replay metadata line.</summary>
        /// <remarks>PHASE 2 - Team 1: Replay System</remarks>
        public static void RecordMeta(string label)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReplayMetaPath));
            File.AppendAllText(ReplayMetaPath, $"{DateTime.UtcNow:o}|{label}{Environment.NewLine}", Encoding.UTF8);
        }

        /// <summary>Returns latest replay metadata lines.</summary>
        /// <remarks>PHASE 2 - Team 1: Replay System</remarks>
        public static IReadOnlyList<string> Tail(int max = 8)
        {
            if (!File.Exists(ReplayMetaPath)) return new[] { "No replay metadata." };
            var lines = File.ReadAllLines(ReplayMetaPath, Encoding.UTF8);
            int skip = Math.Max(0, lines.Length - Math.Max(1, max));
            return lines.Skip(skip).ToList();
        }
    }

    /// <summary>
    /// Caption system state service.
    /// </summary>
    /// <remarks>PHASE 2 - Team 1: Caption System</remarks>
    public static class CaptionSystem
    {
        /// <summary>Caption enabled state.</summary>
        public static bool Enabled { get; private set; } = true;

        /// <summary>Toggles caption enabled state.</summary>
        /// <remarks>PHASE 2 - Team 1: Caption System</remarks>
        public static void Toggle() => Enabled = !Enabled;

        /// <summary>Returns caption line (or empty if disabled).</summary>
        /// <remarks>PHASE 2 - Team 1: Caption System</remarks>
        public static string Line(string text) => Enabled ? "[CC] " + (text ?? string.Empty) : string.Empty;
    }

    /// <summary>
    /// Theme customization service.
    /// </summary>
    /// <remarks>PHASE 2 - Team 1: Theme Customization</remarks>
    public static class ThemeCustomizationSystem
    {
        /// <summary>Current selected UI/world theme key.</summary>
        public static string Current { get; private set; } = "classic";

        /// <summary>Returns available theme keys.</summary>
        /// <remarks>PHASE 2 - Team 1: Theme Customization</remarks>
        public static IReadOnlyList<string> Themes() => new[] { "classic", "neon", "retro", "high_contrast" };

        /// <summary>Sets active theme key if known.</summary>
        /// <remarks>PHASE 2 - Team 1: Theme Customization</remarks>
        public static void Set(string theme)
        {
            if (string.IsNullOrWhiteSpace(theme)) return;
            string t = theme.Trim().ToLowerInvariant();
            if (Themes().Contains(t)) Current = t;
        }
    }
}
