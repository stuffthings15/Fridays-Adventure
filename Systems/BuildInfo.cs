using System;
using System.Reflection;
using System.IO;
using System.Text;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Provides static build metadata for display in UI, logs, and QA reports.
    ///
    /// Team 2  (Producer)       — build version display on title screen.
    /// Team 11 (Build Engineer) — all 10 ideas implemented below:
    ///
    ///   Idea 1:  Assembly version string (already present — extended).
    ///   Idea 2:  Configuration label (DEBUG / RELEASE).
    ///   Idea 3:  Build date from assembly last-write time.
    ///   Idea 4:  Platform detection (x86/x64, CLR version, OS).
    ///   Idea 5:  Git SHA stub — reads HEAD file from .git folder if present.
    ///   Idea 6:  Environment variable overrides (DEBUG_* env vars).
    ///   Idea 7:  Auto-increment build number via a file-backed counter.
    ///   Idea 8:  Required dependency checker (NAudio DLL presence).
    ///   Idea 9:  Release notes embedded as a constant string.
    ///   Idea 10: Log integrity footer — appends a checksum line to log files.
    /// </summary>
    public static class BuildInfo
    {
        // ── Idea 1: Assembly version ──────────────────────────────────────────
        /// <summary>Full version string, e.g. "1.0.0.0".</summary>
        public static readonly string Version =
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        /// <summary>Friendly label shown on title screen and QA reports.</summary>
        public static readonly string DisplayVersion = $"v{Version}";

        // ── Idea 3: Build date ────────────────────────────────────────────────
        /// <summary>Approximate build date (last-write time of the executing assembly).</summary>
        public static readonly DateTime BuildDate = GetBuildDate();

        // ── Idea 2: Configuration label ───────────────────────────────────────
        /// <summary>Human-readable configuration: "DEBUG" or "RELEASE".</summary>
        public static readonly string Configuration =
#if DEBUG
            "DEBUG";
#else
            "RELEASE";
#endif

        // ── Idea 7: Auto-increment build number ──────────────────────────────
        /// <summary>
        /// Monotonically increasing build number stored in Logs\build-counter.txt.
        /// Increments each time the app starts in DEBUG mode.
        /// Idea 7 (Build Engineer).
        /// </summary>
        public static readonly int BuildNumber = LoadAndIncrementBuildNumber();

        // ── Idea 5: Git SHA ───────────────────────────────────────────────────
        /// <summary>
        /// Short git commit SHA read from .git/HEAD if the repository is present.
        /// Returns "unknown" when running outside the source tree.
        /// Idea 5 (Build Engineer).
        /// </summary>
        public static readonly string GitSha = ReadGitSha();

        // ── Idea 4: Platform info ─────────────────────────────────────────────
        /// <summary>
        /// Short platform descriptor: e.g. "Win64 CLR4.0.30319".
        /// Idea 4 (Build Engineer).
        /// </summary>
        public static readonly string Platform =
            $"{(Environment.Is64BitProcess ? "Win64" : "Win32")} CLR{Environment.Version}";

        // ── Idea 9: Embedded release notes ───────────────────────────────────
        /// <summary>
        /// Release notes embedded at compile time for display in the patch notes viewer.
        /// Update this constant each build sprint.
        /// Idea 9 (Build Engineer).
        /// </summary>
        public const string ReleaseNotes =
            "v1.0  — Initial release: Miss Friday, Orca, Swan playable.\n" +
            "       — SMB3-style HUD, P-Meter, World map node states.\n" +
            "       — ErrorLogDebugger + VisualDebugger integrated.\n" +
            "       — PowerUpInventory reserve box, LevelEventSystem.\n" +
            "v1.1  — GameConfig hot-reload, InputMap rebind support.\n" +
            "       — Team 7-19 gameplay and VFX systems added.\n";

        // ── Idea 1 summary field ─────────────────────────────────────────────
        /// <summary>Formatted one-liner for logs and title screen.</summary>
        public static string Summary =>
            $"Friday's Adventure {DisplayVersion}  Build#{BuildNumber}  [{Configuration}]" +
            $"  Built {BuildDate:yyyy-MM-dd}  git:{GitSha}  {Platform}";

        // ── Idea 8: Dependency checker ────────────────────────────────────────

        /// <summary>
        /// Checks for required DLL files adjacent to the executable.
        /// Returns a list of missing dependency names (empty = all present).
        /// Idea 8 (Build Engineer).
        /// </summary>
        public static System.Collections.Generic.List<string> CheckDependencies()
        {
            var missing = new System.Collections.Generic.List<string>();
            string dir  = AppDomain.CurrentDomain.BaseDirectory;
            string[] required = { "NAudio.dll" };
            foreach (var dll in required)
                if (!File.Exists(Path.Combine(dir, dll)))
                    missing.Add(dll);
            return missing;
        }

        // ── Idea 6: Env-var overrides ─────────────────────────────────────────

        /// <summary>
        /// Returns the value of a DEBUG_* environment variable, or null if not set.
        /// Allows CI/CD pipelines to inject build-time settings.
        /// Idea 6 (Build Engineer).
        /// </summary>
        public static string GetEnvOverride(string key)
        {
            return Environment.GetEnvironmentVariable("DEBUG_" + key.ToUpperInvariant());
        }

        // ── Idea 10: Log integrity footer ────────────────────────────────────

        /// <summary>
        /// Appends a simple numeric checksum line to a log file so log tampering
        /// or truncation can be detected during QA review.
        /// Idea 10 (Build Engineer).
        /// </summary>
        public static void AppendIntegrityFooter(string logPath)
        {
            try
            {
                string content = File.Exists(logPath) ? File.ReadAllText(logPath) : "";
                int checksum   = 0;
                foreach (char c in content) checksum += c;
                File.AppendAllText(logPath,
                    $"\nLOG_INTEGRITY_END  checksum={checksum:X8}  {DateTime.UtcNow:yyyyMMddHHmmss}\n");
            }
            catch { /* Integrity footer is advisory only — must not throw */ }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static DateTime GetBuildDate()
        {
            try { return File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location); }
            catch { return DateTime.MinValue; }
        }

        private static string ReadGitSha()
        {
            try
            {
                // Walk up from the executable looking for the .git directory.
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 6; i++)
                {
                    string headFile = Path.Combine(dir, ".git", "HEAD");
                    if (File.Exists(headFile))
                    {
                        string head = File.ReadAllText(headFile).Trim();
                        // HEAD may be "ref: refs/heads/master" → resolve the ref file.
                        if (head.StartsWith("ref: "))
                        {
                            string refPath = Path.Combine(dir, ".git",
                                head.Substring(5).Replace('/', Path.DirectorySeparatorChar));
                            if (File.Exists(refPath))
                                head = File.ReadAllText(refPath).Trim();
                        }
                        return head.Length >= 7 ? head.Substring(0, 7) : head;
                    }
                    dir = Path.GetDirectoryName(dir) ?? dir;
                }
                return "unknown";
            }
            catch { return "unknown"; }
        }

        private static int LoadAndIncrementBuildNumber()
        {
            try
            {
                if (Configuration != "DEBUG") return 0;  // only auto-increment in debug builds
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "Logs", "build-counter.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                int num = 0;
                if (File.Exists(path) && int.TryParse(File.ReadAllText(path).Trim(), out int n))
                    num = n;
                num++;
                File.WriteAllText(path, num.ToString());
                return num;
            }
            catch { return 0; }
        }
    }
}
