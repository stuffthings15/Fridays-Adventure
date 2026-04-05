using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// PHASE 2 - Team 10: Engine Programmer
    /// Feature: Comprehensive Test Logging System
    /// Purpose: Generates detailed logs for analyzing bot performance and level difficulty
    /// </summary>
    public static class TestLogSystem
    {
        private static string _logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "bot-tests");
        private static List<LogEntry> _currentLog = new List<LogEntry>();

        public struct LogEntry
        {
            public float Time { get; set; }
            public string Type { get; set; }
            public string Message { get; set; }
            public float BotX { get; set; }
            public float BotY { get; set; }
        }

        /// <summary>
        /// Initialize logging for a new level
        /// </summary>
        public static void StartLevelLog(string levelName)
        {
            _currentLog.Clear();
            Log("INIT", $"Starting level: {levelName}");
            Console.WriteLine($"📝 Logging started for {levelName}");
        }

        /// <summary>
        /// Add entry to log
        /// </summary>
        public static void Log(string type, string message, float botX = 0, float botY = 0)
        {
            // Only log every 0.5 seconds max to reduce file size
            if (_currentLog.Count > 0)
            {
                var lastEntry = _currentLog[_currentLog.Count - 1];
                if (Math.Abs(lastEntry.Time - (_currentLog.Count * 0.016f)) < 0.1f && lastEntry.Type == type && lastEntry.Message == message)
                    return;  // Skip duplicate entries
            }

            _currentLog.Add(new LogEntry
            {
                Time = _currentLog.Count * 0.016f,
                Type = type,
                Message = message,
                BotX = botX,
                BotY = botY
            });
        }

        /// <summary>
        /// Save log to file
        /// </summary>
        public static void SaveLog(string levelId, string levelName, bool isBeatable, float time, int items, int enemies, string failureReason = "")
        {
            if (!Directory.Exists(_logsDirectory))
                Directory.CreateDirectory(_logsDirectory);

            var sb = new System.Text.StringBuilder();

            // Header
            sb.AppendLine("════════════════════════════════════════════════════════════");
            sb.AppendLine($"LEVEL TEST LOG: {levelName}");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("════════════════════════════════════════════════════════════\n");

            // Summary
            sb.AppendLine("TEST RESULT:");
            sb.AppendLine($"  Status: {(isBeatable ? "✅ BEATABLE" : "❌ NOT BEATABLE")}");
            sb.AppendLine($"  Time: {time:F1}s");
            sb.AppendLine($"  Items Collected: {items}");
            sb.AppendLine($"  Enemies Defeated: {enemies}");
            if (!string.IsNullOrEmpty(failureReason))
                sb.AppendLine($"  Failure Reason: {failureReason}");
            sb.AppendLine("\n");

            // Detailed log
            if (_currentLog.Count > 0)
            {
                sb.AppendLine("DETAILED ACTION LOG:");
                sb.AppendLine("─────────────────────────────────────────────────────────");
                foreach (var entry in _currentLog)
                {
                    sb.AppendLine($"[{entry.Time:F2}s] {entry.Type}: {entry.Message}");
                    if (entry.BotX > 0 || entry.BotY > 0)
                        sb.AppendLine($"        @ ({entry.BotX:F0}, {entry.BotY:F0})");
                }
            }

            // Analysis
            sb.AppendLine("\n\nANALYSIS:");
            sb.AppendLine("─────────────────────────────────────────────────────────");
            sb.AppendLine($"Total Events Logged: {_currentLog.Count}");
            if (_currentLog.Count > 0)
            {
                var distances = _currentLog.Select((e, i) => i > 0 ? _currentLog[i].BotX - _currentLog[i - 1].BotX : 0).Sum();
                sb.AppendLine($"Distance Traveled: ~{distances:F0}px");
                sb.AppendLine($"Action Types: {string.Join(", ", _currentLog.Select(e => e.Type).Distinct())}");
            }

            // Recommendations
            if (!isBeatable)
            {
                sb.AppendLine("\nRECOMMENDATIONS:");
                sb.AppendLine("─────────────────────────────────────────────────────────");
                if (failureReason.Contains("Timeout"))
                {
                    sb.AppendLine("• Level took too long - reduce difficulty");
                    sb.AppendLine("• Simplify enemy patterns");
                    sb.AppendLine("• Add more platforms");
                    sb.AppendLine("• Review logged action sequence for stuck points");
                }
                else if (failureReason.Contains("insufficient"))
                {
                    sb.AppendLine("• Bot cannot traverse level");
                    sb.AppendLine("• Check for impassable gaps");
                    sb.AppendLine("• Verify platform placement");
                    sb.AppendLine("• Add jumping assists");
                }
            }

            string filename = $"{levelId}_{levelName.Replace(" ", "_").Replace(".", "").Replace(":", "")}.log";
            string filepath = Path.Combine(_logsDirectory, filename);

            try
            {
                File.WriteAllText(filepath, sb.ToString());
                Console.WriteLine($"✅ Log saved: {filepath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to save log: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate analysis summary
        /// </summary>
        public static void GenerateAnalysisSummary(List<LevelAutoTestResult> results)
        {
            if (!Directory.Exists(_logsDirectory))
                Directory.CreateDirectory(_logsDirectory);

            var sb = new System.Text.StringBuilder();

            sb.AppendLine("════════════════════════════════════════════════════════════");
            sb.AppendLine("AUTOMATED TEST ANALYSIS SUMMARY");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("════════════════════════════════════════════════════════════\n");

            int beatable = results.Count(r => r.IsBeatable);
            int total = results.Count;

            sb.AppendLine($"OVERALL RESULTS: {beatable}/{total} levels beatable ({beatable * 100 / total}%)\n");

            // Beatable levels
            sb.AppendLine("✅ BEATABLE LEVELS:");
            var beatableLevels = results.Where(r => r.IsBeatable).ToList();
            if (beatableLevels.Count > 0)
            {
                foreach (var result in beatableLevels)
                {
                    sb.AppendLine($"  {result.LevelName}");
                    sb.AppendLine($"    Time: {result.TimeToComplete:F1}s | Items: {result.ItemsCollected} | Enemies: {result.EnemiesDefeated}");
                }
            }

            // Unbeatable levels
            sb.AppendLine("\n❌ LEVELS NEEDING FIXES:");
            var unbeatable = results.Where(r => !r.IsBeatable).ToList();
            if (unbeatable.Count > 0)
            {
                foreach (var result in unbeatable)
                {
                    sb.AppendLine($"  {result.LevelName}");
                    sb.AppendLine($"    Issue: {result.FailureReason}");
                    sb.AppendLine($"    Log: {result.LevelId}_*.log");
                }
            }

            // Next steps
            sb.AppendLine("\n\nNEXT STEPS:");
            sb.AppendLine("─────────────────────────────────────────────────────────");
            sb.AppendLine("1. Review individual level logs in: Logs/bot-tests/");
            sb.AppendLine("2. Identify specific problem areas from logs");
            sb.AppendLine("3. Adjust level geometry or difficulty");
            sb.AppendLine("4. Rerun tests to verify fixes");
            sb.AppendLine("5. Iterate until all levels are beatable");

            string filepath = Path.Combine(_logsDirectory, "TEST_ANALYSIS_SUMMARY.txt");
            try
            {
                File.WriteAllText(filepath, sb.ToString());
                Console.WriteLine($"\n✅ Analysis summary saved: {filepath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to save summary: {ex.Message}");
            }
        }
    }
}
