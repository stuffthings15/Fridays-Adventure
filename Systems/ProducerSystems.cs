using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Systems
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  ProducerSystems.cs  —  Producer / Project Manager: 10 NEW ideas
    //
    //  Idea 1:  Feature flag system — toggle features on/off at runtime.
    //  Idea 2:  Sprint timer — session timer fires an event every N minutes.
    //  Idea 3:  A/B variant tracking — tag which UI or gameplay variant is active.
    //  Idea 4:  Accessibility options — colorblind mode, large text, high contrast.
    //  Idea 5:  Auto-save reminder — prompts player if 10+ min elapsed without save.
    //  Idea 6:  Playtime limit warning — parental-control style "X minutes played" alert.
    //  Idea 7:  Update checker stub — reads a local version file on disk.
    //  Idea 8:  Feedback submission — writes a feedback text file to the log folder.
    //  Idea 9:  Tutorial-skip flag — once tutorials have been seen, skip on replay.
    //  Idea 10: Session tip cycling — rotating helpful tips shown during loading.
    // ═══════════════════════════════════════════════════════════════════════════

    // ── Idea 1: Feature flag system ───────────────────────────────────────────
    /// <summary>
    /// Lightweight feature-flag registry.  Flags are loaded from
    /// <c>Logs\feature-flags.cfg</c> at startup (key=value pairs, one per line)
    /// and can be toggled at runtime by the dev menu or DebugConsole.
    /// Team 2 (Producer) — Idea 1.
    /// </summary>
    public static class FeatureFlags
    {
        private static readonly Dictionary<string, bool> _flags =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private static readonly string FlagFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "feature-flags.cfg");

        /// <summary>Loads flags from disk. Call once at startup.</summary>
        public static void Load()
        {
            _flags.Clear();
            // Sensible defaults — all production features ON.
            SetDefault("HUD_ENABLED",          true);
            SetDefault("VISUAL_DEBUGGER",       true);
            SetDefault("ACHIEVEMENTS",          true);
            SetDefault("SCREEN_SHAKE",          true);
            SetDefault("ACCESSIBILITY_MODE",    false);
            SetDefault("COLORBLIND_MODE",       false);
            SetDefault("HIGH_CONTRAST_UI",      false);
            SetDefault("LARGE_TEXT",            false);
            SetDefault("NSPADE_MINIGAME",       true);
            SetDefault("CARD_MINIGAME",         true);
            SetDefault("HAMMER_BROS_ENCOUNTERS",true);
            SetDefault("TOAD_HOUSE",            true);

            if (!File.Exists(FlagFile)) return;
            try
            {
                foreach (string raw in File.ReadAllLines(FlagFile))
                {
                    string line = raw.Trim();
                    if (line.StartsWith("#") || !line.Contains("=")) continue;
                    var parts = line.Split(new[] { '=' }, 2);
                    if (bool.TryParse(parts[1].Trim(), out bool val))
                        _flags[parts[0].Trim()] = val;
                }
                DebugLogger.LogInfo("FeatureFlags", $"Loaded {_flags.Count} flags from disk.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FeatureFlags.Load", ex);
            }
        }

        /// <summary>Returns true if the feature is enabled.</summary>
        public static bool IsEnabled(string flag) =>
            _flags.TryGetValue(flag, out bool v) && v;

        /// <summary>Sets a flag at runtime. Does NOT persist unless SaveToDisk() is called.</summary>
        public static void Set(string flag, bool value)
        {
            _flags[flag] = value;
            DebugLogger.LogInfo("FeatureFlags.Set", $"{flag} = {value}");
        }

        /// <summary>Persists current flag values to disk.</summary>
        public static void SaveToDisk()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FlagFile));
                var sb = new StringBuilder();
                sb.AppendLine("# Friday's Adventure — Feature Flags");
                sb.AppendLine($"# Saved: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                foreach (var kv in _flags)
                    sb.AppendLine($"{kv.Key} = {kv.Value}");
                File.WriteAllText(FlagFile, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex) { DebugLogger.LogError("FeatureFlags.SaveToDisk", ex); }
        }

        private static void SetDefault(string flag, bool defaultValue)
        {
            if (!_flags.ContainsKey(flag)) _flags[flag] = defaultValue;
        }

        /// <summary>Returns all flags as key-value pairs (for the dev menu).</summary>
        public static IReadOnlyDictionary<string, bool> All => _flags;
    }

    // ── Idea 4: Accessibility options ─────────────────────────────────────────
    /// <summary>
    /// Runtime accessibility settings.  All scenes check these before drawing
    /// to apply colorblind-safe colors, larger fonts, and high-contrast panels.
    /// Team 2 (Producer) — Idea 4.
    /// </summary>
    public static class AccessibilityOptions
    {
        /// <summary>When true, replace saturated reds/greens with distinguishable tones.</summary>
        public static bool ColorblindMode    { get; set; }

        /// <summary>When true, all font sizes are scaled up by 1.4×.</summary>
        public static bool LargeText         { get; set; }

        /// <summary>When true, HUD and menu backgrounds use 90% opacity black panels.</summary>
        public static bool HighContrastUI    { get; set; }

        /// <summary>When true, flashing/strobing effects are suppressed.</summary>
        public static bool ReducedMotion     { get; set; }

        /// <summary>
        /// Applies current FeatureFlag values to the accessibility settings.
        /// Call after FeatureFlags.Load().
        /// Team 2 (Producer) — Idea 4.
        /// </summary>
        public static void SyncFromFlags()
        {
            ColorblindMode = FeatureFlags.IsEnabled("COLORBLIND_MODE");
            LargeText      = FeatureFlags.IsEnabled("LARGE_TEXT");
            HighContrastUI = FeatureFlags.IsEnabled("HIGH_CONTRAST_UI");
        }

        /// <summary>
        /// Returns a font size adjusted for accessibility settings.
        /// If LargeText is enabled the size is multiplied by 1.4.
        /// Team 2 (Producer) — Idea 4.
        /// </summary>
        public static float ScaledFontSize(float baseSize) =>
            LargeText ? baseSize * 1.4f : baseSize;

        /// <summary>
        /// Returns the colorblind-safe equivalent of a standard game color.
        /// Maps RED → ORANGE-YELLOW, GREEN → BLUE-GREEN for deuteranopia.
        /// Team 2 (Producer) — Idea 4.
        /// </summary>
        public static System.Drawing.Color SafeColor(System.Drawing.Color c)
        {
            if (!ColorblindMode) return c;
            // Deuteranopia: shift red → orange, shift green → blue-green
            int r = c.R, g = c.G, b = c.B;
            int newR = (int)(r * 0.56f + g * 0.43f);
            int newG = (int)(r * 0.56f + g * 0.43f);
            int newB = (int)(b * 0.93f + g * 0.07f);
            return System.Drawing.Color.FromArgb(c.A,
                Math.Min(255, newR), Math.Min(255, newG), Math.Min(255, newB));
        }
    }

    // ── Idea 2: Sprint timer ───────────────────────────────────────────────────
    /// <summary>
    /// Fires a <see cref="SprintIntervalEvent"/> every N minutes of play time.
    /// The producer can subscribe to trigger auto-save, analytics flush, etc.
    /// Team 2 (Producer) — Idea 2.
    /// </summary>
    public static class SprintTimer
    {
        private static float _sprintInterval = 10f * 60f;  // 10 minutes
        private static float _accumulated;

        /// <summary>Sets the sprint interval in minutes.</summary>
        public static void SetInterval(float minutes) =>
            _sprintInterval = Math.Max(1f, minutes * 60f);

        /// <summary>Tick from game loop. Fires SprintIntervalEvent when interval elapses.</summary>
        public static void Tick(float dt)
        {
            _accumulated += dt;
            if (_accumulated >= _sprintInterval)
            {
                _accumulated -= _sprintInterval;
                EventBus.Publish(new SprintIntervalEvent { TotalPlaySeconds = SessionStats.Instance.PlaySeconds });
                DebugLogger.LogInfo("SprintTimer", $"Sprint interval reached: {SessionStats.Instance.PlayTimeFormatted}");
            }
        }
    }

    /// <summary>Event fired every time the sprint interval elapses. Team 2 — Idea 2.</summary>
    public sealed class SprintIntervalEvent { public float TotalPlaySeconds; }

    // ── Idea 3: A/B variant tracker ───────────────────────────────────────────
    /// <summary>
    /// Tracks which UI/gameplay variants are active for A/B testing.
    /// Variant names are arbitrary strings set by the producer.
    /// Team 2 (Producer) — Idea 3.
    /// </summary>
    public static class ABVariants
    {
        private static readonly Dictionary<string, string> _active =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Registers a variant. Example: Set("HUD_LAYOUT", "smb3_classic").</summary>
        public static void Set(string experiment, string variant)
        {
            _active[experiment] = variant;
            DebugLogger.LogInfo("ABVariants", $"{experiment} → {variant}");
        }

        /// <summary>Returns the active variant for an experiment, or null.</summary>
        public static string Get(string experiment) =>
            _active.TryGetValue(experiment, out string v) ? v : null;

        /// <summary>Returns true if the given variant is active.</summary>
        public static bool Is(string experiment, string variant) =>
            string.Equals(Get(experiment), variant, StringComparison.OrdinalIgnoreCase);
    }

    // ── Idea 5: Auto-save reminder ────────────────────────────────────────────
    /// <summary>
    /// Tracks time since the last manual save and raises a reminder after a threshold.
    /// Team 2 (Producer) — Idea 5.
    /// </summary>
    public static class AutoSaveReminder
    {
        private static float _timeSinceLastSave;
        private const float ReminderThreshold = 10f * 60f;  // 10 minutes

        /// <summary>True while the reminder should be shown.</summary>
        public static bool ShouldRemind => _timeSinceLastSave >= ReminderThreshold;

        /// <summary>Resets the reminder timer (call when player saves).</summary>
        public static void NotifySaved() { _timeSinceLastSave = 0f; }

        /// <summary>Tick from game loop.</summary>
        public static void Tick(float dt) { _timeSinceLastSave += dt; }
    }

    // ── Idea 6: Playtime limit warning ────────────────────────────────────────
    /// <summary>
    /// Parental-control style playtime limit.
    /// When total play time exceeds the configured limit, an event is fired.
    /// Team 2 (Producer) — Idea 6.
    /// </summary>
    public static class PlaytimeLimit
    {
        private static float _limitSeconds  = float.MaxValue;
        private static bool  _warningFired;
        private static bool  _limitFired;

        /// <summary>Sets the hard limit in minutes. 0 = unlimited.</summary>
        public static void SetLimit(float minutes)
        {
            _limitSeconds = minutes <= 0f ? float.MaxValue : minutes * 60f;
            _warningFired = false;
            _limitFired   = false;
        }

        /// <summary>Tick — checks playtime against limit.</summary>
        public static void Tick(float dt)
        {
            float played = SessionStats.Instance.PlaySeconds;
            float warn   = _limitSeconds * 0.9f;  // 90% → warning

            if (!_warningFired && played >= warn)
            {
                _warningFired = true;
                EventBus.Publish(new PlaytimeLimitEvent { IsHardLimit = false, SecondsPlayed = played });
                DebugLogger.LogInfo("PlaytimeLimit", $"90% warning: {played:F0}s / {_limitSeconds:F0}s");
            }
            if (!_limitFired && played >= _limitSeconds)
            {
                _limitFired = true;
                EventBus.Publish(new PlaytimeLimitEvent { IsHardLimit = true, SecondsPlayed = played });
                DebugLogger.LogInfo("PlaytimeLimit", $"Hard limit reached: {played:F0}s");
            }
        }
    }

    /// <summary>Fired when the playtime warning or hard limit is reached. Team 2 — Idea 6.</summary>
    public sealed class PlaytimeLimitEvent { public bool IsHardLimit; public float SecondsPlayed; }

    // ── Idea 7: Update checker stub ───────────────────────────────────────────
    /// <summary>
    /// Compares the current build version against a local version file.
    /// In production this would call a version endpoint; for now it reads
    /// <c>Assets\latest-version.txt</c>.
    /// Team 2 (Producer) — Idea 7.
    /// </summary>
    public static class UpdateChecker
    {
        private static readonly string VersionFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "latest-version.txt");

        /// <summary>Checks the local version file. Returns an update notice or null.</summary>
        public static string CheckForUpdate()
        {
            try
            {
                if (!File.Exists(VersionFile)) return null;
                string latest = File.ReadAllText(VersionFile).Trim();
                string current = BuildInfo.Version;
                if (string.Compare(latest, current, StringComparison.Ordinal) > 0)
                    return $"Update available: v{latest} (current: v{current})";
                return null;
            }
            catch (Exception ex)
            {
                DebugLogger.LogWarning("UpdateChecker", ex.Message);
                return null;
            }
        }
    }

    // ── Idea 8: Feedback submission ───────────────────────────────────────────
    /// <summary>
    /// Writes player feedback to a timestamped text file in the Logs folder.
    /// In a full release this would post to a feedback server.
    /// Team 2 (Producer) — Idea 8.
    /// </summary>
    public static class FeedbackSubmitter
    {
        private static readonly string FeedbackDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Feedback");

        /// <summary>
        /// Saves a feedback message with session metadata.
        /// Returns the path of the written file.
        /// Team 2 (Producer) — Idea 8.
        /// </summary>
        public static string Submit(string message, string category = "General")
        {
            try
            {
                Directory.CreateDirectory(FeedbackDir);
                string file = Path.Combine(FeedbackDir,
                    $"feedback_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                var sb = new StringBuilder();
                sb.AppendLine($"Build:      {BuildInfo.Summary}");
                sb.AppendLine($"Session:    {SessionStats.Instance.SessionId}");
                sb.AppendLine($"PlayTime:   {SessionStats.Instance.PlayTimeFormatted}");
                sb.AppendLine($"Category:   {category}");
                sb.AppendLine($"Submitted:  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();
                sb.AppendLine(message);
                File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
                DebugLogger.LogInfo("FeedbackSubmitter", $"Feedback written: {file}");
                return file;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FeedbackSubmitter.Submit", ex);
                return null;
            }
        }
    }

    // ── Idea 9: Tutorial-skip flag ────────────────────────────────────────────
    /// <summary>
    /// Tracks which tutorial IDs have been completed so they can be skipped
    /// on subsequent playthroughs.
    /// Team 2 (Producer) — Idea 9.
    /// </summary>
    public static class TutorialTracker
    {
        private static readonly HashSet<string> _completed =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Returns true if this tutorial should be skipped.</summary>
        public static bool ShouldSkip(string tutorialId) => _completed.Contains(tutorialId);

        /// <summary>Marks a tutorial as seen.</summary>
        public static void MarkComplete(string tutorialId)
        {
            if (_completed.Add(tutorialId))
                DebugLogger.LogInfo("Tutorial", $"Completed: {tutorialId}");
        }
    }

    // ── Idea 10: Session tip cycling ──────────────────────────────────────────
    /// <summary>
    /// Provides rotating helpful tips for the loading screen.
    /// Team 2 (Producer) — Idea 10.
    /// </summary>
    public static class TipCycler
    {
        private static readonly string[] _tips =
        {
            "Run at full speed to charge the P-Meter and start flying!",
            "Collect 100 coins to earn an extra life.",
            "Ground-pound enemies to start a stomp chain for bonus points.",
            "Wall-jump to reach secret areas in fortress levels.",
            "Warp Whistles let you skip ahead to any unlocked world.",
            "Find all three King Coins in a level for a perfect rating.",
            "The N-Spade mini-game appears after collecting 80+ coins.",
            "Store a power-up in the item reserve box for emergencies.",
            "Boss Keys are required to open the boss gate on the map.",
            "Press DOWN + JUMP on a cloud platform to fall through it.",
            "The Frog Suit grants full swim control in underwater levels.",
            "A flashing star gives temporary invincibility — run through enemies!",
        };

        private static int _index;

        /// <summary>Returns the next tip in the rotation.</summary>
        public static string NextTip()
        {
            string tip = _tips[_index % _tips.Length];
            _index = (_index + 1) % _tips.Length;
            return tip;
        }
    }
}
