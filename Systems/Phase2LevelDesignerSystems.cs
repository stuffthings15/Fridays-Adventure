// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 5: Level Designer
// Feature: Core Level Concepts Pack
// Purpose: Defines all ten Phase 2 level concepts and deterministic preview
//          geometry for rapid validation in ops tooling.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Fridays_Adventure.Systems
{
    /// <summary>Represents one Phase 2 level concept definition.</summary>
    public sealed class Phase2LevelDefinition
    {
        /// <summary>Stable level id used by tools and save references.</summary>
        public string Id { get; set; }

        /// <summary>Display name used by UI scenes.</summary>
        public string Name { get; set; }

        /// <summary>Visual/environment concept statement.</summary>
        public string Theme { get; set; }

        /// <summary>Primary mechanic focus for this level.</summary>
        public string MechanicFocus { get; set; }

        /// <summary>Short objective text shown in ops preview.</summary>
        public string Objective { get; set; }
    }

    /// <summary>
    /// Provides Phase 2 Team 5 level definitions and preview geometry helpers.
    /// </summary>
    public static class Phase2LevelDesignerSystems
    {
        private static readonly List<Phase2LevelDefinition> _levels = new List<Phase2LevelDefinition>
        {
            new Phase2LevelDefinition { Id = "casino_level",          Name = "Casino Level",            Theme = "Neon arcade floor",            MechanicFocus = "Risk/reward pickups",      Objective = "Cash in chips to open vault gate." },
            new Phase2LevelDefinition { Id = "mountain_peak",         Name = "Mountain Peak Gauntlet",   Theme = "High-altitude ridge",           MechanicFocus = "Wind + precision jumps",   Objective = "Reach summit before storm closes in." },
            new Phase2LevelDefinition { Id = "mirror_dimension",      Name = "Mirror Dimension",         Theme = "Reflected geometry realm",      MechanicFocus = "Inverse controls segments",Objective = "Align mirrored runes to exit." },
            new Phase2LevelDefinition { Id = "time_limit_survival",   Name = "Time-Limit Survival",      Theme = "Collapsing arena",              MechanicFocus = "Clock pressure routing",   Objective = "Survive waves until extraction." },
            new Phase2LevelDefinition { Id = "shadow_realm",          Name = "Shadow Realm",             Theme = "Low-light haunted district",    MechanicFocus = "Visibility management",    Objective = "Relight beacon towers." },
            new Phase2LevelDefinition { Id = "crystal_cavern",        Name = "Crystal Cavern",           Theme = "Reflective mineral caves",      MechanicFocus = "Beam bounce puzzles",     Objective = "Power all crystal pylons." },
            new Phase2LevelDefinition { Id = "lava_flow_chase",       Name = "Lava Flow Chase",          Theme = "Volcanic runoff channels",      MechanicFocus = "Forward chase pacing",    Objective = "Outrun lava surge to safe dock." },
            new Phase2LevelDefinition { Id = "pinball_table",         Name = "Pinball Table Level",      Theme = "Mechanical pinball labyrinth",  MechanicFocus = "Bumper momentum control", Objective = "Trigger all lane bumpers." },
            new Phase2LevelDefinition { Id = "gallery_heist",         Name = "Gallery Heist",            Theme = "Museum security grid",          MechanicFocus = "Stealth + timing",        Objective = "Steal relic and escape unseen." },
            new Phase2LevelDefinition { Id = "dna_strand",            Name = "DNA Strand Level",         Theme = "Biotech helix towers",          MechanicFocus = "Spiral traversal",        Objective = "Stabilize helix core nodes." },
        };

        /// <summary>Returns all Team 5 Phase 2 level definitions.</summary>
        /// <remarks>PHASE 2 - Team 5: Complete level list</remarks>
        public static IReadOnlyList<Phase2LevelDefinition> GetAll() => _levels;

        /// <summary>Returns a level definition by index, clamped to valid range.</summary>
        /// <remarks>PHASE 2 - Team 5: Level lookup helper</remarks>
        public static Phase2LevelDefinition GetByIndex(int index)
        {
            if (_levels.Count == 0) return null;
            int i = Math.Max(0, Math.Min(_levels.Count - 1, index));
            return _levels[i];
        }

        /// <summary>Builds deterministic platform preview geometry for a level index.</summary>
        /// <remarks>PHASE 2 - Team 5: Prototype geometry preview</remarks>
        public static IReadOnlyList<Rectangle> BuildPreviewGeometry(int levelIndex, int width, int height)
        {
            int seed = 2000 + Math.Max(0, levelIndex) * 113;
            return ProceduralLevelGenerator.Generate(seed, width, height, count: 14)
                .OrderBy(r => r.Y)
                .ToList();
        }
    }
}
