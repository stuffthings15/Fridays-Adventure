// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 8: Systems Programmer
// Feature: Systems Services Pack
// Purpose: Implements localization, analytics, config validation, DLC loading,
//          patch management, cloud-save sync, mod loading, replay capture,
//          language packs, and statistics aggregation.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Phase 2 localization lookup service.
    /// </summary>
    /// <remarks>PHASE 2 - Team 8: Localization System</remarks>
    public static class Phase2LocalizationSystem
    {
        /// <summary>Translates a key via LanguagePackManager with fallback.</summary>
        /// <remarks>PHASE 2 - Team 8: Localization System</remarks>
        public static string T(string key, string fallback) => LanguagePackManager.T(key, fallback);
    }

    /// <summary>
    /// Analytics event logger for phase 2 instrumentation.
    /// </summary>
    /// <remarks>PHASE 2 - Team 8: Analytics Event Logger</remarks>
    public static class AnalyticsEventLogger
    {
        private static readonly string PathCsv = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "phase2-analytics-events.csv");

        /// <summary>Writes one analytics event record.</summary>
        /// <remarks>PHASE 2 - Team 8: Analytics Event Logger</remarks>
        public static void Log(string eventName, string payload)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PathCsv));
            string e = (eventName ?? "event").Replace(',', ' ');
            string p = (payload ?? string.Empty).Replace(',', ' ').Replace('\n', ' ').Replace('\r', ' ');
            File.AppendAllText(PathCsv, $"{DateTime.UtcNow:o},{e},{p}{Environment.NewLine}", Encoding.UTF8);
        }

        /// <summary>Returns total number of logged analytics rows.</summary>
        /// <remarks>PHASE 2 - Team 8: Analytics Event Logger</remarks>
        public static int Count() => File.Exists(PathCsv) ? File.ReadAllLines(PathCsv, Encoding.UTF8).Length : 0;
    }

    /// <summary>
    /// Configuration validator for core game settings.
    /// </summary>
    /// <remarks>PHASE 2 - Team 8: Configuration Validator</remarks>
    public static class ConfigurationValidator
    {
        /// <summary>Returns validation messages for key runtime config fields.</summary>
        /// <remarks>PHASE 2 - Team 8: Configuration Validator</remarks>
        public static IReadOnlyList<string> Validate()
        {
            var list = new List<string>();
            var game = Engine.Game.Instance;
            if (game?.Audio != null)
            {
                if (game.Audio.MusicVolume < 0 || game.Audio.MusicVolume > 100)
                    list.Add("MusicVolume out of range [0..100]");
                if (game.Audio.SfxVolume < 0 || game.Audio.SfxVolume > 100)
                    list.Add("SfxVolume out of range [0..100]");
            }
            else
            {
                list.Add("Audio manager unavailable.");
            }
            if (list.Count == 0) list.Add("Config validation passed.");
            return list;
        }
    }

    /// <summary>
    /// DLC content loader helper.
    /// </summary>
    /// <remarks>PHASE 2 - Team 8: DLC Content Loader</remarks>
    public static class DlcContentLoader
    {
        /// <summary>Returns detected DLC package names.</summary>
        /// <remarks>PHASE 2 - Team 8: DLC Content Loader</remarks>
        public static IReadOnlyList<string> GetPackages() => DlcDetectionSystem.GetInstalledPackages();
    }

    /// <summary>
    /// Patch manager queue for local patch packages.
    /// </summary>
    /// <remarks>PHASE 2 - Team 8: Patch Manager</remarks>
    public static class PatchManager
    {
        /// <summary>Returns discoverable patch files from PatchDistributionSystem.</summary>
        /// <remarks>PHASE 2 - Team 8: Patch Manager</remarks>
        public static IReadOnlyList<string> Discover() => PatchDistributionSystem.Discover();

        /// <summary>Applies first pending patch and returns status text.</summary>
        /// <remarks>PHASE 2 - Team 8: Patch Manager</remarks>
        public static string ApplyFirst()
        {
            var p = Discover();
            if (p.Count == 0) return "No patch packages found.";
            var applied = PatchDistributionSystem.GetApplied();
            string candidate = p.FirstOrDefault(x => !applied.Contains(x));
            if (string.IsNullOrWhiteSpace(candidate)) return "All discovered patches already marked applied.";
            PatchDistributionSystem.MarkApplied(candidate);
            return "Applied: " + candidate;
        }
    }

    /// <summary>
    /// Cloud save integration stub using local sync snapshots.
    /// </summary>
    /// <remarks>PHASE 2 - Team 8: Cloud Save Integration</remarks>
    public static class CloudSaveIntegration
    {
        /// <summary>Creates an outbound sync snapshot and returns path.</summary>
        /// <remarks>PHASE 2 - Team 8: Cloud Save Integration</remarks>
        public static string Upload() => CrossPlatformSync.ExportSnapshot();

        /// <summary>Imports latest sync snapshot and returns success flag.</summary>
        /// <remarks>PHASE 2 - Team 8: Cloud Save Integration</remarks>
        public static bool Download() => CrossPlatformSync.ImportSnapshot();
    }

    /// <summary>
    /// Mod loader adapter for installed mod metadata.
    /// </summary>
    /// <remarks>PHASE 2 - Team 8: Mod Loader System</remarks>
    public static class ModLoaderSystem
    {
        /// <summary>Returns enabled mod identifiers.</summary>
        /// <remarks>PHASE 2 - Team 8: Mod Loader System</remarks>
        public static IReadOnlyList<string> EnabledIds()
            => ModMetadataSystem.LoadAll().Where(m => m.Enabled).Select(m => m.Id).ToList();
    }

    /// <summary>
    /// Event replay recorder file utility.
    /// </summary>
    /// <remarks>PHASE 2 - Team 8: Event Replay Recorder</remarks>
    public static class EventReplayRecorder
    {
        private static readonly string ReplayPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "phase2-event-replay.log");

        /// <summary>Appends one replay event line.</summary>
        /// <remarks>PHASE 2 - Team 8: Event Replay Recorder</remarks>
        public static void Record(string evt)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReplayPath));
            File.AppendAllText(ReplayPath, $"{DateTime.UtcNow:o}|{evt}{Environment.NewLine}", Encoding.UTF8);
        }

        /// <summary>Returns last replay lines.</summary>
        /// <remarks>PHASE 2 - Team 8: Event Replay Recorder</remarks>
        public static IReadOnlyList<string> Tail(int max = 10)
        {
            if (!File.Exists(ReplayPath)) return new[] { "No replay events yet." };
            var lines = File.ReadAllLines(ReplayPath, Encoding.UTF8);
            int skip = Math.Max(0, lines.Length - Math.Max(1, max));
            return lines.Skip(skip).ToList();
        }
    }

    /// <summary>
    /// Language pack system helper.
    /// </summary>
    /// <remarks>PHASE 2 - Team 8: Language Pack System</remarks>
    public static class LanguagePackSystem
    {
        /// <summary>Returns available language codes.</summary>
        /// <remarks>PHASE 2 - Team 8: Language Pack System</remarks>
        public static IReadOnlyList<string> Available() => LanguagePackManager.GetAvailableLanguages();

        /// <summary>Cycles to next available language and returns new code.</summary>
        /// <remarks>PHASE 2 - Team 8: Language Pack System</remarks>
        public static string CycleNext()
        {
            var langs = Available();
            if (langs.Count == 0) return LanguagePackManager.CurrentLanguage;
            int i = langs.ToList().FindIndex(x => x.Equals(LanguagePackManager.CurrentLanguage, StringComparison.OrdinalIgnoreCase));
            int next = (i + 1) % langs.Count;
            LanguagePackManager.SetLanguage(langs[next]);
            return langs[next];
        }
    }

    /// <summary>
    /// Statistics aggregator for session + profile metrics.
    /// </summary>
    /// <remarks>PHASE 2 - Team 8: Statistics Aggregator</remarks>
    public static class StatisticsAggregator
    {
        /// <summary>Returns aggregate metric lines.</summary>
        /// <remarks>PHASE 2 - Team 8: Statistics Aggregator</remarks>
        public static IReadOnlyList<string> Summary()
        {
            var s = SessionStats.Instance;
            var p = PlayerProfileSystem.Load();
            return new[]
            {
                $"PlayTime={s.PlayTimeFormatted}",
                $"Deaths={s.DeathCount}",
                $"Enemies={s.EnemiesDefeated}",
                $"Bosses={s.BossesDefeated}",
                $"ProfileXP={p.TotalXp}",
                $"SeasonTier={p.SeasonTier}",
            };
        }
    }
}
