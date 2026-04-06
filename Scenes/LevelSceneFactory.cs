using System;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Scenes
{
    // ────────────────────────────────────────────
    // PHASE 2 - Team 10: Engine Programmer
    // Feature: Level Scene Factory
    // Purpose: Maps level IDs to their actual game Scene constructors so
    //          BotPlayLevelScene can load the real game scenes.
    // ────────────────────────────────────────────

    /// <summary>
    /// Creates the correct gameplay <see cref="Scene"/> for a given level ID.
    /// Mirrors the level routing used in <c>OverworldScene</c>.
    /// </summary>
    public static class LevelSceneFactory
    {
        /// <summary>
        /// Returns the real game scene for <paramref name="levelId"/>.
        /// Also sets <c>Game.Instance.Save.CurrentNodeId</c> so level scenes
        /// that read that value (e.g. UnderwaterScene for background selection)
        /// receive the correct ID.
        /// </summary>
        public static Scene Create(string levelId, string levelName)
        {
            // Let scenes that read CurrentNodeId know which level is loading
            if (Game.Instance?.Save != null)
                Game.Instance.Save.CurrentNodeId = levelId;

            switch (levelId)
            {
                // Storm-deck survival levels
                case "storm1":
                case "storm2":
                    return new StormScene();

                // Sky island — vertical platformer
                case "sky":
                    return new SkyIslandScene();

                // Underwater levels
                case "coral":
                case "dive_gate":
                case "sunken_gate":
                case "kelp":
                case "boiling_vent":
                case "abyss":
                    return new UnderwaterScene();

                // Marine Blockade boss (Garp)
                case "blockade":
                    return new BossScene();

                // Warlord boss fights
                case "warlord1":
                    return new WarlordBossScene(WarlordConfig.FireLordSudo());
                case "warlord2":
                    return new WarlordBossScene(WarlordConfig.StormLordVanta());

                // Final centipede boss — uses WarlordBossScene with centipede body segments
                case "centipede_final":
                    return new WarlordBossScene(WarlordConfig.CentipedeOfTheDeep());

                // All remaining island levels (dino, wano, harbor, tundra)
                default:
                    return new IslandScene(levelId, levelName);
            }
        }
    }
}
