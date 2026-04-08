// ────────────────────────────────────────────
// PHASE 2 - Team 8: Systems Programmer
// Feature: Asset Gap Detector
// Purpose: Tracks all sprite and audio references that returned null
//          at runtime, building a structured requirements list for
//          the self-healing asset pipeline to resolve.
// ────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fridays_Adventure.Data
{
    /// <summary>
    /// Categories of missing assets for targeted resolution.
    /// </summary>
    public enum AssetCategory
    {
        Unknown,
        PlayerCharacter,
        Enemy,
        Boss,
        Portrait,
        Background,
        UI,
        Tile,
        Audio
    }

    /// <summary>
    /// A single detected missing asset requirement.
    /// </summary>
    public sealed class MissingAssetEntry
    {
        /// <summary>Filename that was requested but not found.</summary>
        public string FileName { get; set; }

        /// <summary>Inferred asset category based on filename prefix.</summary>
        public AssetCategory Category { get; set; }

        /// <summary>Whether the pipeline successfully resolved this gap.</summary>
        public bool Resolved { get; set; }

        /// <summary>Path to the resolved replacement file, if any.</summary>
        public string ResolvedPath { get; set; }

        /// <summary>How many times this file was requested and missed.</summary>
        public int MissCount { get; set; }

        /// <summary>UTC timestamp of the first miss.</summary>
        public DateTime FirstMissUtc { get; set; }

        /// <summary>Keywords extracted from the filename for vendor matching.</summary>
        public List<string> Keywords { get; set; } = new List<string>();
    }

    /// <summary>
    /// Detects and catalogues missing asset references at runtime.
    /// Thread-safe — misses can be recorded from any thread.
    /// <remarks>PHASE 2 - Team 8: Self-Healing Asset Pipeline — Gap Detection</remarks>
    /// </summary>
    public static class AssetGapDetector
    {
        // ── All detected misses, keyed by lowercase filename ─────────
        private static readonly Dictionary<string, MissingAssetEntry> _misses =
            new Dictionary<string, MissingAssetEntry>(StringComparer.OrdinalIgnoreCase);

        private static readonly object _lock = new object();

        // ── Public API ───────────────────────────────────────────────

        /// <summary>
        /// Records a sprite/audio file that was requested but not found.
        /// Safe to call from any thread. Deduplicates by filename.
        /// </summary>
        public static void RecordMiss(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            lock (_lock)
            {
                if (_misses.TryGetValue(fileName, out var existing))
                {
                    existing.MissCount++;
                    return;
                }

                var entry = new MissingAssetEntry
                {
                    FileName     = fileName,
                    Category     = InferCategory(fileName),
                    MissCount    = 1,
                    FirstMissUtc = DateTime.UtcNow,
                    Keywords     = ExtractKeywords(fileName)
                };
                _misses[fileName] = entry;
            }
        }

        /// <summary>
        /// Returns a snapshot of all currently unresolved missing assets.
        /// </summary>
        public static List<MissingAssetEntry> GetUnresolvedMisses()
        {
            lock (_lock)
            {
                var result = new List<MissingAssetEntry>();
                foreach (var kv in _misses)
                {
                    if (!kv.Value.Resolved)
                        result.Add(kv.Value);
                }
                return result;
            }
        }

        /// <summary>
        /// Returns a snapshot of ALL missing asset entries (resolved + unresolved).
        /// </summary>
        public static List<MissingAssetEntry> GetAllMisses()
        {
            lock (_lock)
            {
                return new List<MissingAssetEntry>(_misses.Values);
            }
        }

        /// <summary>
        /// Marks a missing asset as resolved with the given replacement path.
        /// </summary>
        public static void MarkResolved(string fileName, string resolvedPath)
        {
            lock (_lock)
            {
                if (_misses.TryGetValue(fileName, out var entry))
                {
                    entry.Resolved     = true;
                    entry.ResolvedPath = resolvedPath;
                }
            }
        }

        /// <summary>
        /// Clears all tracked misses. Used after a full healing cycle.
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock) { _misses.Clear(); }
        }

        /// <summary>Total number of unique missing assets detected.</summary>
        public static int TotalMisses
        {
            get { lock (_lock) { return _misses.Count; } }
        }

        /// <summary>Number of misses that have been resolved.</summary>
        public static int ResolvedCount
        {
            get
            {
                lock (_lock)
                {
                    int c = 0;
                    foreach (var kv in _misses)
                        if (kv.Value.Resolved) c++;
                    return c;
                }
            }
        }

        /// <summary>
        /// Generates a plaintext report of all detected gaps and their resolution status.
        /// </summary>
        public static string GenerateReport()
        {
            lock (_lock)
            {
                var sb = new StringBuilder();
                sb.AppendLine("════════════════════════════════════════════════════════════");
                sb.AppendLine("ASSET GAP DETECTION REPORT");
                sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine("════════════════════════════════════════════════════════════");
                sb.AppendLine();
                sb.AppendLine($"Total unique missing assets: {_misses.Count}");

                int resolved = 0;
                foreach (var kv in _misses)
                    if (kv.Value.Resolved) resolved++;

                sb.AppendLine($"Resolved: {resolved} / {_misses.Count}");
                sb.AppendLine();

                // Group by category
                var groups = new Dictionary<AssetCategory, List<MissingAssetEntry>>();
                foreach (var kv in _misses)
                {
                    var cat = kv.Value.Category;
                    if (!groups.ContainsKey(cat))
                        groups[cat] = new List<MissingAssetEntry>();
                    groups[cat].Add(kv.Value);
                }

                foreach (var group in groups)
                {
                    sb.AppendLine($"── {group.Key} ({group.Value.Count}) ──");
                    foreach (var entry in group.Value)
                    {
                        string status = entry.Resolved
                            ? $"✅ → {entry.ResolvedPath}"
                            : "❌ UNRESOLVED";
                        sb.AppendLine($"  {entry.FileName}  (hits: {entry.MissCount})  {status}");
                        if (entry.Keywords.Count > 0)
                            sb.AppendLine($"    Keywords: {string.Join(", ", entry.Keywords)}");
                    }
                    sb.AppendLine();
                }

                return sb.ToString();
            }
        }

        // ── Category inference from filename conventions ──────────────

        /// <summary>
        /// Infers the asset category from the filename prefix/pattern.
        /// </summary>
        private static AssetCategory InferCategory(string fileName)
        {
            string lower = fileName.ToLowerInvariant();

            if (lower.StartsWith("player_"))    return AssetCategory.PlayerCharacter;
            if (lower.StartsWith("enemy_"))     return AssetCategory.Enemy;
            if (lower.StartsWith("boss_"))      return AssetCategory.Boss;
            if (lower.StartsWith("portrait_"))  return AssetCategory.Portrait;
            if (lower.StartsWith("bg_"))        return AssetCategory.Background;
            if (lower.StartsWith("ui_"))        return AssetCategory.UI;
            if (lower.StartsWith("tile_"))      return AssetCategory.Tile;

            // Audio detection
            string ext = Path.GetExtension(lower);
            if (ext == ".wav" || ext == ".mp3" || ext == ".ogg")
                return AssetCategory.Audio;

            if (lower.StartsWith("music_") || lower.StartsWith("sfx_"))
                return AssetCategory.Audio;

            return AssetCategory.Unknown;
        }

        /// <summary>
        /// Extracts search keywords from a filename for vendor asset matching.
        /// "enemy_goomba.png" → ["enemy", "goomba"]
        /// "bg_Centipede_of_the_Deep.png" → ["centipede", "deep", "background"]
        /// </summary>
        private static List<string> ExtractKeywords(string fileName)
        {
            // Remove extension
            string name = Path.GetFileNameWithoutExtension(fileName);

            // Split by underscores, hyphens, spaces, camelCase
            var parts = new List<string>();
            foreach (string segment in name.Split('_', '-', ' '))
            {
                string lower = segment.ToLowerInvariant().Trim();
                if (string.IsNullOrEmpty(lower)) continue;
                // Skip noise words
                if (lower == "of" || lower == "the" || lower == "a" || lower == "an") continue;
                parts.Add(lower);
            }

            // Add semantic aliases for common prefixes
            if (fileName.StartsWith("bg_", StringComparison.OrdinalIgnoreCase))
                parts.Add("background");
            if (fileName.StartsWith("enemy_", StringComparison.OrdinalIgnoreCase))
                parts.Add("character");
            if (fileName.StartsWith("player_", StringComparison.OrdinalIgnoreCase))
                parts.Add("character");

            return parts;
        }
    }
}
