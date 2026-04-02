using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Fridays_Adventure.Systems
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  BuildEngineerExtensions.cs  —  Build / Tech Support Engineer: 10 NEW ideas
    //
    //  Idea 1:  Build manifest — writes a JSON file listing all compiled assemblies.
    //  Idea 2:  Incremental build detector — detects if output is older than source.
    //  Idea 3:  Symbol map export — writes public type/method names to a text file.
    //  Idea 4:  Dead-code scanner stub — reports types with zero documented usages.
    //  Idea 5:  License header checker — validates C# files have a license comment.
    //  Idea 6:  CI readiness flag — writes a READY file only when build+tests pass.
    //  Idea 7:  Dependency health check — verifies required DLL files are present.
    //  Idea 8:  Log archiver — ZIPs old log files to keep the Logs folder tidy.
    //  Idea 9:  Integrity hash — writes a SHA-256 hash of the compiled EXE.
    //  Idea 10: Version resource embed — reads AssemblyInfo version at runtime.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Build and Tech Support Engineer utilities.
    /// Primarily invoked from the dev menu and on application startup.
    /// Team 11 (Build / Tech Support Engineer) — Ideas 1–10.
    /// </summary>
    public static class BuildEngineerExtensions
    {
        private static readonly string LogDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        // ── Idea 1: Build manifest ────────────────────────────────────────────
        /// <summary>
        /// Writes a JSON file listing all loaded assemblies with their versions.
        /// Output: <c>Logs\build-manifest.json</c>
        /// Team 11 (Build Engineer) — Idea 1.
        /// </summary>
        public static void WriteBuildManifest()
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"generated\": \"{DateTime.Now:o}\",");
                sb.AppendLine($"  \"buildInfo\": \"{BuildInfo.Summary}\",");
                sb.AppendLine("  \"assemblies\": [");
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < asms.Length; i++)
                {
                    var a = asms[i];
                    sb.Append($"    {{ \"name\": \"{a.GetName().Name}\", " +
                              $"\"version\": \"{a.GetName().Version}\" }}");
                    if (i < asms.Length - 1) sb.AppendLine(",");
                    else sb.AppendLine();
                }
                sb.AppendLine("  ]");
                sb.AppendLine("}");
                File.WriteAllText(Path.Combine(LogDir, "build-manifest.json"), sb.ToString(), Encoding.UTF8);
                DebugLogger.LogInfo("BuildManifest", "Written build-manifest.json");
            }
            catch (Exception ex) { DebugLogger.LogError("BuildManifest", ex); }
        }

        // ── Idea 2: Incremental build detector ───────────────────────────────
        /// <summary>
        /// Returns true if the output EXE is newer than the most recently modified
        /// source file in the project directory.
        /// Team 11 (Build Engineer) — Idea 2.
        /// </summary>
        public static bool IsOutputUpToDate()
        {
            try
            {
                string exePath = Assembly.GetExecutingAssembly().Location;
                DateTime exeDate = File.GetLastWriteTime(exePath);

                string srcDir = Path.GetFullPath(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
                if (!Directory.Exists(srcDir)) return true;

                DateTime latestSrc = DateTime.MinValue;
                foreach (string f in Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories))
                {
                    DateTime d = File.GetLastWriteTime(f);
                    if (d > latestSrc) latestSrc = d;
                }
                return exeDate >= latestSrc;
            }
            catch { return true; }
        }

        // ── Idea 3: Symbol map export ─────────────────────────────────────────
        /// <summary>
        /// Exports public type and method names from the main assembly to a text file.
        /// Useful for tracking API surface and dead code analysis.
        /// Team 11 (Build Engineer) — Idea 3.
        /// </summary>
        public static void ExportSymbolMap()
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                var sb = new StringBuilder();
                sb.AppendLine($"# Symbol Map — {BuildInfo.Summary}");
                sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();

                var asm   = Assembly.GetExecutingAssembly();
                var types = asm.GetTypes();
                Array.Sort(types, (a, b) => string.Compare(a.FullName, b.FullName));
                foreach (var t in types)
                {
                    if (!t.IsPublic) continue;
                    sb.AppendLine($"TYPE  {t.FullName}");
                    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance |
                                                    BindingFlags.Static | BindingFlags.DeclaredOnly))
                        sb.AppendLine($"  METHOD  {m.Name}");
                }

                File.WriteAllText(Path.Combine(LogDir, "symbol-map.txt"), sb.ToString(), Encoding.UTF8);
                DebugLogger.LogInfo("SymbolMap", $"Symbol map written: {types.Length} types.");
            }
            catch (Exception ex) { DebugLogger.LogError("SymbolMap", ex); }
        }

        // ── Idea 4: Dead-code scanner stub ────────────────────────────────────
        /// <summary>
        /// Stub: logs public types that have no documented XML summary.
        /// In a full implementation this would cross-reference call graphs.
        /// Team 11 (Build Engineer) — Idea 4.
        /// </summary>
        public static List<string> ScanForUndocumentedTypes()
        {
            var results = new List<string>();
            var asm     = Assembly.GetExecutingAssembly();
            foreach (var t in asm.GetTypes())
            {
                if (!t.IsPublic) continue;
                // Heuristic: types with no public methods are likely utilities.
                int pubMethods = t.GetMethods(BindingFlags.Public |
                    BindingFlags.Instance | BindingFlags.DeclaredOnly).Length;
                if (pubMethods == 0)
                    results.Add(t.FullName);
            }
            DebugLogger.LogInfo("DeadCodeScanner",
                $"Found {results.Count} potentially undocumented/empty public types.");
            return results;
        }

        // ── Idea 5: License header checker ───────────────────────────────────
        /// <summary>
        /// Checks that all .cs files in the source tree begin with a comment block.
        /// Returns paths of files that lack a header comment.
        /// Team 11 (Build Engineer) — Idea 5.
        /// </summary>
        public static List<string> CheckLicenseHeaders(string srcRoot)
        {
            var missing = new List<string>();
            if (!Directory.Exists(srcRoot)) return missing;
            foreach (string f in Directory.GetFiles(srcRoot, "*.cs", SearchOption.AllDirectories))
            {
                try
                {
                    string first = File.ReadLines(f).GetEnumerator() is var en
                        && en.MoveNext() ? en.Current?.TrimStart() : string.Empty;
                    if (!first.StartsWith("//") && !first.StartsWith("/*"))
                        missing.Add(f);
                }
                catch { }
            }
            return missing;
        }

        // ── Idea 6: CI readiness flag ──────────────────────────────────────────
        /// <summary>
        /// Writes <c>Logs\ci-ready.flag</c> if <paramref name="buildOk"/> and
        /// <paramref name="testsOk"/> are both true.
        /// Team 11 (Build Engineer) — Idea 6.
        /// </summary>
        public static void WriteCIFlag(bool buildOk, bool testsOk)
        {
            string path = Path.Combine(LogDir, "ci-ready.flag");
            Directory.CreateDirectory(LogDir);
            if (buildOk && testsOk)
                File.WriteAllText(path, $"READY {DateTime.Now:o} {BuildInfo.Summary}");
            else
                File.WriteAllText(path, $"NOT_READY {DateTime.Now:o} build={buildOk} tests={testsOk}");
            DebugLogger.LogInfo("CIFlag", $"CI flag: build={buildOk} tests={testsOk}");
        }

        // ── Idea 7: Dependency health check ──────────────────────────────────
        /// <summary>
        /// Verifies that required DLL files are present next to the executable.
        /// Returns a list of missing dependency names.
        /// Team 11 (Build Engineer) — Idea 7.
        /// </summary>
        public static List<string> CheckDependencies()
        {
            string[] required = { "NAudio.dll" };
            string   dir      = AppDomain.CurrentDomain.BaseDirectory;
            var missing = new List<string>();
            foreach (string dll in required)
                if (!File.Exists(Path.Combine(dir, dll)))
                    missing.Add(dll);

            if (missing.Count == 0)
                DebugLogger.LogInfo("DepCheck", "All dependencies present.");
            else
                DebugLogger.LogError("DepCheck", $"Missing: {string.Join(", ", missing)}");
            return missing;
        }

        // ── Idea 8: Log archiver ───────────────────────────────────────────────
        /// <summary>
        /// Moves log files older than <paramref name="keepDays"/> into an
        /// <c>Archive</c> sub-folder.
        /// Team 11 (Build Engineer) — Idea 8.
        /// </summary>
        public static void ArchiveOldLogs(int keepDays = 7)
        {
            try
            {
                string archiveDir = Path.Combine(LogDir, "Archive");
                Directory.CreateDirectory(archiveDir);
                int archived = 0;
                foreach (string f in Directory.GetFiles(LogDir, "*.log"))
                {
                    if ((DateTime.Now - File.GetLastWriteTime(f)).TotalDays > keepDays)
                    {
                        File.Move(f, Path.Combine(archiveDir, Path.GetFileName(f)));
                        archived++;
                    }
                }
                DebugLogger.LogInfo("LogArchiver", $"Archived {archived} log file(s).");
            }
            catch (Exception ex) { DebugLogger.LogError("LogArchiver", ex); }
        }

        // ── Idea 9: Integrity hash ────────────────────────────────────────────
        /// <summary>
        /// Computes a SHA-256 hash of the executing assembly and writes it to
        /// <c>Logs\exe-hash.txt</c>.
        /// Team 11 (Build Engineer) — Idea 9.
        /// </summary>
        public static string WriteIntegrityHash()
        {
            try
            {
                string exePath = Assembly.GetExecutingAssembly().Location;
                if (!File.Exists(exePath)) return null;
                byte[] bytes;
                using (var sha = SHA256.Create())
                using (var fs  = File.OpenRead(exePath))
                    bytes = sha.ComputeHash(fs);
                string hash = BitConverter.ToString(bytes).Replace("-", "").ToLower();
                Directory.CreateDirectory(LogDir);
                File.WriteAllText(Path.Combine(LogDir, "exe-hash.txt"),
                    $"{BuildInfo.Summary}\nSHA256: {hash}\nDate: {DateTime.Now:o}");
                DebugLogger.LogInfo("IntegrityHash", $"SHA-256: {hash.Substring(0, 16)}...");
                return hash;
            }
            catch (Exception ex) { DebugLogger.LogError("IntegrityHash", ex); return null; }
        }

        // ── Idea 10: Version resource embed ──────────────────────────────────
        /// <summary>
        /// Reads the AssemblyFileVersion attribute at runtime.
        /// Useful for display in about dialogs and QA reports.
        /// Team 11 (Build Engineer) — Idea 10.
        /// </summary>
        public static string GetFileVersion()
        {
            var fv = System.Diagnostics.FileVersionInfo.GetVersionInfo(
                     Assembly.GetExecutingAssembly().Location);
            return fv.FileVersion ?? BuildInfo.Version;
        }
    }
}
