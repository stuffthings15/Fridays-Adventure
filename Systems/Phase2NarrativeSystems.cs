// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 6: Narrative Designer
// Feature: Narrative Systems Pack
// Purpose: Implements Phase 2 narrative structures for dialogue, relationships,
//          logs, flashbacks, epilogue, side quests, rivals, endings, and codex.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Branch dialogue tree resolver.
    /// </summary>
    /// <remarks>PHASE 2 - Team 6: Branch Dialogue Trees</remarks>
    public static class BranchDialogueTreesSystem
    {
        /// <summary>Returns dialogue line for node id and choice branch.</summary>
        /// <remarks>PHASE 2 - Team 6: Branch Dialogue Trees</remarks>
        public static string Resolve(string nodeId, int choice)
        {
            string n = (nodeId ?? "intro").ToLowerInvariant();
            if (n == "intro") return choice == 0 ? "We sail at dawn." : "We wait for the storm to pass.";
            if (n == "rival") return choice == 0 ? "Face me on the upper deck." : "Prove your resolve first.";
            return "The sea keeps its secrets for now.";
        }
    }

    /// <summary>
    /// Character relationship progression tracker.
    /// </summary>
    /// <remarks>PHASE 2 - Team 6: Character Relationship System</remarks>
    public static class CharacterRelationshipSystem
    {
        private static readonly Dictionary<string, int> _scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Adds relationship points to a character.</summary>
        /// <remarks>PHASE 2 - Team 6: Character Relationship System</remarks>
        public static void Add(string character, int delta)
        {
            if (string.IsNullOrWhiteSpace(character)) return;
            if (!_scores.ContainsKey(character)) _scores[character] = 0;
            _scores[character] += delta;
        }

        /// <summary>Returns current relationship score.</summary>
        /// <remarks>PHASE 2 - Team 6: Character Relationship System</remarks>
        public static int Get(string character)
        {
            return _scores.TryGetValue(character ?? string.Empty, out int v) ? v : 0;
        }
    }

    /// <summary>
    /// World building audio log catalog.
    /// </summary>
    /// <remarks>PHASE 2 - Team 6: World Building Audio Logs</remarks>
    public static class WorldBuildingAudioLogsSystem
    {
        /// <summary>Returns available audio-log entries.</summary>
        /// <remarks>PHASE 2 - Team 6: World Building Audio Logs</remarks>
        public static IReadOnlyList<string> GetEntries() => new[]
        {
            "Log A-03: The harbor lights failed before the first quake.",
            "Log B-11: Ancient tide maps show routes no ship can follow.",
            "Log C-07: The fortress core hums when storm pressure rises."
        };
    }

    /// <summary>
    /// Flashback scene registry.
    /// </summary>
    /// <remarks>PHASE 2 - Team 6: Flashback Scenes</remarks>
    public static class FlashbackScenesSystem
    {
        /// <summary>Returns flashback scene descriptors.</summary>
        /// <remarks>PHASE 2 - Team 6: Flashback Scenes</remarks>
        public static IReadOnlyList<string> GetScenes() => new[]
        {
            "Harbor Departure",
            "Broken Compass Oath",
            "Rival at Sunset"
        };
    }

    /// <summary>
    /// Post-game epilogue resolver.
    /// </summary>
    /// <remarks>PHASE 2 - Team 6: Post-Game Epilogue</remarks>
    public static class PostGameEpilogueSystem
    {
        /// <summary>Returns epilogue key based on completion profile.</summary>
        /// <remarks>PHASE 2 - Team 6: Post-Game Epilogue</remarks>
        public static string Resolve(bool allBosses, bool allRelics)
        {
            if (allBosses && allRelics) return "epilogue_legend";
            if (allBosses) return "epilogue_victory";
            return "epilogue_survivor";
        }
    }

    /// <summary>
    /// Environmental storytelling note provider.
    /// </summary>
    /// <remarks>PHASE 2 - Team 6: Environmental Storytelling</remarks>
    public static class EnvironmentalStorytellingSystem
    {
        /// <summary>Returns scene clues for level context.</summary>
        /// <remarks>PHASE 2 - Team 6: Environmental Storytelling</remarks>
        public static IReadOnlyList<string> Clues() => new[]
        {
            "Broken lanterns point toward the hidden dock route.",
            "Salt-burn marks indicate prior lightning strikes.",
            "Fresh rope cuts suggest someone escaped recently."
        };
    }

    /// <summary>
    /// NPC side-quest tracker.
    /// </summary>
    /// <remarks>PHASE 2 - Team 6: NPC Side Quests</remarks>
    public static class NpcSideQuestsSystem
    {
        private static readonly HashSet<string> _completed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Marks quest id as completed.</summary>
        /// <remarks>PHASE 2 - Team 6: NPC Side Quests</remarks>
        public static void Complete(string questId)
        {
            if (!string.IsNullOrWhiteSpace(questId)) _completed.Add(questId);
        }

        /// <summary>Returns completed quest count.</summary>
        /// <remarks>PHASE 2 - Team 6: NPC Side Quests</remarks>
        public static int CompletedCount() => _completed.Count;
    }

    /// <summary>
    /// Rival encounter progression tracker.
    /// </summary>
    /// <remarks>PHASE 2 - Team 6: Rival Encounters</remarks>
    public static class RivalEncountersSystem
    {
        /// <summary>Current rival encounter stage.</summary>
        public static int Stage { get; private set; }

        /// <summary>Advances rival encounter stage.</summary>
        /// <remarks>PHASE 2 - Team 6: Rival Encounters</remarks>
        public static void Advance() => Stage++;
    }

    /// <summary>
    /// Secret ending requirement evaluator.
    /// </summary>
    /// <remarks>PHASE 2 - Team 6: Secret Ending</remarks>
    public static class SecretEndingSystem
    {
        /// <summary>Returns true when secret ending requirements are met.</summary>
        /// <remarks>PHASE 2 - Team 6: Secret Ending</remarks>
        public static bool Unlocked(bool allRelics, int sideQuestCount, int deathCount)
            => allRelics && sideQuestCount >= 5 && deathCount <= 3;
    }

    /// <summary>
    /// Narrative codex storage.
    /// </summary>
    /// <remarks>PHASE 2 - Team 6: Codex System</remarks>
    public static class CodexSystem
    {
        private static readonly Dictionary<string, string> _entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["iron_current"] = "A vanished trade path linking pre-fracture ports.",
            ["storm_core"] = "Engine relic said to bend wind and tide.",
            ["rival_mark"] = "Symbol worn by challengers of the old fleet."
        };

        /// <summary>Returns all codex keys.</summary>
        /// <remarks>PHASE 2 - Team 6: Codex System</remarks>
        public static IReadOnlyList<string> Keys() => _entries.Keys.OrderBy(x => x).ToList();

        /// <summary>Gets codex body text by key.</summary>
        /// <remarks>PHASE 2 - Team 6: Codex System</remarks>
        public static string Get(string key) => _entries.TryGetValue(key ?? string.Empty, out string v) ? v : "Unknown entry.";
    }
}
