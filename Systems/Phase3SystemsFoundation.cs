// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 8: Systems Programmer
// Feature: Foundation Systems Pack (Mods/DLC/Profile/Season Pass/Migration)
// Purpose: Provide core data systems for Wave 1 expansion features.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fridays_Adventure.Systems
{
    /// <summary>Metadata row describing one installed mod package.</summary>
    public sealed class ModMetadata
    {
        /// <summary>Unique mod id.</summary>
        public string Id { get; set; }
        /// <summary>Human readable mod name.</summary>
        public string Name { get; set; }
        /// <summary>Semantic-ish version string.</summary>
        public string Version { get; set; }
        /// <summary>Author or team.</summary>
        public string Author { get; set; }
        /// <summary>Feature tags or categories.</summary>
        public string Tags { get; set; }
        /// <summary>Absolute package path.</summary>
        public string Path { get; set; }
        /// <summary>Whether this mod is enabled at runtime.</summary>
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// Team 8 Idea 1: Mod Metadata System.
    /// Loads and stores lightweight mod descriptors from <c>Mods\*.modinfo</c> files.
    /// </summary>
    public static class ModMetadataSystem
    {
        private static readonly string ModsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods");
        private static readonly string EnabledPath = Path.Combine(ModsDir, "enabled-mods.cfg");

        /// <summary>Loads all mod metadata files from disk.</summary>
        public static IReadOnlyList<ModMetadata> LoadAll()
        {
            Directory.CreateDirectory(ModsDir);
            EnsureSampleMod();

            var enabled = LoadEnabledIds();
            var list = new List<ModMetadata>();
            foreach (string f in Directory.GetFiles(ModsDir, "*.modinfo"))
            {
                var map = ParseKeyValueFile(f);
                string id = map.ContainsKey("id") ? map["id"] : Path.GetFileNameWithoutExtension(f);
                list.Add(new ModMetadata
                {
                    Id = id,
                    Name = map.ContainsKey("name") ? map["name"] : id,
                    Version = map.ContainsKey("version") ? map["version"] : "1.0.0",
                    Author = map.ContainsKey("author") ? map["author"] : "Unknown",
                    Tags = map.ContainsKey("tags") ? map["tags"] : "",
                    Path = f,
                    Enabled = enabled.Contains(id)
                });
            }
            return list.OrderBy(m => m.Name).ToList();
        }

        /// <summary>Toggles the enabled state of a mod id and saves settings.</summary>
        public static void SetEnabled(string modId, bool enabled)
        {
            var ids = LoadEnabledIds();
            if (enabled) ids.Add(modId);
            else ids.Remove(modId);
            SaveEnabledIds(ids);
            DebugLogger.LogInfo("ModMetadataSystem", $"{modId} enabled={enabled}");
        }

        private static HashSet<string> LoadEnabledIds()
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(EnabledPath)) return ids;
            foreach (string line in File.ReadAllLines(EnabledPath, Encoding.UTF8))
            {
                string t = line.Trim();
                if (string.IsNullOrWhiteSpace(t) || t.StartsWith("#")) continue;
                ids.Add(t);
            }
            return ids;
        }

        private static void SaveEnabledIds(HashSet<string> ids)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Enabled mods");
            foreach (string id in ids.OrderBy(x => x)) sb.AppendLine(id);
            File.WriteAllText(EnabledPath, sb.ToString(), Encoding.UTF8);
        }

        private static Dictionary<string, string> ParseKeyValueFile(string path)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string k = line.Substring(0, eq).Trim();
                string v = line.Substring(eq + 1).Trim();
                map[k] = v;
            }
            return map;
        }

        private static void EnsureSampleMod()
        {
            string sample = Path.Combine(ModsDir, "sample-ui-pack.modinfo");
            if (File.Exists(sample)) return;
            File.WriteAllText(sample,
                "id=sample-ui-pack\nname=Sample UI Pack\nversion=1.0.0\nauthor=Fridays Team\ntags=ui,theme\n",
                Encoding.UTF8);
        }
    }

    /// <summary>
    /// Team 8 Idea 7: DLC Detection System.
    /// Detects installed DLC markers from <c>Assets\DLC\*.dlc</c> files.
    /// </summary>
    public static class DlcDetectionSystem
    {
        private static readonly string DlcDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "DLC");

        /// <summary>Returns installed DLC package names.</summary>
        public static IReadOnlyList<string> GetInstalledPackages()
        {
            Directory.CreateDirectory(DlcDir);
            EnsureSampleDlc();
            return Directory.GetFiles(DlcDir, "*.dlc")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(x => x)
                .ToList();
        }

        /// <summary>Returns true when a named DLC package is installed.</summary>
        public static bool IsInstalled(string packageName)
        {
            return GetInstalledPackages().Any(x => x.Equals(packageName, StringComparison.OrdinalIgnoreCase));
        }

        private static void EnsureSampleDlc()
        {
            string sample = Path.Combine(DlcDir, "story-pack-alpha.dlc");
            if (!File.Exists(sample))
                File.WriteAllText(sample, "phase3_story_pack=true", Encoding.UTF8);
        }
    }

    /// <summary>Persistent player profile model for Phase 3 progression UI.</summary>
    public sealed class PlayerProfile
    {
        /// <summary>Profile display name.</summary>
        public string DisplayName { get; set; }
        /// <summary>Total accumulated profile XP.</summary>
        public int TotalXp { get; set; }
        /// <summary>Cosmetic inventory count.</summary>
        public int CosmeticCount { get; set; }
        /// <summary>Current season pass tier.</summary>
        public int SeasonTier { get; set; }
    }

    /// <summary>
    /// Team 8 Idea 8: Player Profile System.
    /// Loads/saves lightweight profile state to <c>Logs\player-profile.cfg</c>.
    /// </summary>
    public static class PlayerProfileSystem
    {
        private static readonly string ProfilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "player-profile.cfg");

        /// <summary>Loads profile from disk or creates default profile.</summary>
        public static PlayerProfile Load()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath));
            if (!File.Exists(ProfilePath))
            {
                var p = new PlayerProfile { DisplayName = "Captain", TotalXp = 0, CosmeticCount = 0, SeasonTier = 1 };
                Save(p);
                return p;
            }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in File.ReadAllLines(ProfilePath, Encoding.UTF8))
            {
                string line = raw.Trim();
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                map[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }

            return new PlayerProfile
            {
                DisplayName = map.ContainsKey("displayName") ? map["displayName"] : "Captain",
                TotalXp = ParseInt(map, "totalXp", 0),
                CosmeticCount = ParseInt(map, "cosmeticCount", 0),
                SeasonTier = ParseInt(map, "seasonTier", 1),
            };
        }

        /// <summary>Saves the profile data to disk.</summary>
        public static void Save(PlayerProfile profile)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"displayName={profile.DisplayName ?? "Captain"}");
            sb.AppendLine($"totalXp={Math.Max(0, profile.TotalXp)}");
            sb.AppendLine($"cosmeticCount={Math.Max(0, profile.CosmeticCount)}");
            sb.AppendLine($"seasonTier={Math.Max(1, profile.SeasonTier)}");
            File.WriteAllText(ProfilePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>Adds profile XP and persists new state.</summary>
        public static PlayerProfile AddXp(int amount)
        {
            var p = Load();
            p.TotalXp = Math.Max(0, p.TotalXp + amount);
            p.SeasonTier = SeasonPassManager.CalculateTier(p.TotalXp);
            Save(p);
            return p;
        }

        private static int ParseInt(Dictionary<string, string> map, string key, int fallback)
        {
            return map.ContainsKey(key) && int.TryParse(map[key], out int v) ? v : fallback;
        }
    }

    /// <summary>
    /// Team 8 Idea 9: Season Pass Manager.
    /// Converts XP to tier and provides next-tier threshold info.
    /// </summary>
    public static class SeasonPassManager
    {
        private const int XpPerTier = 500;
        private const int MaxTier = 100;

        /// <summary>Calculates current tier from cumulative XP.</summary>
        public static int CalculateTier(int totalXp)
        {
            int tier = 1 + Math.Max(0, totalXp) / XpPerTier;
            return Math.Min(MaxTier, tier);
        }

        /// <summary>Returns XP required to reach next tier.</summary>
        public static int XpToNextTier(int totalXp)
        {
            int tier = CalculateTier(totalXp);
            if (tier >= MaxTier) return 0;
            int nextThreshold = tier * XpPerTier;
            return Math.Max(0, nextThreshold - Math.Max(0, totalXp));
        }
    }

    /// <summary>
    /// Team 8 Idea 10: Data Migration Tool.
    /// Migrates legacy profile key names to current schema.
    /// </summary>
    public static class DataMigrationTool
    {
        /// <summary>
        /// Attempts to migrate legacy profile files in-place.
        /// Returns true if migration was applied.
        /// </summary>
        public static bool MigrateLegacyProfileIfNeeded()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "player-profile.cfg");
            if (!File.Exists(path)) return false;

            string text = File.ReadAllText(path, Encoding.UTF8);
            if (!text.Contains("xp=") && !text.Contains("name=")) return false;

            string migrated = text.Replace("name=", "displayName=")
                                  .Replace("xp=", "totalXp=")
                                  .Replace("cosmetics=", "cosmeticCount=")
                                  .Replace("tier=", "seasonTier=");

            File.WriteAllText(path, migrated, Encoding.UTF8);
            DebugLogger.LogInfo("DataMigrationTool", "Legacy player-profile.cfg migrated to latest schema.");
            return true;
        }
    }

    /// <summary>
    /// Team 8 Idea 2: Workshop Integration.
    /// Provides a local workshop catalog and simple install simulation.
    /// </summary>
    public static class WorkshopIntegration
    {
        private static readonly string CatalogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Data", "workshop-catalog.csv");
        private static readonly string ModsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods");

        /// <summary>Workshop catalog row.</summary>
        public sealed class WorkshopItem
        {
            /// <summary>Unique item id.</summary>
            public string Id { get; set; }
            /// <summary>Display title.</summary>
            public string Title { get; set; }
            /// <summary>Author name.</summary>
            public string Author { get; set; }
            /// <summary>Tag group.</summary>
            public string Tags { get; set; }
        }

        /// <summary>Loads all catalog items from local csv.</summary>
        public static IReadOnlyList<WorkshopItem> GetCatalog()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
            if (!File.Exists(CatalogPath)) WriteDefaultCatalog();

            var list = new List<WorkshopItem>();
            foreach (string raw in File.ReadAllLines(CatalogPath, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("#")) continue;
                string[] p = raw.Split('|');
                if (p.Length < 4) continue;
                list.Add(new WorkshopItem { Id = p[0].Trim(), Title = p[1].Trim(), Author = p[2].Trim(), Tags = p[3].Trim() });
            }
            return list;
        }

        /// <summary>Installs a workshop item as a local mod descriptor.</summary>
        public static bool Install(string itemId)
        {
            var item = GetCatalog().FirstOrDefault(x => x.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));
            if (item == null) return false;

            Directory.CreateDirectory(ModsDir);
            string modInfoPath = Path.Combine(ModsDir, item.Id + ".modinfo");
            var sb = new StringBuilder();
            sb.AppendLine($"id={item.Id}");
            sb.AppendLine($"name={item.Title}");
            sb.AppendLine("version=1.0.0");
            sb.AppendLine($"author={item.Author}");
            sb.AppendLine($"tags={item.Tags}");
            File.WriteAllText(modInfoPath, sb.ToString(), Encoding.UTF8);
            DebugLogger.LogInfo("WorkshopIntegration", $"Installed workshop item '{item.Id}'.");
            return true;
        }

        private static void WriteDefaultCatalog()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# id|title|author|tags");
            sb.AppendLine("cyber-ui-pack|Cyber UI Pack|Community Team|ui,theme");
            sb.AppendLine("classic-sfx-lite|Classic SFX Lite|Audio Guild|audio,sfx");
            sb.AppendLine("storm-boss-remix|Storm Boss Remix|Mod Lab|boss,combat");
            File.WriteAllText(CatalogPath, sb.ToString(), Encoding.UTF8);
        }
    }

    /// <summary>
    /// Team 8 Idea 3: Achievement Unlock Logger.
    /// Subscribes to achievement events and logs them to CSV for analytics.
    /// </summary>
    public static class AchievementUnlockLogger
    {
        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "phase3-achievement-unlocks.csv");
        private static bool _subscribed;

        /// <summary>Ensures one-time event subscription.</summary>
        public static void EnsureSubscribed()
        {
            if (_subscribed) return;
            EventBus.Subscribe<AchievementEarnedEvent>(OnEarned);
            _subscribed = true;
        }

        private static void OnEarned(AchievementEarnedEvent evt)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
            string name = evt.Achievement?.Name ?? "Unknown";
            string id = evt.Achievement?.Id ?? "unknown";
            File.AppendAllText(LogPath, $"{DateTime.UtcNow:o},{id},{name}{Environment.NewLine}", Encoding.UTF8);
        }
    }

    /// <summary>
    /// Team 8 Idea 4: Server Communication Library.
    /// Provides a deterministic local request/response transport mock.
    /// </summary>
    public static class ServerCommunicationLibrary
    {
        /// <summary>Response model for mock server requests.</summary>
        public sealed class Response
        {
            /// <summary>Status code (200 success).</summary>
            public int StatusCode { get; set; }
            /// <summary>Response payload text.</summary>
            public string Payload { get; set; }
            /// <summary>Roundtrip latency estimate.</summary>
            public int LatencyMs { get; set; }
        }

        /// <summary>Sends a local mock request and returns a response.</summary>
        public static Response Send(string route, string payload)
        {
            var node = ServerArchitecture.SelectBest("us-east");
            int latency = Math.Max(12, node?.LatencyMs ?? 40);
            return new Response
            {
                StatusCode = 200,
                LatencyMs = latency,
                Payload = $"ok:{route}:{payload}"
            };
        }
    }

    /// <summary>
    /// Team 8 Idea 5: Language Pack Manager.
    /// Loads simple key/value language packs from Assets/Localization.
    /// </summary>
    public static class LanguagePackManager
    {
        private static readonly string LocDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Localization");
        private static readonly Dictionary<string, Dictionary<string, string>> _packs =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Current active language code (e.g. en-US).</summary>
        public static string CurrentLanguage { get; private set; } = "en-US";

        /// <summary>Loads language packs from disk if not already loaded.</summary>
        public static void EnsureLoaded()
        {
            if (_packs.Count > 0) return;
            Directory.CreateDirectory(LocDir);
            EnsureDefaultPack();

            foreach (string file in Directory.GetFiles(LocDir, "*.lang", SearchOption.TopDirectoryOnly))
            {
                string code = Path.GetFileNameWithoutExtension(file);
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string raw in File.ReadAllLines(file, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    map[line.Substring(0, idx).Trim()] = line.Substring(idx + 1).Trim();
                }
                _packs[code] = map;
            }
        }

        /// <summary>Returns all installed language codes.</summary>
        public static IReadOnlyList<string> GetAvailableLanguages()
        {
            EnsureLoaded();
            return _packs.Keys.OrderBy(x => x).ToList();
        }

        /// <summary>Sets active language when available.</summary>
        public static bool SetLanguage(string code)
        {
            EnsureLoaded();
            if (!_packs.ContainsKey(code)) return false;
            CurrentLanguage = code;
            return true;
        }

        /// <summary>Translates a key for current language with fallback.</summary>
        public static string T(string key, string fallback)
        {
            EnsureLoaded();
            if (_packs.TryGetValue(CurrentLanguage, out var map) && map.TryGetValue(key, out var value))
                return value;
            if (_packs.TryGetValue("en-US", out var en) && en.TryGetValue(key, out var enValue))
                return enValue;
            return fallback;
        }

        private static void EnsureDefaultPack()
        {
            string path = Path.Combine(LocDir, "en-US.lang");
            if (File.Exists(path)) return;
            var sb = new StringBuilder();
            sb.AppendLine("# key=value");
            sb.AppendLine("ui.customization=Character Customization");
            sb.AppendLine("ui.shop=Cosmetics Shop");
            sb.AppendLine("ui.streaming=Streaming Mode");
            sb.AppendLine("ui.leaderboard=Leaderboard Display");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
    }

    /// <summary>
    /// Team 8 Idea 6: Cosmetic Inventory.
    /// Persists owned/equipped cosmetics for profile and shop systems.
    /// </summary>
    public static class CosmeticInventorySystem
    {
        private static readonly string PathCfg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "cosmetics-inventory.cfg");

        /// <summary>Returns owned cosmetic IDs.</summary>
        public static HashSet<string> GetOwned()
        {
            var owned = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "default_skin" };
            if (!File.Exists(PathCfg)) return owned;
            foreach (string raw in File.ReadAllLines(PathCfg, Encoding.UTF8))
            {
                if (!raw.StartsWith("own=", StringComparison.OrdinalIgnoreCase)) continue;
                string id = raw.Substring(4).Trim();
                if (!string.IsNullOrWhiteSpace(id)) owned.Add(id);
            }
            return owned;
        }

        /// <summary>Adds an owned cosmetic ID and persists.</summary>
        public static void AddOwned(string id)
        {
            var owned = GetOwned();
            owned.Add(id);
            Save(owned, GetEquipped());
        }

        /// <summary>Returns currently equipped cosmetic ID.</summary>
        public static string GetEquipped()
        {
            if (!File.Exists(PathCfg)) return "default_skin";
            foreach (string raw in File.ReadAllLines(PathCfg, Encoding.UTF8))
                if (raw.StartsWith("equip=", StringComparison.OrdinalIgnoreCase))
                    return raw.Substring(6).Trim();
            return "default_skin";
        }

        /// <summary>Sets equipped cosmetic when owned.</summary>
        public static bool Equip(string id)
        {
            var owned = GetOwned();
            if (!owned.Contains(id)) return false;
            Save(owned, id);
            return true;
        }

        private static void Save(HashSet<string> owned, string equipped)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathCfg));
            var sb = new StringBuilder();
            foreach (string id in owned.OrderBy(x => x)) sb.AppendLine("own=" + id);
            sb.AppendLine("equip=" + equipped);
            File.WriteAllText(PathCfg, sb.ToString(), Encoding.UTF8);
        }
    }

    /// <summary>
    /// Team 9 Idea 9: Custom Game Setup state.
    /// Stores custom-session gameplay toggles used by UI setup screens.
    /// </summary>
    public static class CustomGameSetupState
    {
        /// <summary>Selected starting lives for custom run.</summary>
        public static int StartingLives { get; set; } = 3;
        /// <summary>Whether enemies are scaled harder than normal.</summary>
        public static bool HardEnemies { get; set; }
        /// <summary>Whether tutorials are suppressed for this run.</summary>
        public static bool SkipTutorials { get; set; }
    }

    /// <summary>
    /// Team 9 Idea 10: Streaming Mode Toggle.
    /// Controls whether sensitive HUD/profile details should be hidden.
    /// </summary>
    public static class StreamingModeSettings
    {
        /// <summary>True when streaming-safe mode is active.</summary>
        public static bool Enabled { get; private set; }

        /// <summary>Toggles streaming mode and logs the state.</summary>
        public static void Toggle()
        {
            Enabled = !Enabled;
            DebugLogger.LogInfo("StreamingModeSettings", $"Enabled={Enabled}");
        }
    }
}
