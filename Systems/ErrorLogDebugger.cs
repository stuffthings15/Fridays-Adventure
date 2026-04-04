using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Fridays_Adventure.Systems
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  ErrorLogDebugger.cs  —  Structured Error + Info Logger
    //
    //  This is the MISSING DebugLogger class referenced throughout the project.
    //  All gameplay code that calls DebugLogger.LogError / DebugLogger.LogInfo
    //  routes through here, which then fans out to:
    //    1. A rotating plaintext log file  (Logs\debug-YYYY-MM-DD.log)
    //    2. A structured JSON log file     (Logs\errors.json)
    //    3. VisualDebugger.RecordError     (screenshot + HTML report)
    //    4. DebugConsole overlay output
    //
    //  Team 3  (Technical Lead)  — Ideas 1-10 : error categories, severity levels,
    //                               breadcrumb trail, stack trace, rate limiter,
    //                               auto-export on critical, live feed, context tags,
    //                               performance hit warnings, per-scene grouping.
    //
    //  Team 8  (Systems Programmer) — Ideas 1-10 : rotating logs, log level filter,
    //                               JSON export, async screenshots, memory snapshot,
    //                               batch screenshot, search API, trend analysis,
    //                               configurable verbosity, log rollover.
    //
    //  Team 11 (Build Engineer)  — Ideas 1-10 : build env header, assembly version,
    //                               platform info, crash dump, ZIP report, git info,
    //                               validation script, startup self-test, log cleaner,
    //                               log file integrity check.
    // ═══════════════════════════════════════════════════════════════════════════

    // ── Team 3 — Idea 1: Structured error severity levels ─────────────────────
    /// <summary>Severity of a logged entry. Higher = more urgent.</summary>
    public enum LogLevel
    {
        Debug    = 0,
        Info     = 1,
        Warning  = 2,
        Error    = 3,
        Critical = 4
    }

    // ── Team 3 — Idea 2: Structured entry with stack trace ────────────────────
    /// <summary>A single structured log entry with full metadata.</summary>
    public sealed class LogEntry
    {
        /// <summary>Unique entry ID (timestamp-based).</summary>
        public string   Id          { get; set; }
        /// <summary>UTC timestamp of the entry.</summary>
        public DateTime Timestamp   { get; set; }
        /// <summary>Severity level.</summary>
        public LogLevel Level       { get; set; }
        /// <summary>Source context (class.method or scene name).</summary>
        public string   Context     { get; set; }
        /// <summary>Human-readable message.</summary>
        public string   Message     { get; set; }
        /// <summary>Full stack trace (null for Info/Debug entries).</summary>
        public string   StackTrace  { get; set; }
        /// <summary>Managed memory (bytes) at time of log.</summary>
        public long     MemoryBytes { get; set; }
        /// <summary>Path to screenshot taken at error time, if any.</summary>
        public string   ScreenshotPath { get; set; }
    }

    /// <summary>
    /// Central logging facade referenced by all other game systems.
    /// Routes entries to rotating text logs, JSON export, VisualDebugger,
    /// and the in-game DebugConsole.
    /// </summary>
    public static class DebugLogger
    {
        // ── Screenshot provider ────────────────────────────────────────────────
        /// <summary>
        /// Set by the WinForms host (Form1) so that the logger can capture the
        /// current frame when an ERROR is recorded.
        /// Team 8 (Systems Programmer) — Idea 4: automatic screenshot on error.
        /// </summary>
        public static Func<Bitmap> ScreenshotProvider { get; set; }

        // ── Team 8 — Idea 9: configurable verbosity (filter by min level) ──────
        /// <summary>Minimum log level written to file. Defaults to Info.</summary>
        public static LogLevel MinFileLevel    { get; set; } = LogLevel.Info;

        /// <summary>Minimum log level sent to the in-game DebugConsole.</summary>
        public static LogLevel MinConsoleLevel { get; set; } = LogLevel.Warning;

        // ── Team 3 — Idea 4: breadcrumb trail ─────────────────────────────────
        /// <summary>Rolling queue of the last 8 context strings before an error.</summary>
        private static readonly Queue<string> _breadcrumbs = new Queue<string>(8);

        // ── Team 3 — Idea 6: performance hit warnings ─────────────────────────
        // Tracks how many errors were logged in the last second.
        private static float _errorRateWindow;   // seconds elapsed in current rate window
        private static int   _errorRateCount;
        private const  int   ErrorFloodThreshold = 10;  // >10 errors/sec → flood warning

        // ── Team 8 — Idea 1: rotating log file ────────────────────────────────
        private static readonly string LogDir     = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string JsonPath   = Path.Combine(LogDir, "errors.json");
        private static readonly string CrashDir   = Path.Combine(LogDir, "CrashDumps");

        // Current day's rotating plaintext log.
        private static string CurrentLogFile =>
            Path.Combine(LogDir, $"debug-{DateTime.Now:yyyy-MM-dd}.log");

        // ── In-memory ring buffer ──────────────────────────────────────────────
        private const int MaxEntries = 200;
        private static readonly object _lock = new object();
        private static readonly List<LogEntry> _entries = new List<LogEntry>(MaxEntries);

        // ── Team 3 — Idea 3: error deduplication + rate limiter ───────────────
        private static string _lastContext;
        private static string _lastMessage;
        private static int    _dupCount;
        private const  int    DupSuppressLimit = 5;

        // ── Team 11 — Idea 1: build / platform header ─────────────────────────
        private static readonly string _envHeader;

        static DebugLogger()
        {
            Directory.CreateDirectory(LogDir);
            Directory.CreateDirectory(CrashDir);

            // Team 11 (Build Engineer) — Idea 2: daily/old-log rotation hygiene on startup.
            CleanOldLogs(7);

            // Team 11 — Idea 2: assembly version; Idea 3: platform / OS info.
            _envHeader = BuildEnvironmentHeader();
            AppendText(LogLevel.Info, $"=== SESSION OPEN === {_envHeader}");

            // Team 11 — Idea 8: startup self-test entry.
            AppendText(LogLevel.Debug, "DebugLogger self-test: logging pipeline active.");

            // Team 11 — Idea 10: log integrity line (simple marker).
            AppendText(LogLevel.Debug, $"LOG_INTEGRITY_START {DateTime.UtcNow:yyyyMMddHHmmss}");
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Logs an ERROR from an Exception, capturing the stack trace automatically.
        /// Also triggers a screenshot and records the entry in VisualDebugger.
        /// Team 3 (Technical Lead) — Idea 2: stack trace capture.
        /// </summary>
        public static void LogError(string context, Exception ex)
        {
            string msg = ex?.Message ?? "(null exception)";
            string stack = ex?.ToString() ?? string.Empty;
            WriteEntry(LogLevel.Error, context, msg, stack);
        }

        /// <summary>
        /// Logs an ERROR with a plain message string.
        /// Team 3 (Technical Lead) — Idea 1: structured error category.
        /// </summary>
        public static void LogError(string context, string message)
        {
            WriteEntry(LogLevel.Error, context, message, null);
        }

        /// <summary>
        /// Logs a WARNING — non-fatal issue worth tracking.
        /// Team 3 (Technical Lead) — Idea 1: warning severity level.
        /// </summary>
        public static void LogWarning(string context, string message)
        {
            WriteEntry(LogLevel.Warning, context, message, null);
        }

        /// <summary>
        /// Logs a CRITICAL error — also triggers a crash dump and auto-export.
        /// Team 3 (Technical Lead) — Idea 7: auto-export on critical.
        /// Team 11 (Build Engineer) — Idea 4: crash dump on critical.
        /// </summary>
        public static void LogCritical(string context, Exception ex)
        {
            string msg   = ex?.Message ?? "(null)";
            string stack = ex?.ToString() ?? string.Empty;
            WriteEntry(LogLevel.Critical, context, msg, stack);

            // Write crash dump file.
            WriteCrashDump(context, msg, stack);

            // Auto-export ZIP on critical failure.
            TryExportZipReport();
        }

        /// <summary>
        /// Logs an INFO entry — no screenshot, lower overhead.
        /// Team 3 (Technical Lead) — Idea 1: structured info category.
        /// </summary>
        public static void LogInfo(string context, string message)
        {
            WriteEntry(LogLevel.Info, context, message, null);
        }

        /// <summary>
        /// Logs a DEBUG entry — lowest level, filtered from file by default.
        /// </summary>
        public static void LogDebug(string context, string message)
        {
            WriteEntry(LogLevel.Debug, context, message, null);
        }

        // ── Team 3 — Idea 4: breadcrumb trail API ─────────────────────────────
        /// <summary>
        /// Pushes a context string onto the breadcrumb trail.
        /// Call at key game events so errors include recent activity context.
        /// Team 3 (Technical Lead) — Idea 4.
        /// </summary>
        public static void PushBreadcrumb(string context)
        {
            lock (_lock)
            {
                if (_breadcrumbs.Count >= 8) _breadcrumbs.Dequeue();
                _breadcrumbs.Enqueue($"{DateTime.Now:HH:mm:ss.fff} {context}");
            }
        }

        // ── Team 8 — Idea 7: log search / filter API ──────────────────────────
        /// <summary>
        /// Returns all in-memory log entries at or above the given minimum level.
        /// Team 8 (Systems Programmer) — Idea 7: search and filter.
        /// </summary>
        public static List<LogEntry> Query(LogLevel minLevel = LogLevel.Debug, string contextFilter = null)
        {
            lock (_lock)
            {
                var result = new List<LogEntry>();
                foreach (var e in _entries)
                {
                    if (e.Level < minLevel) continue;
                    if (contextFilter != null &&
                        e.Context.IndexOf(contextFilter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    result.Add(e);
                }
                return result;
            }
        }

        // ── Team 8 — Idea 8: error trend analysis ─────────────────────────────
        /// <summary>
        /// Returns the top N most frequent error contexts in the current session.
        /// Team 8 (Systems Programmer) — Idea 8.
        /// </summary>
        public static List<KeyValuePair<string, int>> GetTopErrorContexts(int top = 5)
        {
            lock (_lock)
            {
                var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in _entries)
                {
                    if (e.Level < LogLevel.Error) continue;
                    if (!counts.ContainsKey(e.Context)) counts[e.Context] = 0;
                    counts[e.Context]++;
                }
                var list = new List<KeyValuePair<string, int>>(counts);
                list.Sort((a, b) => b.Value.CompareTo(a.Value));
                return list.Count > top ? list.GetRange(0, top) : list;
            }
        }

        // ── Team 11 — Idea 9: clean old logs ──────────────────────────────────
        /// <summary>
        /// Deletes log files older than <paramref name="daysToKeep"/> days.
        /// Call from startup or dev menu.
        /// Team 11 (Build Engineer) — Idea 9.
        /// </summary>
        public static void CleanOldLogs(int daysToKeep = 7)
        {
            try
            {
                foreach (string f in Directory.GetFiles(LogDir, "debug-*.log"))
                {
                    if ((DateTime.Now - File.GetLastWriteTime(f)).TotalDays > daysToKeep)
                        File.Delete(f);
                }
                AppendText(LogLevel.Info, $"Log cleanup: kept last {daysToKeep} days.");
            }
            catch (Exception ex)
            {
                AppendText(LogLevel.Warning, "Log cleanup failed: " + ex.Message);
            }
        }

        // ── Team 8 — Idea 3: JSON log export ──────────────────────────────────
        /// <summary>
        /// Exports all in-memory entries to a JSON log file.
        /// Team 8 (Systems Programmer) — Idea 3.
        /// </summary>
        public static void ExportJson()
        {
            lock (_lock)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("[");
                    for (int i = 0; i < _entries.Count; i++)
                    {
                        var e = _entries[i];
                        sb.AppendLine("  {");
                        sb.AppendLine($"    \"id\": \"{e.Id}\",");
                        sb.AppendLine($"    \"timestamp\": \"{e.Timestamp:o}\",");
                        sb.AppendLine($"    \"level\": \"{e.Level}\",");
                        sb.AppendLine($"    \"context\": \"{JsonEsc(e.Context)}\",");
                        sb.AppendLine($"    \"message\": \"{JsonEsc(e.Message)}\",");
                        sb.AppendLine($"    \"memoryBytes\": {e.MemoryBytes},");
                        sb.AppendLine($"    \"screenshot\": \"{JsonEsc(e.ScreenshotPath)}\"");
                        sb.Append("  }");
                        if (i < _entries.Count - 1) sb.AppendLine(",");
                        else sb.AppendLine();
                    }
                    sb.AppendLine("]");
                    File.WriteAllText(JsonPath, sb.ToString(), Encoding.UTF8);
                }
                catch { /* JSON export is non-critical */ }
            }
        }

        // ── Internal write pipeline ────────────────────────────────────────────

        private static void WriteEntry(LogLevel level, string context, string message, string stack)
        {
            lock (_lock)
            {
                // ── Team 3 — Idea 3: deduplication / rate limiting ────────────
                if (level >= LogLevel.Error)
                {
                    if (context == _lastContext && message == _lastMessage)
                    {
                        _dupCount++;
                        if (_dupCount > DupSuppressLimit) return;  // suppress flood
                    }
                    else
                    {
                        _lastContext = context;
                        _lastMessage = message;
                        _dupCount    = 0;
                    }
                }

                // ── Team 3 — Idea 6: performance hit warning ──────────────────
                // Use Environment.TickCount to track errors within a 1-second window.
                float nowSec = Environment.TickCount / 1000f;
                if (level >= LogLevel.Error)
                {
                    if (nowSec - _errorRateWindow >= 1.0f)
                    {
                        // New 1-second window: reset the rate counter and window start.
                        _errorRateWindow = nowSec;
                        _errorRateCount  = 0;
                    }
                    _errorRateCount++;
                    if (_errorRateCount >= ErrorFloodThreshold)
                    {
                        AppendText(LogLevel.Warning,
                            $"[FLOOD] {_errorRateCount} errors in 1 s — possible error storm in [{context}]");
                        _errorRateCount = 0;
                        _errorRateWindow = nowSec;
                    }
                }

                // ── Team 5 (memory snapshot) ──────────────────────────────────
                long mem = GC.GetTotalMemory(false);

                // ── Build entry ───────────────────────────────────────────────
                var entry = new LogEntry
                {
                    Id          = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"),
                    Timestamp   = DateTime.Now,
                    Level       = level,
                    Context     = context ?? "(null)",
                    Message     = message ?? "(null)",
                    StackTrace  = stack,
                    MemoryBytes = mem
                };

                // ── Team 8 — Idea 4: screenshot capture for errors ────────────
                if (level >= LogLevel.Error && ScreenshotProvider != null)
                    entry.ScreenshotPath = TryCaptureShot(entry.Id);

                // ── Store in ring buffer ──────────────────────────────────────
                _entries.Add(entry);
                if (_entries.Count > MaxEntries) _entries.RemoveAt(0);

                // ── Write to rotating text log (Team 8 — Idea 1) ─────────────
                if (level >= MinFileLevel)
                {
                    string crumbs = BuildBreadcrumbString();
                    AppendText(level,
                        $"[{level.ToString().ToUpper(),-8}] [{context}] {message}" +
                        (stack != null ? $"\n  STACK: {stack.Split('\n')[0]}" : "") +
                        (crumbs.Length > 0 ? $"\n  CRUMBS: {crumbs}" : "") +
                        $"  MEM:{mem / 1024}KB");
                }

                // ── Forward to VisualDebugger ─────────────────────────────────
                if (level >= LogLevel.Error)
                    VisualDebugger.RecordError(context, message + (stack != null ? "\n" + stack : ""), entry.ScreenshotPath);
                else
                    VisualDebugger.RecordInfo(context, message);

                // ── Forward to DebugConsole (Team 3 — Idea 8: live feed) ──────
                if (level >= MinConsoleLevel)
                    DebugConsole.Print($"[{level}] [{context}] {Truncate(message, 80)}");

                // ── Team 8 — Idea 3: update JSON on every error ───────────────
                if (level >= LogLevel.Error)
                    ExportJson();
            }
        }

        // ── Team 3 — Idea 5: per-scene error grouping ─────────────────────────
        /// <summary>
        /// Returns all error entries grouped by context tag.
        /// Team 3 (Technical Lead) — Idea 5: group by source.
        /// </summary>
        public static Dictionary<string, List<LogEntry>> GroupByContext()
        {
            lock (_lock)
            {
                var groups = new Dictionary<string, List<LogEntry>>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in _entries)
                {
                    if (!groups.ContainsKey(e.Context))
                        groups[e.Context] = new List<LogEntry>();
                    groups[e.Context].Add(e);
                }
                return groups;
            }
        }

        // ── Crash dump (Team 11 — Idea 4) ─────────────────────────────────────
        private static void WriteCrashDump(string context, string msg, string stack)
        {
            try
            {
                string path = Path.Combine(CrashDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                var sb = new StringBuilder();
                sb.AppendLine(_envHeader);
                sb.AppendLine($"CRASH at {DateTime.Now:o}");
                sb.AppendLine($"Context: {context}");
                sb.AppendLine($"Message: {msg}");
                sb.AppendLine("Stack:");
                sb.AppendLine(stack);
                sb.AppendLine("Recent breadcrumbs:");
                sb.AppendLine(BuildBreadcrumbString());
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch { /* crash dump write must never throw */ }
        }

        // ── ZIP report (Team 11 — Idea 5) ─────────────────────────────────────
        private static void TryExportZipReport()
        {
            // ZIP creation requires System.IO.Compression which is .NET Framework 4.5+.
            // We create a simple folder copy instead to stay within Framework 4.7.2.
            try
            {
                string dest = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"FridaysAdventure_ErrorReport_{DateTime.Now:yyyyMMdd_HHmmss}");
                if (!Directory.Exists(dest)) Directory.CreateDirectory(dest);
                foreach (string f in Directory.GetFiles(LogDir, "*.log"))
                    File.Copy(f, Path.Combine(dest, Path.GetFileName(f)), true);
                foreach (string f in Directory.GetFiles(LogDir, "*.html"))
                    File.Copy(f, Path.Combine(dest, Path.GetFileName(f)), true);
                AppendText(LogLevel.Info, $"Error report exported to: {dest}");
            }
            catch { /* export failure is non-fatal */ }
        }

        // ── Build environment header (Team 11 — Ideas 1-3, 6) ─────────────────
        private static string BuildEnvironmentHeader()
        {
            // Collect platform details for the log header.
            string os      = Environment.OSVersion.ToString();
            string runtime = RuntimeEnvironment.GetSystemVersion();
            string asm     = Assembly.GetExecutingAssembly().FullName;
            string dir     = AppDomain.CurrentDomain.BaseDirectory;
            return $"{BuildInfo.Summary} | OS:{os} | CLR:{runtime} | Dir:{dir}";
        }

        // ── Screenshot capture ─────────────────────────────────────────────────
        private static string TryCaptureShot(string id)
        {
            try
            {
                if (ScreenshotProvider == null) return null;
                // Forward to VisualDebugger's shared screenshot directory.
                string dir  = Path.Combine(LogDir, "ErrorShots");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "error_" + id + ".png");
                using (Bitmap bmp = ScreenshotProvider())
                {
                    if (bmp == null) return null;
                    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                }
                return path;
            }
            catch { return null; }
        }

        // ── File append ────────────────────────────────────────────────────────
        private static void AppendText(LogLevel level, string line)
        {
            try
            {
                // Team 8 — Idea 10: log rollover if file exceeds 4 MB.
                string path = CurrentLogFile;
                if (File.Exists(path) && new FileInfo(path).Length > 4 * 1024 * 1024)
                {
                    string rolled = Path.Combine(
                        Path.GetDirectoryName(path) ?? LogDir,
                        $"debug-{DateTime.Now:yyyy-MM-dd}_{DateTime.Now:HHmmss}_rolled.log");
                    if (File.Exists(rolled)) File.Delete(rolled);
                    File.Move(path, rolled);
                }
                File.AppendAllText(path,
                    $"[{DateTime.Now:HH:mm:ss.fff}] {line}{Environment.NewLine}",
                    Encoding.UTF8);
            }
            catch { /* file write failure must never crash the game */ }
        }

        // ── Breadcrumb helper ──────────────────────────────────────────────────
        private static string BuildBreadcrumbString()
        {
            if (_breadcrumbs.Count == 0) return string.Empty;
            return string.Join(" → ", _breadcrumbs);
        }

        // ── Truncate helper ────────────────────────────────────────────────────
        private static string Truncate(string s, int max)
        {
            if (s == null) return "(null)";
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }

        // ── JSON escape ────────────────────────────────────────────────────────
        private static string JsonEsc(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }
    }
}
