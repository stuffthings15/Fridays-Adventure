// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 2: Producer
// Feature: Weekly Challenge, Roadmap, Feedback Portal, Test Mode, Telemetry
// Purpose: Provides production-facing systems for Phase 2 operations dashboard.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Produces a deterministic weekly challenge string from current calendar week.
    /// </summary>
    /// <remarks>PHASE 2 - Team 2: Weekly Challenge Generator</remarks>
    public static class WeeklyChallengeGenerator
    {
        private static readonly string[] Challenges =
        {
            "Clear any island with no damage taken.",
            "Defeat 40 enemies in one session.",
            "Collect 150 berries before game over.",
            "Beat a boss in under 120 seconds.",
            "Complete two levels using only jump attacks.",
            "Finish a run with 3 or fewer deaths.",
            "Reach combo chain 12+ at least once.",
        };

        /// <summary>Returns this week's challenge description.</summary>
        /// <remarks>PHASE 2 - Team 2: Weekly Challenge Generator</remarks>
        public static string Current()
        {
            var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
            int week = cal.GetWeekOfYear(DateTime.UtcNow,
                System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday);
            return Challenges[week % Challenges.Length];
        }
    }

    /// <summary>
    /// Provides Phase 2 roadmap lines for in-game visibility.
    /// </summary>
    /// <remarks>PHASE 2 - Team 2: Content Roadmap Display</remarks>
    public static class ContentRoadmapDisplay
    {
        /// <summary>Returns roadmap items for the current implementation wave.</summary>
        /// <remarks>PHASE 2 - Team 2: Content Roadmap Display</remarks>
        public static IReadOnlyList<string> GetItems()
        {
            return new[]
            {
                "WEEK 1: settings + difficulty + telemetry",
                "WEEK 2: level systems + combat extensions",
                "WEEK 3: narrative + codex + side quests",
                "WEEK 4: optimization + QA verification",
            };
        }
    }

    /// <summary>
    /// Player feedback persistence API for Phase 2 dashboards.
    /// </summary>
    /// <remarks>PHASE 2 - Team 2: Player Feedback Portal</remarks>
    public static class PlayerFeedbackPortal
    {
        private static readonly string FeedbackPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "phase2-feedback.csv");

        /// <summary>Submits a feedback row to local csv storage.</summary>
        /// <remarks>PHASE 2 - Team 2: Player Feedback Portal</remarks>
        public static void Submit(string category, string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FeedbackPath));
            string c = (category ?? "General").Replace(',', ' ').Trim();
            string m = (message ?? string.Empty).Replace(',', ' ').Replace('\n', ' ').Replace('\r', ' ').Trim();
            File.AppendAllText(FeedbackPath, $"{DateTime.UtcNow:o},{c},{m}{Environment.NewLine}", Encoding.UTF8);
        }

        /// <summary>Returns summarized feedback counts by category.</summary>
        /// <remarks>PHASE 2 - Team 2: Player Feedback Portal</remarks>
        public static IReadOnlyList<string> Summary()
        {
            if (!File.Exists(FeedbackPath)) return new[] { "No feedback submitted yet." };
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in File.ReadAllLines(FeedbackPath, Encoding.UTF8))
            {
                var p = line.Split(',');
                if (p.Length < 3) continue;
                string k = p[1].Trim();
                if (!counts.ContainsKey(k)) counts[k] = 0;
                counts[k]++;
            }
            return counts.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}: {kv.Value}").ToList();
        }
    }

    /// <summary>
    /// Runtime test-mode selector for QA and producer testing loops.
    /// </summary>
    /// <remarks>PHASE 2 - Team 2: Test Mode Selector</remarks>
    public static class TestModeSelector
    {
        /// <summary>Available runtime test modes.</summary>
        public enum TestMode
        {
            /// <summary>Regular game rules.</summary>
            Normal,
            /// <summary>Fast movement and reduced cooldowns.</summary>
            Speed,
            /// <summary>High enemy density and stress conditions.</summary>
            Stress,
            /// <summary>Friendly validation mode with reduced penalties.</summary>
            Safe
        }

        /// <summary>Current selected test mode.</summary>
        /// <remarks>PHASE 2 - Team 2: Test Mode Selector</remarks>
        public static TestMode Current { get; private set; } = TestMode.Normal;

        /// <summary>Advances to the next test mode value.</summary>
        /// <remarks>PHASE 2 - Team 2: Test Mode Selector</remarks>
        public static void Next()
        {
            int v = ((int)Current + 1) % Enum.GetValues(typeof(TestMode)).Length;
            Current = (TestMode)v;
            DebugLogger.LogInfo("TestModeSelector", "Mode=" + Current);
        }
    }

    /// <summary>
    /// Telemetry dashboard data helpers for producer and QA quick-read panels.
    /// </summary>
    /// <remarks>PHASE 2 - Team 2: Telemetry Dashboard</remarks>
    public static class TelemetryDashboard
    {
        /// <summary>Returns compact telemetry summary lines.</summary>
        /// <remarks>PHASE 2 - Team 2: Telemetry Dashboard</remarks>
        public static IReadOnlyList<string> Summary()
        {
            var s = SessionStats.Instance;
            string frame = FrameTimeHistogram.GetSummary();
            int nl = frame.IndexOf('\n');
            if (nl > 0) frame = frame.Substring(0, nl);
            return new[]
            {
                $"PlayTime: {s.PlayTimeFormatted}",
                $"Deaths: {s.DeathCount}  Enemies: {s.EnemiesDefeated}",
                $"Levels: {s.LevelsCompleted}  Bosses: {s.BossesDefeated}",
                $"ComboMax: {s.LongestCombo}",
                frame,
                $"Mode: {TestModeSelector.Current}",
            };
        }
    }

    /// <summary>
    /// Milestone tracker for producer-facing phase checkpoints.
    /// </summary>
    /// <remarks>PHASE 2 - Team 2: Milestone Tracker</remarks>
    public static class MilestoneTracker
    {
        /// <summary>Returns milestone completion lines derived from current session stats.</summary>
        /// <remarks>PHASE 2 - Team 2: Milestone Tracker</remarks>
        public static IReadOnlyList<string> GetMilestones()
        {
            var s = SessionStats.Instance;
            return new[]
            {
                Format("First Level Clear", s.LevelsCompleted >= 1),
                Format("Triple Level Clear", s.LevelsCompleted >= 3),
                Format("Boss Defeated", s.BossesDefeated >= 1),
                Format("Combo 10+", s.LongestCombo >= 10),
                Format("Collector 100", s.BerriesCollected >= 100),
            };
        }

        private static string Format(string label, bool complete)
            => (complete ? "[DONE] " : "[....] ") + label;
    }

    /// <summary>
    /// Session recording utility that emits periodic snapshots to CSV.
    /// </summary>
    /// <remarks>PHASE 2 - Team 2: Session Recording</remarks>
    public static class SessionRecording
    {
        private static readonly string SessionPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "phase2-session-recording.csv");

        /// <summary>Appends one session snapshot row.</summary>
        /// <remarks>PHASE 2 - Team 2: Session Recording</remarks>
        public static void RecordSnapshot(string source)
        {
            var s = SessionStats.Instance;
            Directory.CreateDirectory(Path.GetDirectoryName(SessionPath));
            File.AppendAllText(SessionPath,
                $"{DateTime.UtcNow:o},{(source ?? "manual")},{s.PlayTimeFormatted},{s.DeathCount},{s.LevelsCompleted},{s.BossesDefeated},{s.LongestCombo}{Environment.NewLine}",
                Encoding.UTF8);
        }

        /// <summary>Returns last recorded snapshot lines.</summary>
        /// <remarks>PHASE 2 - Team 2: Session Recording</remarks>
        public static IReadOnlyList<string> Tail(int max = 8)
        {
            if (!File.Exists(SessionPath)) return new[] { "No session snapshots yet." };
            var lines = File.ReadAllLines(SessionPath, Encoding.UTF8);
            int skip = Math.Max(0, lines.Length - Math.Max(1, max));
            return lines.Skip(skip).ToList();
        }
    }

    /// <summary>
    /// Broadcast channel for producer communications and status notices.
    /// </summary>
    /// <remarks>PHASE 2 - Team 2: Communication Broadcast</remarks>
    public static class CommunicationBroadcast
    {
        private static readonly Queue<string> _queue = new Queue<string>();

        /// <summary>Pushes one broadcast message into the queue.</summary>
        /// <remarks>PHASE 2 - Team 2: Communication Broadcast</remarks>
        public static void Push(string message)
        {
            string msg = (message ?? string.Empty).Trim();
            if (msg.Length == 0) return;
            _queue.Enqueue($"[{DateTime.Now:HH:mm}] {msg}");
            while (_queue.Count > 20) _queue.Dequeue();
        }

        /// <summary>Returns queued broadcast messages in display order.</summary>
        /// <remarks>PHASE 2 - Team 2: Communication Broadcast</remarks>
        public static IReadOnlyList<string> GetAll() => _queue.ToList();
    }
}
