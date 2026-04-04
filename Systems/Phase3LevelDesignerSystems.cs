// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 5: Level Designer
// Feature: Core Content Level Expansion Definitions
// Purpose: Defines all ten Phase 3 level concepts with deterministic preview
//          geometry for rapid prototyping and validation.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Fridays_Adventure.Systems
{
    /// <summary>Represents one Phase 3 level design definition.</summary>
    public sealed class Phase3LevelDefinition
    {
        /// <summary>Stable level id used by tools and saves.</summary>
        public string Id { get; set; }

        /// <summary>Display name used in UI.</summary>
        public string Name { get; set; }

        /// <summary>Core visual/theme concept.</summary>
        public string Theme { get; set; }

        /// <summary>Main encounter or traversal focus.</summary>
        public string HazardFocus { get; set; }

        /// <summary>Player objective hint for the stage.</summary>
        public string ObjectiveHint { get; set; }
    }

    /// <summary>
    /// Phase 3 Team 5 level definition service.
    /// </summary>
    public static class Phase3LevelDesignerSystems
    {
        private static readonly List<Phase3LevelDefinition> _levels = new List<Phase3LevelDefinition>
        {
            new Phase3LevelDefinition { Id = "dream_island",    Name = "Dream Island",     Theme = "Surreal floating fragments", HazardFocus = "Gravity-shift gaps",      ObjectiveHint = "Chase memory shards to the summit." },
            new Phase3LevelDefinition { Id = "neon_city",       Name = "Neon City Zone",   Theme = "Cyberpunk skyline",            HazardFocus = "Laser traffic lanes",     ObjectiveHint = "Sync jumps with signal phases." },
            new Phase3LevelDefinition { Id = "haunted_mansion", Name = "Haunted Mansion",  Theme = "Spectral corridors",           HazardFocus = "Vanish/reappear floors",  ObjectiveHint = "Track ghost keys to unlock exits." },
            new Phase3LevelDefinition { Id = "space_station",   Name = "Space Station",    Theme = "Orbital ring modules",         HazardFocus = "Low-gravity drift",       ObjectiveHint = "Restore three reactor nodes." },
            new Phase3LevelDefinition { Id = "factory_complex", Name = "Factory Complex",  Theme = "Industrial conveyor maze",     HazardFocus = "Crusher timing",          ObjectiveHint = "Route power to central core." },
            new Phase3LevelDefinition { Id = "carnival_chaos",  Name = "Carnival Chaos",   Theme = "Clockwork midway",             HazardFocus = "Pinball bumpers",         ObjectiveHint = "Collect tickets to open boss gate." },
            new Phase3LevelDefinition { Id = "volcano_lair",    Name = "Volcano Lair",     Theme = "Molten caverns",               HazardFocus = "Lava rise cycles",        ObjectiveHint = "Escape before final eruption." },
            new Phase3LevelDefinition { Id = "library_archive", Name = "Library Archive",  Theme = "Ancient knowledge vault",      HazardFocus = "Shifting book platforms", ObjectiveHint = "Assemble forbidden codex pages." },
            new Phase3LevelDefinition { Id = "metro_subway",    Name = "Metro Subway",     Theme = "Underground rail network",     HazardFocus = "Passing train bursts",    ObjectiveHint = "Ride line swaps to reach terminal." },
            new Phase3LevelDefinition { Id = "final_fortress",  Name = "Final Fortress",   Theme = "Multi-phase endgame citadel",  HazardFocus = "Gauntlet boss relays",    ObjectiveHint = "Break core seals and finish campaign." },
        };

        /// <summary>Returns all Team 5 phase 3 level definitions.</summary>
        /// <remarks>PHASE 3 - Team 5: Complete level list</remarks>
        public static IReadOnlyList<Phase3LevelDefinition> GetAll() => _levels;

        /// <summary>Returns a level definition by zero-based index.</summary>
        /// <remarks>PHASE 3 - Team 5: Level lookup helper</remarks>
        public static Phase3LevelDefinition GetByIndex(int index)
        {
            if (_levels.Count == 0) return null;
            int i = Math.Max(0, Math.Min(_levels.Count - 1, index));
            return _levels[i];
        }

        /// <summary>Builds deterministic platform preview rectangles for a level.</summary>
        /// <remarks>PHASE 3 - Team 5: Prototype geometry preview</remarks>
        public static IReadOnlyList<Rectangle> BuildPreviewGeometry(int levelIndex, int width, int height)
        {
            int seed = 5000 + Math.Max(0, levelIndex) * 137;
            return ProceduralLevelGenerator.Generate(seed, width, height, count: 16)
                .OrderBy(r => r.Y)
                .ToList();
        }
    }
}
