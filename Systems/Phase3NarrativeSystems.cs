// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 6: Narrative Designer
// Feature: Narrative Expansion Systems Pack
// Purpose: Implements narrative progression structures for origins, rival arc,
//          multiverse ending, romance, lore, mentor, prophecy, sequel hook,
//          death consequences, and timeline split tracking.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Team 6 Feature 1: Character Origins repository.
    /// </summary>
    public static class CharacterOriginsSystem
    {
        private static readonly Dictionary<string, string> _origins = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Miss Friday"] = "Raised by navigators, sworn to chart the storm edge.",
            ["Orca"] = "Former harbor guardian who mastered tide-forged combat.",
            ["Swan"] = "Sky courier from cloud temples with gliding battle arts.",
        };

        /// <summary>Gets an origin synopsis for a character.</summary>
        /// <remarks>PHASE 3 - Team 6: Character Origins</remarks>
        public static string GetOrigin(string characterName)
        {
            if (string.IsNullOrWhiteSpace(characterName)) return "Unknown origin.";
            return _origins.TryGetValue(characterName, out string text) ? text : "Unknown origin.";
        }
    }

    /// <summary>
    /// Team 6 Feature 2: Secret Rival Arc tracker.
    /// </summary>
    public static class SecretRivalArcSystem
    {
        private static int _chapter;

        /// <summary>Advances rival arc chapter and returns new chapter index.</summary>
        /// <remarks>PHASE 3 - Team 6: Secret Rival Arc</remarks>
        public static int AdvanceChapter() => ++_chapter;

        /// <summary>Returns current rival arc chapter.</summary>
        /// <remarks>PHASE 3 - Team 6: Secret Rival Arc</remarks>
        public static int CurrentChapter => _chapter;
    }

    /// <summary>
    /// Team 6 Feature 3: Multiverse ending resolver.
    /// </summary>
    public static class MultiverseEndingSystem
    {
        /// <summary>Resolves ending key from completion profile.</summary>
        /// <remarks>PHASE 3 - Team 6: Multiverse Ending</remarks>
        public static string ResolveEnding(bool allBosses, bool allCollectibles, bool noDeaths)
        {
            if (allBosses && allCollectibles && noDeaths) return "ending_true_multiverse";
            if (allBosses && allCollectibles) return "ending_harmony";
            if (allBosses) return "ending_victory";
            return "ending_survivor";
        }
    }

    /// <summary>
    /// Team 6 Feature 4: Character romance subplot affinity.
    /// </summary>
    public static class CharacterRomanceSubplotSystem
    {
        private static readonly Dictionary<string, int> _affinity = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Adds affinity points for a character path.</summary>
        /// <remarks>PHASE 3 - Team 6: Character Romance Subplot</remarks>
        public static void AddAffinity(string characterName, int delta)
        {
            if (string.IsNullOrWhiteSpace(characterName)) return;
            if (!_affinity.ContainsKey(characterName)) _affinity[characterName] = 0;
            _affinity[characterName] += delta;
        }

        /// <summary>Gets affinity score for a character path.</summary>
        /// <remarks>PHASE 3 - Team 6: Character Romance Subplot</remarks>
        public static int GetAffinity(string characterName)
        {
            return _affinity.TryGetValue(characterName ?? string.Empty, out int v) ? v : 0;
        }
    }

    /// <summary>
    /// Team 6 Feature 5: World lore expansion codex entries.
    /// </summary>
    public static class WorldLoreExpansionSystem
    {
        private static readonly List<string> _entries = new List<string>
        {
            "The Iron Current once linked every sky harbor before the fracture.",
            "Sea-stone storms are strongest near the drowned observatories.",
            "The final fortress was built around a sleeping weather engine.",
            "Old maps mention a thirteenth tide beyond the known line.",
        };

        /// <summary>Returns all lore codex entries.</summary>
        /// <remarks>PHASE 3 - Team 6: World Lore Expansion</remarks>
        public static IReadOnlyList<string> GetEntries() => _entries;
    }

    /// <summary>
    /// Team 6 Feature 6: Mentor character guidance lines.
    /// </summary>
    public static class MentorCharacterSystem
    {
        /// <summary>Returns a mentor guidance line for a stage id.</summary>
        /// <remarks>PHASE 3 - Team 6: Mentor Character</remarks>
        public static string GetGuidance(string stageId)
        {
            switch ((stageId ?? string.Empty).ToLowerInvariant())
            {
                case "volcano_lair": return "Watch the rhythm of the magma vents before committing.";
                case "neon_city": return "Signal towers pulse in sets of three—move on the third beat.";
                case "final_fortress": return "Patience wins here; force opens with timing, not fury.";
                default: return "Trust your route, then trust your instincts.";
            }
        }
    }

    /// <summary>
    /// Team 6 Feature 7: Ancient prophecy fragments.
    /// </summary>
    public static class AncientProphecySystem
    {
        private static readonly string[] _fragments =
        {
            "When the ninth tide breaks, the sky will remember the sea.",
            "A blade of dawn will cut the fortress shadow in two.",
            "The mapless path opens only to those who return what they took.",
        };

        /// <summary>Returns all prophecy fragments.</summary>
        /// <remarks>PHASE 3 - Team 6: Ancient Prophecy</remarks>
        public static IReadOnlyList<string> GetFragments() => _fragments;
    }

    /// <summary>
    /// Team 6 Feature 8: Post-credit sequel hook generator.
    /// </summary>
    public static class PostCreditSequelHookSystem
    {
        /// <summary>Returns sequel hook line for credits stinger.</summary>
        /// <remarks>PHASE 3 - Team 6: Post-Credit Sequel Hook</remarks>
        public static string GetHook()
        {
            return "In the final static, a new signal appears beyond World Tour coordinates.";
        }
    }

    /// <summary>
    /// Team 6 Feature 9: Character death consequences tracker.
    /// </summary>
    public static class CharacterDeathConsequencesSystem
    {
        private static int _deaths;

        /// <summary>Registers a narrative-impacting death event.</summary>
        /// <remarks>PHASE 3 - Team 6: Character Death Consequences</remarks>
        public static void RegisterDeath() => _deaths++;

        /// <summary>Returns narrative consequence tag from death count.</summary>
        /// <remarks>PHASE 3 - Team 6: Character Death Consequences</remarks>
        public static string GetConsequenceTag()
        {
            if (_deaths >= 10) return "fragile_timeline";
            if (_deaths >= 4) return "strained_timeline";
            return "stable_timeline";
        }
    }

    /// <summary>
    /// Team 6 Feature 10: Timeline split resolver.
    /// </summary>
    public static class TimelineSplitSystem
    {
        /// <summary>Resolves active timeline branch key.</summary>
        /// <remarks>PHASE 3 - Team 6: Timeline Split</remarks>
        public static string ResolveBranch(bool rivalArcComplete, bool prophecyFound)
        {
            if (rivalArcComplete && prophecyFound) return "branch_ascendant";
            if (rivalArcComplete) return "branch_rival";
            if (prophecyFound) return "branch_prophecy";
            return "branch_mainline";
        }
    }
}
