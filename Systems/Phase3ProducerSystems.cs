// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 2: Producer / Project Manager
// Feature: Content Pipeline Dashboard + Community Ops Data Layer
// Purpose: Provides foundational data for roadmap, leaderboard, calendar,
//          and survey workflows used by Phase 3 producer tooling.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Fridays_Adventure.Systems
{
    /// <summary>Single player leaderboard entry.</summary>
    public sealed class LeaderboardEntry
    {
        /// <summary>Display name for the run.</summary>
        public string Player { get; set; }
        /// <summary>Total score achieved.</summary>
        public int Score { get; set; }
        /// <summary>UTC timestamp when the run was recorded.</summary>
        public DateTime TimestampUtc { get; set; }
    }

    /// <summary>Pipeline task row shown in the producer dashboard.</summary>
    public sealed class PipelineTask
    {
        /// <summary>Task title.</summary>
        public string Name { get; set; }
        /// <summary>Status string (Planned/In Progress/Done/Blocked).</summary>
        public string Status { get; set; }
        /// <summary>Team owner of the task.</summary>
        public string OwnerTeam { get; set; }
    }

    /// <summary>Community event calendar item.</summary>
    public sealed class CommunityEventItem
    {
        /// <summary>Event title.</summary>
        public string Name { get; set; }
        /// <summary>Scheduled date (local date display).</summary>
        public DateTime Date { get; set; }
        /// <summary>Short event details.</summary>
        public string Details { get; set; }
    }

    /// <summary>
    /// Phase 3 producer foundations data hub.
    /// </summary>
    public static class Phase3ProducerSystems
    {
        private static readonly string LogsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string DataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Data");
        private static readonly string LeaderboardPath = Path.Combine(LogsDir, "phase3-leaderboard.csv");
        private static readonly string SurveyPath = Path.Combine(LogsDir, "phase3-surveys.csv");
        private static readonly string CalendarPath = Path.Combine(DataDir, "community-events.csv");
        private static readonly string BetaProgramPath = Path.Combine(LogsDir, "phase3-beta-program.csv");

        private static readonly List<PipelineTask> _pipeline = new List<PipelineTask>
        {
            new PipelineTask { Name = "Community Council Board", Status = "In Progress", OwnerTeam = "Team 2" },
            new PipelineTask { Name = "Mod Metadata Index", Status = "Planned", OwnerTeam = "Team 8" },
            new PipelineTask { Name = "Mod Manager UI", Status = "Planned", OwnerTeam = "Team 9" },
            new PipelineTask { Name = "CI Mod Validation", Status = "Planned", OwnerTeam = "Team 11" },
            new PipelineTask { Name = "Seasonal Roadmap Panel", Status = "In Progress", OwnerTeam = "Team 2" },
        };

        private static readonly string[] _seasonRoadmap =
        {
            "SPRING: Community Council + roadmap + leaderboard",
            "SUMMER: Mod framework + workshop browsing + DLC detection",
            "AUTUMN: Challenge hub + cosmetics economy + profile systems",
            "WINTER: Gauntlet events + polish wave + QA community programs",
        };

        /// <summary>Returns content pipeline rows for display.</summary>
        public static IReadOnlyList<PipelineTask> GetPipelineTasks() => _pipeline;

        /// <summary>Returns the high-level seasonal roadmap lines.</summary>
        public static IReadOnlyList<string> GetSeasonalRoadmap() => _seasonRoadmap;

        /// <summary>Returns community events sorted by date.</summary>
        public static IReadOnlyList<CommunityEventItem> GetCommunityEvents()
        {
            EnsurePaths();
            if (!File.Exists(CalendarPath)) WriteDefaultCalendar();

            var list = new List<CommunityEventItem>();
            foreach (string raw in File.ReadAllLines(CalendarPath, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("#")) continue;
                string[] p = raw.Split('|');
                if (p.Length < 3) continue;
                if (!DateTime.TryParseExact(p[0].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime d))
                    continue;
                list.Add(new CommunityEventItem
                {
                    Date = d,
                    Name = p[1].Trim(),
                    Details = p[2].Trim()
                });
            }
            return list.OrderBy(e => e.Date).ToList();
        }

        /// <summary>Adds a leaderboard row and persists top 20 results.</summary>
        public static void AddLeaderboardScore(string player, int score)
        {
            if (string.IsNullOrWhiteSpace(player)) player = "Player";
            var all = GetLeaderboard().ToList();
            all.Add(new LeaderboardEntry { Player = player.Trim(), Score = score, TimestampUtc = DateTime.UtcNow });
            all = all.OrderByDescending(x => x.Score).ThenBy(x => x.TimestampUtc).Take(20).ToList();
            SaveLeaderboard(all);
        }

        /// <summary>Returns leaderboard rows sorted descending by score.</summary>
        public static IReadOnlyList<LeaderboardEntry> GetLeaderboard()
        {
            EnsurePaths();
            if (!File.Exists(LeaderboardPath)) return new List<LeaderboardEntry>();

            var rows = new List<LeaderboardEntry>();
            foreach (string raw in File.ReadAllLines(LeaderboardPath, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("#")) continue;
                string[] p = raw.Split(',');
                if (p.Length < 3) continue;
                if (!int.TryParse(p[1], out int score)) continue;
                if (!DateTime.TryParse(p[2], null, DateTimeStyles.RoundtripKind, out DateTime ts))
                    ts = DateTime.UtcNow;
                rows.Add(new LeaderboardEntry { Player = p[0], Score = score, TimestampUtc = ts });
            }
            return rows.OrderByDescending(x => x.Score).ThenBy(x => x.TimestampUtc).ToList();
        }

        /// <summary>Submits one player survey response to persistent CSV.</summary>
        public static void SubmitSurveyResponse(string topic, int rating1To5, string note)
        {
            EnsurePaths();
            rating1To5 = Math.Max(1, Math.Min(5, rating1To5));
            string safeTopic = (topic ?? "General").Replace(',', ' ').Trim();
            string safeNote = (note ?? string.Empty).Replace(',', ' ').Replace('\n', ' ').Replace('\r', ' ').Trim();
            File.AppendAllText(SurveyPath,
                $"{DateTime.UtcNow:o},{safeTopic},{rating1To5},{safeNote}{Environment.NewLine}", Encoding.UTF8);
            DebugLogger.LogInfo("Phase3ProducerSystems.Survey", $"Survey submitted for topic '{safeTopic}'.");
        }

        /// <summary>Returns a short survey stats summary per topic.</summary>
        public static IReadOnlyList<string> GetSurveySummary()
        {
            EnsurePaths();
            if (!File.Exists(SurveyPath)) return new List<string> { "No survey responses yet." };

            var sums = new Dictionary<string, (int count, int scoreSum)>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in File.ReadAllLines(SurveyPath, Encoding.UTF8))
            {
                string[] p = raw.Split(',');
                if (p.Length < 4) continue;
                string topic = p[1].Trim();
                if (!int.TryParse(p[2], out int r)) continue;
                if (!sums.ContainsKey(topic)) sums[topic] = (0, 0);
                var v = sums[topic];
                sums[topic] = (v.count + 1, v.scoreSum + r);
            }

            return sums
                .OrderByDescending(kv => kv.Value.count)
                .Select(kv =>
                {
                    double avg = kv.Value.count > 0 ? (double)kv.Value.scoreSum / kv.Value.count : 0;
                    return $"{kv.Key}: {kv.Value.count} responses, avg {avg:F2}/5";
                })
                .ToList();
        }

        private static void SaveLeaderboard(List<LeaderboardEntry> rows)
        {
            EnsurePaths();
            var sb = new StringBuilder();
            sb.AppendLine("# player,score,timestampUtc");
            foreach (var r in rows)
                sb.AppendLine($"{r.Player},{r.Score},{r.TimestampUtc:o}");
            File.WriteAllText(LeaderboardPath, sb.ToString(), Encoding.UTF8);
        }

        private static void EnsurePaths()
        {
            Directory.CreateDirectory(LogsDir);
            Directory.CreateDirectory(DataDir);
        }

        private static void WriteDefaultCalendar()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# yyyy-MM-dd|Event|Details");
            sb.AppendLine($"{DateTime.Today.AddDays(7):yyyy-MM-dd}|Community Council|Monthly producer-led roadmap vote");
            sb.AppendLine($"{DateTime.Today.AddDays(14):yyyy-MM-dd}|Challenge Weekend|Top score leaderboard sprint");
            sb.AppendLine($"{DateTime.Today.AddDays(30):yyyy-MM-dd}|Creator Spotlight|Featured community content showcase");
            File.WriteAllText(CalendarPath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Team 2 Idea 6: Revenue Model System snapshot.
        /// Returns a fixed, local estimate model based on active users and conversion.
        /// </summary>
        public static IReadOnlyList<string> GetRevenueModelSnapshot()
        {
            // Local deterministic simulation values for planning dashboards.
            const int monthlyActiveUsers = 2400;
            const float conversionRate = 0.035f;    // 3.5%
            const float avgPurchase = 4.99f;

            int payingUsers = (int)(monthlyActiveUsers * conversionRate);
            float monthlyGross = payingUsers * avgPurchase;

            return new[]
            {
                $"MAU: {monthlyActiveUsers:N0}",
                $"Conversion: {conversionRate * 100f:F1}%",
                $"Paying users: {payingUsers:N0}",
                $"Avg purchase: ${avgPurchase:F2}",
                $"Estimated monthly gross: ${monthlyGross:N2}",
                "Model: cosmetics-only, no pay-to-win gameplay impact",
            };
        }

        /// <summary>
        /// Team 2 Idea 7: Quality Gate Automation.
        /// Executes local readiness checks and returns pass/fail lines.
        /// </summary>
        public static IReadOnlyList<string> RunQualityGateAutomation()
        {
            string root = AppDomain.CurrentDomain.BaseDirectory;
            var lines = new List<string>();

            bool hasAssets = Directory.Exists(Path.Combine(root, "Assets"));
            lines.Add($"Assets bundle: {(hasAssets ? "PASS" : "FAIL")}");

            bool hasLogs = Directory.Exists(Path.Combine(root, "Logs"));
            lines.Add($"Logs directory: {(hasLogs ? "PASS" : "FAIL")}");

            bool hasConfig = File.Exists(Path.Combine(root, "game-config.ini"));
            lines.Add($"Runtime config: {(hasConfig ? "PASS" : "FAIL")}");

            bool hasBuildManifest = File.Exists(Path.Combine(root, "Logs", "build-manifest.txt"));
            lines.Add($"Build manifest: {(hasBuildManifest ? "PASS" : "WARN")}");

            bool hasRelease = Directory.Exists(Path.Combine(root, "Release")) ||
                              Directory.Exists(Path.Combine(root, "bin", "Release"));
            lines.Add($"Release artifacts: {(hasRelease ? "PASS" : "WARN")}");

            int failCount = lines.Count(x => x.EndsWith("FAIL"));
            lines.Insert(0, failCount == 0 ? "QUALITY GATE: PASS" : $"QUALITY GATE: FAIL ({failCount})");

            DebugLogger.LogInfo("Phase3ProducerSystems.QualityGate", lines[0]);
            return lines;
        }

        /// <summary>
        /// Team 2 Idea 9: Content Creator Dashboard summary.
        /// Aggregates mod, leaderboard, and survey health indicators.
        /// </summary>
        public static IReadOnlyList<string> GetContentCreatorDashboardSummary()
        {
            int modCount = ModMetadataSystem.LoadAll().Count;
            int dlcCount = DlcDetectionSystem.GetInstalledPackages().Count;
            int leaderboardRows = GetLeaderboard().Count;
            int surveyRows = File.Exists(SurveyPath) ? File.ReadAllLines(SurveyPath, Encoding.UTF8).Length : 0;

            return new[]
            {
                $"Installed mods: {modCount}",
                $"Installed DLC packs: {dlcCount}",
                $"Leaderboard rows: {leaderboardRows}",
                $"Survey responses: {Math.Max(0, surveyRows)}",
                "Creator recommendation: spotlight top 3 weekly runs",
            };
        }

        /// <summary>
        /// Team 2 Idea 10: Beta Testing Program registry.
        /// Adds one tester record to local CSV.
        /// </summary>
        public static void RegisterBetaTester(string alias, string focusArea)
        {
            EnsurePaths();
            string safeAlias = (alias ?? "tester").Replace(',', ' ').Trim();
            string safeFocus = (focusArea ?? "general").Replace(',', ' ').Trim();
            File.AppendAllText(BetaProgramPath,
                $"{DateTime.UtcNow:o},{safeAlias},{safeFocus}{Environment.NewLine}", Encoding.UTF8);
            DebugLogger.LogInfo("Phase3ProducerSystems.Beta", $"Registered beta tester: {safeAlias} ({safeFocus})");
        }

        /// <summary>
        /// Team 2 Idea 10: Returns beta testing program summary lines.
        /// </summary>
        public static IReadOnlyList<string> GetBetaProgramSummary()
        {
            EnsurePaths();
            if (!File.Exists(BetaProgramPath))
                return new[] { "No beta testers registered yet." };

            var lines = File.ReadAllLines(BetaProgramPath, Encoding.UTF8)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            var focusCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in lines)
            {
                string[] p = raw.Split(',');
                if (p.Length < 3) continue;
                string focus = p[2].Trim();
                if (!focusCounts.ContainsKey(focus)) focusCounts[focus] = 0;
                focusCounts[focus]++;
            }

            var summary = new List<string> { $"Total testers: {lines.Count}" };
            summary.AddRange(focusCounts.OrderByDescending(kv => kv.Value)
                .Select(kv => $"{kv.Key}: {kv.Value}"));
            return summary;
        }
    }
}
