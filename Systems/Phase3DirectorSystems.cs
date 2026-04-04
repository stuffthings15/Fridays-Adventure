// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 1: Game Director
// Feature: Director Expansion Systems Pack
// Purpose: Implement New Game+ progression, endless mode, challenge rotation,
//          cosmetic economy hooks, achievement 2.0, seasonal events, gauntlets,
//          DLC pipeline metadata, custom modifiers, and world tour planning.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Team 1 Feature 1: New Game+ Mode runtime services.
    /// </summary>
    public static class NewGamePlusMode
    {
        /// <summary>Enables New Game+ and applies baseline reward boosts.</summary>
        /// <remarks>PHASE 3 - Team 1: New Game+ Mode</remarks>
        public static void Enable()
        {
            var game = Engine.Game.Instance;
            if (game == null) return;
            game.NewGamePlus = true;
            game.PlayerBounty += 500;
            DebugLogger.LogInfo("NewGamePlusMode", "New Game+ enabled.");
        }

        /// <summary>Returns true when New Game+ is active.</summary>
        /// <remarks>PHASE 3 - Team 1: New Game+ Mode</remarks>
        public static bool IsActive() => Engine.Game.Instance?.NewGamePlus == true;
    }

    /// <summary>
    /// Team 1 Feature 2: Endless Mode state manager.
    /// </summary>
    public static class EndlessModeSystem
    {
        /// <summary>Current endless wave.</summary>
        public static int Wave { get; private set; }

        /// <summary>Current endless score.</summary>
        public static int Score { get; private set; }

        /// <summary>Starts endless mode from wave 1.</summary>
        /// <remarks>PHASE 3 - Team 1: Endless Mode</remarks>
        public static void Start()
        {
            Wave = 1;
            Score = 0;
        }

        /// <summary>Advances endless mode by one wave and awards score.</summary>
        /// <remarks>PHASE 3 - Team 1: Endless Mode</remarks>
        public static void AdvanceWave()
        {
            Wave = Math.Max(1, Wave + 1);
            Score += 250 * Wave;
        }
    }

    /// <summary>
    /// Team 1 Feature 3: Weekly challenge rotation.
    /// </summary>
    public static class ChallengeOfWeekSystem
    {
        private static readonly string[] _pool =
        {
            "No Damage Island Clear",
            "Speed Clear Under 5:00",
            "Only Ice Wall KOs",
            "No Power-Up Run",
            "Boss Rush 3-Stage Sprint",
            "Collect All Star Coins",
            "Perfect Combo Chain",
        };

        /// <summary>Returns deterministic challenge text for current ISO week.</summary>
        /// <remarks>PHASE 3 - Team 1: Challenge of the Week</remarks>
        public static string GetCurrentChallenge()
        {
            var now = DateTime.UtcNow;
            var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
            int week = cal.GetWeekOfYear(now,
                System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday);
            return _pool[week % _pool.Length];
        }
    }

    /// <summary>
    /// Team 1 Feature 4: Cosmetic shop economy helpers.
    /// </summary>
    public static class CosmeticShopEconomy
    {
        /// <summary>Default price table for cosmetic IDs.</summary>
        public static readonly Dictionary<string, int> PriceById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "default_skin", 0 },
            { "cyber-ui-pack", 500 },
            { "classic-sfx-lite", 500 },
            { "storm-boss-remix", 800 },
            { "captain-gold", 1000 },
        };

        /// <summary>Returns configured cosmetic price; fallback 500.</summary>
        /// <remarks>PHASE 3 - Team 1: Cosmetic Shop</remarks>
        public static int GetPrice(string cosmeticId)
        {
            if (string.IsNullOrWhiteSpace(cosmeticId)) return 500;
            return PriceById.TryGetValue(cosmeticId, out int p) ? p : 500;
        }
    }

    /// <summary>
    /// Team 1 Feature 5: Achievement System 2.0 progression tiers.
    /// </summary>
    public static class AchievementSystem2
    {
        /// <summary>Returns progression milestones for achievement modernization.</summary>
        /// <remarks>PHASE 3 - Team 1: Achievement System 2.0</remarks>
        public static IReadOnlyList<string> GetTierProgress()
        {
            int earned = AchievementSystem.EarnedCount();
            return new[]
            {
                $"Bronze Tier : {(earned >= 5 ? "UNLOCKED" : "LOCKED")} (5)",
                $"Silver Tier : {(earned >= 10 ? "UNLOCKED" : "LOCKED")} (10)",
                $"Gold Tier   : {(earned >= 20 ? "UNLOCKED" : "LOCKED")} (20)",
                $"Legend Tier : {(earned >= 30 ? "UNLOCKED" : "LOCKED")} (30)",
                $"Earned total: {earned}",
            };
        }
    }

    /// <summary>
    /// Team 1 Feature 6: Seasonal events provider.
    /// </summary>
    public static class SeasonalEventsSystem
    {
        /// <summary>Returns current seasonal event label and bonus rule.</summary>
        /// <remarks>PHASE 3 - Team 1: Seasonal Events</remarks>
        public static string GetCurrentSeasonEvent()
        {
            int m = DateTime.Now.Month;
            if (m >= 3 && m <= 5) return "Spring Bloom: +20% coin rewards";
            if (m >= 6 && m <= 8) return "Summer Storm: Bosses gain +10% HP, higher bounty";
            if (m >= 9 && m <= 11) return "Autumn Gauntlet: Endless mode score x1.25";
            return "Winter Blitz: Dash recharge and XP bonuses";
        }
    }

    /// <summary>
    /// Team 1 Feature 7: Boss Gauntlet Extended planner.
    /// </summary>
    public static class BossGauntletExtended
    {
        /// <summary>Returns ordered boss lineup for extended gauntlet.</summary>
        /// <remarks>PHASE 3 - Team 1: Boss Gauntlet Extended</remarks>
        public static IReadOnlyList<string> GetBossLineup()
        {
            return new[]
            {
                "Marine Blockade",
                "Lord Sudo",
                "Lord Vanta",
                "Centipede of the Deep",
                "Final Fortress Core",
            };
        }
    }

    /// <summary>
    /// Team 1 Feature 8: Story DLC pipeline metadata.
    /// </summary>
    public static class StoryDlcPipeline
    {
        private static readonly string DlcManifestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "DLC", "story-dlc-manifest.cfg");

        /// <summary>Ensures story DLC manifest exists and returns its lines.</summary>
        /// <remarks>PHASE 3 - Team 1: Story DLC Pipeline</remarks>
        public static IReadOnlyList<string> EnsureAndReadManifest()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DlcManifestPath));
            if (!File.Exists(DlcManifestPath))
            {
                var sb = new StringBuilder();
                sb.AppendLine("pack=story-pack-alpha");
                sb.AppendLine("state=prototype");
                sb.AppendLine("chapters=2");
                File.WriteAllText(DlcManifestPath, sb.ToString(), Encoding.UTF8);
            }
            return File.ReadAllLines(DlcManifestPath, Encoding.UTF8);
        }
    }

    /// <summary>
    /// Team 1 Feature 9: Custom game modifiers state.
    /// </summary>
    public static class CustomGameModifiers
    {
        /// <summary>Incoming damage multiplier for custom runs.</summary>
        public static float DamageTakenMultiplier { get; set; } = 1f;

        /// <summary>Score bonus multiplier for custom runs.</summary>
        public static float ScoreMultiplier { get; set; } = 1f;

        /// <summary>True if one-hit mode is forced.</summary>
        public static bool OneHitMode { get; set; }

        /// <summary>Resets modifiers to default safe values.</summary>
        /// <remarks>PHASE 3 - Team 1: Custom Game Modifiers</remarks>
        public static void Reset()
        {
            DamageTakenMultiplier = 1f;
            ScoreMultiplier = 1f;
            OneHitMode = false;
        }
    }

    /// <summary>
    /// Team 1 Feature 10: World Tour mode plan.
    /// </summary>
    public static class WorldTourMode
    {
        /// <summary>Returns world sequence for full-tour challenge.</summary>
        /// <remarks>PHASE 3 - Team 1: World Tour Mode</remarks>
        public static IReadOnlyList<string> GetTourSequence()
        {
            return new[]
            {
                "Dinosaur Island",
                "Sky Island",
                "Blade Nation",
                "Harbor Town",
                "Coral Reef",
                "Tundra Peak",
                "Sunken Gate",
                "Kelp Labyrinth",
                "Boiling Vent Ruins",
                "Abyss Engine",
            };
        }
    }
}
