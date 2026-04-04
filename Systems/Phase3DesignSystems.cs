// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 4: Lead Game Designer
// Feature: Design Expansion Systems Pack
// Purpose: Implements Phase 3 gameplay design systems (bosses, roguelike,
//          progression, balancing, puzzles, ranking, and unlock tiers).
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Mega boss catalog and stats provider.
    /// </summary>
    public static class MegaBossesSystem
    {
        /// <summary>Returns configured mega-boss names.</summary>
        /// <remarks>PHASE 3 - Team 4: Mega Bosses</remarks>
        public static IReadOnlyList<string> GetBosses() => new[]
        {
            "Abyss Leviathan",
            "Clockwork Warlord",
            "Storm Sovereign",
            "Final Fortress Core"
        };
    }

    /// <summary>
    /// Roguelike run modifier generator.
    /// </summary>
    public static class RoguelikeElementsSystem
    {
        private static readonly string[] Positive = { "+15% damage", "+1 jump", "+20% coin gain", "+faster cooldowns" };
        private static readonly string[] Negative = { "-15% health", "No medkits", "Enemies +20% speed", "One-hit hazards" };

        /// <summary>Returns deterministic run modifiers from seed.</summary>
        /// <remarks>PHASE 3 - Team 4: Roguelike Elements</remarks>
        public static IReadOnlyList<string> GenerateRunModifiers(int seed)
        {
            var rng = new Random(seed);
            return new[] { Positive[rng.Next(Positive.Length)], Negative[rng.Next(Negative.Length)] };
        }
    }

    /// <summary>
    /// Character progression (XP/Level) helpers.
    /// </summary>
    public static class CharacterProgressionSystem
    {
        /// <summary>Calculates progression level from total XP.</summary>
        /// <remarks>PHASE 3 - Team 4: Character Progression</remarks>
        public static int LevelFromXp(int xp) => Math.Max(1, 1 + Math.Max(0, xp) / 750);

        /// <summary>Returns XP needed to reach next level.</summary>
        /// <remarks>PHASE 3 - Team 4: Character Progression</remarks>
        public static int XpToNextLevel(int xp)
        {
            int lvl = LevelFromXp(xp);
            int next = lvl * 750;
            return Math.Max(0, next - Math.Max(0, xp));
        }
    }

    /// <summary>
    /// Risk/reward balancing model.
    /// </summary>
    public static class RiskRewardBalancingSystem
    {
        /// <summary>Returns reward multiplier from selected risk level [0..3].</summary>
        /// <remarks>PHASE 3 - Team 4: Risk/Reward Balancing</remarks>
        public static float RewardMultiplier(int riskLevel)
        {
            switch (Math.Max(0, Math.Min(3, riskLevel)))
            {
                case 0: return 1.0f;
                case 1: return 1.15f;
                case 2: return 1.35f;
                default: return 1.60f;
            }
        }
    }

    /// <summary>
    /// Puzzle platforming generator.
    /// </summary>
    public static class PuzzlePlatformingSystem
    {
        /// <summary>Returns lightweight puzzle node sequence.</summary>
        /// <remarks>PHASE 3 - Team 4: Puzzle Platforming</remarks>
        public static IReadOnlyList<string> BuildPuzzleRoute(int complexity)
        {
            int c = Math.Max(1, Math.Min(8, complexity));
            var list = new List<string>();
            for (int i = 1; i <= c; i++) list.Add("Node " + i + " -> switch/gate");
            return list;
        }
    }

    /// <summary>
    /// Time-attack leaderboard adapter.
    /// </summary>
    public static class TimeAttackLeaderboardsSystem
    {
        /// <summary>Records a time attack score entry (higher score better).</summary>
        /// <remarks>PHASE 3 - Team 4: Time-Attack Leaderboards</remarks>
        public static void Record(string player, TimeSpan clearTime)
        {
            int score = Math.Max(1, (int)(1_000_000 - clearTime.TotalMilliseconds));
            Phase3ProducerSystems.AddLeaderboardScore(player, score);
        }
    }

    /// <summary>
    /// Collectible hunting tracker.
    /// </summary>
    public static class CollectibleHuntingSystem
    {
        private static readonly HashSet<string> _found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Marks collectible id as found.</summary>
        /// <remarks>PHASE 3 - Team 4: Collectible Hunting</remarks>
        public static void MarkFound(string collectibleId)
        {
            if (!string.IsNullOrWhiteSpace(collectibleId)) _found.Add(collectibleId);
        }

        /// <summary>Returns total found collectible count.</summary>
        /// <remarks>PHASE 3 - Team 4: Collectible Hunting</remarks>
        public static int CountFound() => _found.Count;
    }

    /// <summary>
    /// Co-op mechanics design descriptor.
    /// </summary>
    public static class CoopMechanicsDesignSystem
    {
        /// <summary>Returns high-level co-op mechanic definitions.</summary>
        /// <remarks>PHASE 3 - Team 4: Co-op Mechanics Design</remarks>
        public static IReadOnlyList<string> GetDesignNotes() => new[]
        {
            "Revive tether when partner is downed",
            "Dual-switch doors requiring two players",
            "Shared combo meter buff",
            "Assist throw and ledge boost"
        };
    }

    /// <summary>
    /// Skill-based ranking model.
    /// </summary>
    public static class SkillBasedRankingSystem
    {
        /// <summary>Returns rank from score and deaths.</summary>
        /// <remarks>PHASE 3 - Team 4: Skill-Based Ranking</remarks>
        public static string Rank(int score, int deaths)
        {
            int s = score - deaths * 500;
            if (s >= 10000) return "S";
            if (s >= 7000) return "A";
            if (s >= 4000) return "B";
            if (s >= 2000) return "C";
            return "D";
        }
    }

    /// <summary>
    /// Unlockable difficulty tiers service.
    /// </summary>
    public static class UnlockableDifficultyTiersSystem
    {
        /// <summary>Returns unlocked tiers based on completed boss count.</summary>
        /// <remarks>PHASE 3 - Team 4: Unlockable Difficulty Tiers</remarks>
        public static IReadOnlyList<string> GetUnlockedTiers(int bossesDefeated)
        {
            var tiers = new List<string> { "Normal" };
            if (bossesDefeated >= 2) tiers.Add("Hard");
            if (bossesDefeated >= 4) tiers.Add("Expert");
            if (bossesDefeated >= 6) tiers.Add("Mythic");
            return tiers;
        }
    }
}
