using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// PHASE 2 - Team 10: Engine Programmer
    /// Feature: Enhanced Test Results with Visual Analysis
    /// Purpose: Extended test results that include visual debugging data and stuck detection
    /// </summary>
    public class EnhancedLevelTestResult
    {
        public string LevelId { get; set; }
        public string LevelName { get; set; }
        public bool IsBeatable { get; set; }
        public float TimeToComplete { get; set; }
        public int ItemsCollected { get; set; }
        public int EnemiesDefeated { get; set; }
        public string FailureReason { get; set; }
        public AutoTestBot BotData { get; set; }

        // Visual debugging information
        public BotVisualDebugger VisualDebugData { get; set; }
        public bool BotGotStuck { get; set; }
        public float StuckDuration { get; set; }
        public List<BotVisualDebugger.BotActionLog> DetailedActionLog { get; set; } = new List<BotVisualDebugger.BotActionLog>();

        /// <summary>
        /// Save detailed analysis report to file
        /// </summary>
        public void SaveDetailedReport(string baseLogDirectory)
        {
            try
            {
                string logsDir = Path.Combine(baseLogDirectory, "detailed-analysis");
                Directory.CreateDirectory(logsDir);

                string fileName = $"{LevelId}_visual_analysis_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(logsDir, fileName);

                var report = new StringBuilder();
                report.AppendLine($"\n{'═'} DETAILED VISUAL ANALYSIS REPORT {'═'}\n");
                report.AppendLine($"Level: {LevelName} ({LevelId})");
                report.AppendLine($"Test Date/Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine($"\n{'─'} PERFORMANCE METRICS {'─'}\n");
                report.AppendLine($"Overall Result: {(IsBeatable ? "✅ BEATABLE" : "❌ NOT BEATABLE")}");
                report.AppendLine($"Time to Complete: {TimeToComplete:F2}s");
                report.AppendLine($"Distance Traveled: {BotData?.DistanceTraveled:F0}px");
                report.AppendLine($"Items Collected: {ItemsCollected}");
                report.AppendLine($"Enemies Defeated: {EnemiesDefeated}");
                report.AppendLine($"Final Bot Position: ({BotData?.BotX:F0}, {BotData?.BotY:F0})");

                report.AppendLine($"\n{'─'} STUCK DETECTION {'─'}\n");
                report.AppendLine($"Bot Got Stuck: {(BotGotStuck ? "⚠️ YES" : "✅ NO")}");
                if (BotGotStuck)
                    report.AppendLine($"Stuck Duration: {StuckDuration:F2}s");

                report.AppendLine($"\n{'─'} FAILURE REASON {'─'}\n");
                if (!string.IsNullOrEmpty(FailureReason))
                    report.AppendLine($"{FailureReason}");
                else
                    report.AppendLine("None - Level completed successfully!");

                report.AppendLine($"\n{'─'} ACTION TIMELINE (First 50 Actions) {'─'}\n");
                for (int i = 0; i < Math.Min(50, DetailedActionLog.Count); i++)
                {
                    var log = DetailedActionLog[i];
                    report.AppendLine($"[{log.Time:F2}s] {log.Action,-25} | {log.State,-12} | ({log.BotX:F0}, {log.BotY:F0}) | {log.Details}");
                }

                if (DetailedActionLog.Count > 50)
                    report.AppendLine($"\n... and {DetailedActionLog.Count - 50} more actions");

                // Add item and enemy analysis if available
                if (VisualDebugData != null)
                {
                    report.Append("\n");
                    report.Append(VisualDebugData.GetItemEnemyAnalysisReport());
                }

                report.AppendLine($"\n{'═'} END OF REPORT {'═'}\n");

                File.WriteAllText(filePath, report.ToString());
                System.Diagnostics.Debug.WriteLine($"✅ Detailed analysis saved: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to save detailed report: {ex.Message}");
            }
        }

        /// <summary>
        /// Get summary for display
        /// </summary>
        public string GetSummary()
        {
            return $"Time: {TimeToComplete:F1}s | Distance: {BotData?.DistanceTraveled:F0}px | Items: {ItemsCollected} | Enemies: {EnemiesDefeated} | Stuck: {(BotGotStuck ? "⚠️" : "✅")} | Completed: {(IsBeatable ? "✅" : "❌")}";
        }
    }

    /// <summary>
    /// Test configuration for visual vs. statistical mode
    /// </summary>
    public enum TestMode
    {
        Statistical,  // Fast, no rendering, just statistics
        Visual        // Slower, renders bot, detailed logging, stuck detection
    }
}
