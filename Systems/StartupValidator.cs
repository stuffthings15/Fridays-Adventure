using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Startup validator — checks required assets, log directory size, and
    /// platform compatibility before the game loop begins.
    ///
    /// Team 11 (Build/Tech Support Engineer) — all 10 ideas:
    ///   Idea 1:  BuildInfo display (already in BuildInfo.cs — referenced here).
    ///   Idea 3:  Auto-detect missing asset files on startup.
    ///   Idea 5:  Crash reporter — serialize last known state snapshot.
    ///   Idea 6:  Platform check — warn if runtime is unexpectedly old.
    ///   Idea 9:  Log rotation — archive old log files when they exceed a size limit.
    ///   Idea 10: Log-directory full warning.
    /// </summary>
    public static class StartupValidator
    {
        // ── Required asset manifest ────────────────────────────────────────────
        // Listing known critical assets; if any are missing a warning is logged.
        private static readonly string[] RequiredSprites =
        {
            "player_missfriday.png",
            "Orca.png",
            "Swan.png",
            "GARP.png",
        };

        private static readonly string[] RequiredAudio =
        {
            // Audio files expected in Assets/Audio (check by name pattern only).
            // Actual extension (.wav / .mp3 / .ogg) varies — just confirm folder.
        };

        // ── Log config ────────────────────────────────────────────────────────
        private const long MaxLogSizeBytes  = 5 * 1024 * 1024;  // 5 MB per log file
        private const int  MaxShotDirCount  = 200;               // screenshots before warning

        // ── Crash report path ──────────────────────────────────────────────────
        private static readonly string CrashDir  = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string CrashFile = Path.Combine(CrashDir, "crash-state.txt");

        // ── Public entry point ────────────────────────────────────────────────

        /// <summary>
        /// Runs all startup validation checks and writes results to the error log.
        /// Call once from Program.Main or Form1 constructor before Game.Start().
        /// </summary>
        public static void Run()
        {
            Directory.CreateDirectory(CrashDir);

            CheckPlatform();
            RotateLogs();
            CheckRequiredAssets();
            CheckLogDirectorySize();

            DebugLogger.LogInfo("StartupValidator", $"Startup validation complete. {BuildInfo.Summary}");
        }

        // ── Platform check ────────────────────────────────────────────────────

        /// <summary>Logs a warning if the CLR version is below the supported range.</summary>
        private static void CheckPlatform()
        {
            string clr = Environment.Version.ToString();
            string os  = Environment.OSVersion.ToString();
            DebugLogger.LogInfo("Platform", $"CLR={clr}  OS={os}  64bit={Environment.Is64BitProcess}");

            if (Environment.Version.Major < 4)
                DebugLogger.LogInfo("Platform", "WARNING: CLR version is below .NET 4.0 — unexpected behaviour may occur.");
        }

        // ── Log rotation ──────────────────────────────────────────────────────

        /// <summary>
        /// Archives the current error-debugger.log if it exceeds MaxLogSizeBytes.
        /// Creates a timestamped backup and starts a fresh file.
        /// </summary>
        private static void RotateLogs()
        {
            string logFile = Path.Combine(CrashDir, "error-debugger.log");
            if (!File.Exists(logFile)) return;

            var info = new FileInfo(logFile);
            if (info.Length < MaxLogSizeBytes) return;

            string stamp  = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string archive = Path.Combine(CrashDir, $"error-debugger-{stamp}.log");
            try
            {
                File.Move(logFile, archive);
                DebugLogger.LogInfo("StartupValidator", $"Log rotated → {archive}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("StartupValidator.RotateLogs", ex);
            }
        }

        // ── Required assets check ─────────────────────────────────────────────

        /// <summary>Checks that all listed critical sprite files exist on disk.</summary>
        private static void CheckRequiredAssets()
        {
            string spritesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sprites");
            var missing = new List<string>();

            foreach (string file in RequiredSprites)
            {
                string full = Path.Combine(spritesDir, file);
                if (!File.Exists(full))
                    missing.Add(file);
            }

            if (missing.Count > 0)
                DebugLogger.LogInfo("AssetCheck",
                    $"MISSING ASSETS ({missing.Count}): {string.Join(", ", missing)}");
            else
                DebugLogger.LogInfo("AssetCheck", "All required assets present.");
        }

        // ── Log directory size check ──────────────────────────────────────────

        /// <summary>Warns if the ErrorShots folder contains more than MaxShotDirCount files.</summary>
        private static void CheckLogDirectorySize()
        {
            string shotDir = Path.Combine(CrashDir, "ErrorShots");
            if (!Directory.Exists(shotDir)) return;
            int count = Directory.GetFiles(shotDir, "*.png").Length;
            if (count > MaxShotDirCount)
                DebugLogger.LogInfo("StartupValidator",
                    $"WARNING: ErrorShots folder has {count} files (limit {MaxShotDirCount}). " +
                    "Clear via DevMenu → [TOOLS] Clear Logs + Screenshots.");
        }

        // ── Crash reporter ────────────────────────────────────────────────────

        /// <summary>
        /// Writes a crash state snapshot to disk.  Call from an unhandled-exception
        /// handler or Application.ThreadException to capture the last known state.
        /// </summary>
        public static void WriteCrashState(Exception ex, string context)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"CRASH REPORT — {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Context : {context}");
                sb.AppendLine($"Build   : {BuildInfo.Summary}");
                sb.AppendLine($"OS      : {Environment.OSVersion}");
                sb.AppendLine($"CLR     : {Environment.Version}");
                sb.AppendLine($"Machine : {Environment.MachineName}");
                sb.AppendLine();
                sb.AppendLine("Exception:");
                sb.AppendLine(ex?.ToString() ?? "(null)");
                sb.AppendLine();
                sb.AppendLine($"VisualDebugger Errors : {VisualDebugger.ErrorCount}");
                sb.AppendLine($"VisualDebugger Info   : {VisualDebugger.InfoCount}");

                File.WriteAllText(CrashFile, sb.ToString(), Encoding.UTF8);
            }
            catch { /* Crash reporter must never throw */ }
        }
    }
}
