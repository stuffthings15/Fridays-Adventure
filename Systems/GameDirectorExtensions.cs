using System;
using System.Collections.Generic;
using System.Drawing;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Systems
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  GameDirectorExtensions.cs  —  Game Director / Creative Director: 10 NEW ideas
    //
    //  Extends the core Game singleton with higher-level SMB3-style authorial
    //  systems that the Game Director controls without touching engine code.
    //
    //  Idea 1:  Warp Whistle — one-shot item that teleports to any unlocked world.
    //  Idea 2:  Hammer Bros map encounter — random blocking event on overworld.
    //  Idea 3:  Boss re-lock system — returning to overworld keeps boss locked
    //           until the player reaches its node again.
    //  Idea 4:  King Coin tracker — three golden star-coins per world.
    //  Idea 5:  World-boss fight intro cutout card (displays boss name/portrait).
    //  Idea 6:  World-clear fanfare delay — short celebration before map unlocks.
    //  Idea 7:  N-Spade mini-game hint — shown once per world after 80+ coins.
    //  Idea 8:  Bonus-room hint flag — shows "?" node hint when ≥50 coins.
    //  Idea 9:  Grand-finale unlock flag — set when all 3 worlds are cleared.
    //  Idea 10: Director debug summary — human-readable game-state snapshot.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Singleton manager for all Game Director authorial systems.
    /// Access via <see cref="GameDirector.Instance"/>.
    /// Team 1 (Game Director / Creative Director) — Ideas 1–10.
    /// </summary>
    public sealed class GameDirector
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        public static readonly GameDirector Instance = new GameDirector();

        // ── Idea 1: Warp Whistle ───────────────────────────────────────────────
        /// <summary>
        /// Number of Warp Whistles the player holds.
        /// Each use shows a world-select overlay and teleports to any unlocked world.
        /// Idea 1 (Game Director).
        /// </summary>
        public int WarpWhistleCount { get; set; }

        /// <summary>Set of world numbers the player may warp to via Warp Whistle.</summary>
        private readonly HashSet<int> _warpUnlocked = new HashSet<int> { 1 };

        /// <summary>
        /// Unlocks a world as a valid warp destination.
        /// Idea 1 (Game Director).
        /// </summary>
        public void UnlockWarpDestination(int world)
        {
            _warpUnlocked.Add(world);
            DebugLogger.LogInfo("GameDirector.UnlockWarp", $"World {world} added to warp list.");
        }

        /// <summary>
        /// Attempts to use a Warp Whistle to jump to <paramref name="world"/>.
        /// Returns false if no whistle or destination is locked.
        /// Idea 1 (Game Director).
        /// </summary>
        public bool UseWarpWhistle(int world)
        {
            if (WarpWhistleCount <= 0) return false;
            if (!_warpUnlocked.Contains(world)) return false;

            WarpWhistleCount--;
            var g = Game.Instance;
            g.WorldNumber = world;
            g.LevelNumber = 1;
            DebugLogger.LogInfo("GameDirector.UseWarpWhistle",
                $"Warped to World {world}. Whistles left: {WarpWhistleCount}");
            return true;
        }

        // ── Idea 2: Hammer Bros map encounter ─────────────────────────────────
        /// <summary>
        /// List of overworld node IDs that currently have a Hammer Bros encounter.
        /// The map draws a special icon over these nodes; clicking them starts a
        /// HammerBros mini-level instead of the normal level.
        /// Idea 2 (Game Director).
        /// </summary>
        public readonly HashSet<string> HammerBrosNodes = new HashSet<string>();

        private readonly Random _rng = new Random();

        /// <summary>
        /// Randomly places Hammer Bros on 0–2 unlocked overworld nodes.
        /// Call when entering a new world.
        /// Idea 2 (Game Director).
        /// </summary>
        public void SpawnHammerBros(IEnumerable<string> unlockedNodeIds)
        {
            HammerBrosNodes.Clear();
            var pool = new List<string>(unlockedNodeIds);
            int count = Math.Min(pool.Count, _rng.Next(0, 3));
            for (int i = 0; i < count; i++)
            {
                int idx = _rng.Next(pool.Count);
                HammerBrosNodes.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            DebugLogger.LogInfo("GameDirector.SpawnHammerBros",
                $"Placed {HammerBrosNodes.Count} Hammer Bros encounter(s).");
        }

        // ── Idea 3: Boss re-lock system ────────────────────────────────────────
        /// <summary>
        /// Set of boss-node IDs that have been cleared and should not re-trigger.
        /// Idea 3 (Game Director).
        /// </summary>
        private readonly HashSet<string> _clearedBosses = new HashSet<string>();

        /// <summary>Marks a boss node as permanently defeated.</summary>
        public void ClearBoss(string nodeId)
        {
            _clearedBosses.Add(nodeId);
            DebugLogger.LogInfo("GameDirector.ClearBoss", $"Boss {nodeId} cleared.");
        }

        /// <summary>Returns true if the boss at this node has been defeated.</summary>
        public bool IsBossCleared(string nodeId) => _clearedBosses.Contains(nodeId);

        // ── Idea 4: King Coin tracker ──────────────────────────────────────────
        /// <summary>
        /// Tracks which King Coins (star coins) have been collected, keyed by
        /// "world_level_slot", e.g. "1_2_A", "1_2_B", "1_2_C".
        /// Idea 4 (Game Director).
        /// </summary>
        private readonly HashSet<string> _kingCoins = new HashSet<string>();

        /// <summary>Records a collected King Coin. Returns true if this is a new coin.</summary>
        public bool CollectKingCoin(int world, int level, char slot)
        {
            string key = $"{world}_{level}_{char.ToUpper(slot)}";
            if (_kingCoins.Contains(key)) return false;
            _kingCoins.Add(key);
            AchievementSystem.Grant("ach_berry_100");
            DebugLogger.LogInfo("GameDirector.KingCoin", $"Collected: {key}");
            return true;
        }

        /// <summary>Returns how many King Coins have been collected in a given world.</summary>
        public int GetKingCoinCount(int world)
        {
            int n = 0;
            foreach (string k in _kingCoins)
                if (k.StartsWith($"{world}_")) n++;
            return n;
        }

        // ── Idea 5: Boss fight intro card ─────────────────────────────────────
        /// <summary>
        /// Name of the boss for the intro cutout card.
        /// Set by boss scenes; cleared after the card has displayed.
        /// Idea 5 (Game Director).
        /// </summary>
        public string PendingBossIntroName { get; set; }

        /// <summary>
        /// Color accent for the boss intro card.
        /// Idea 5 (Game Director).
        /// </summary>
        public Color PendingBossIntroColor { get; set; } = Color.OrangeRed;

        // ── Idea 6: World-clear fanfare state ──────────────────────────────────
        /// <summary>
        /// Timer for the world-clear fanfare celebration before unlocking the next map.
        /// Set to FanfareDuration when a world is completed.
        /// Idea 6 (Game Director).
        /// </summary>
        public float WorldClearFanfareTimer { get; private set; }
        private const float FanfareDuration = 3.0f;

        /// <summary>
        /// Triggers the world-clear fanfare.
        /// Idea 6 (Game Director).
        /// </summary>
        public void TriggerWorldClearFanfare()
        {
            WorldClearFanfareTimer = FanfareDuration;
            DebugLogger.LogInfo("GameDirector.Fanfare", "World-clear fanfare started.");
        }

        /// <summary>Advances the fanfare timer. Call from Game.OnTick.</summary>
        public void Tick(float dt)
        {
            if (WorldClearFanfareTimer > 0f)
                WorldClearFanfareTimer = Math.Max(0f, WorldClearFanfareTimer - dt);
        }

        /// <summary>True while the fanfare is playing.</summary>
        public bool IsFanfarePlaying => WorldClearFanfareTimer > 0f;

        // ── Idea 7: N-Spade hint flag ──────────────────────────────────────────
        /// <summary>
        /// Set to true once the game has shown the N-Spade hint this world.
        /// Reset when the world changes.
        /// Idea 7 (Game Director).
        /// </summary>
        public bool NSpadeHintShown { get; set; }

        /// <summary>
        /// Returns true if the N-Spade mini-game hint should be shown.
        /// Condition: player has 80+ coins and hint hasn't been shown this world.
        /// Idea 7 (Game Director).
        /// </summary>
        public bool ShouldShowNSpadeHint()
        {
            if (NSpadeHintShown) return false;
            return Game.Instance != null && Game.Instance.CoinCount >= 80;
        }

        // ── Idea 8: Bonus-room hint ("?" node) ────────────────────────────────
        /// <summary>
        /// True when the bonus-room hint node should be visible on the overworld.
        /// Condition: player has 50+ coins and has not yet visited the bonus room
        /// for the current world.
        /// Idea 8 (Game Director).
        /// </summary>
        private readonly HashSet<int> _bonusRoomsVisited = new HashSet<int>();

        /// <summary>Whether the bonus-room "?" icon should pulse on the map.</summary>
        public bool BonusRoomHintVisible(int world)
        {
            if (_bonusRoomsVisited.Contains(world)) return false;
            return Game.Instance != null && Game.Instance.CoinCount >= 50;
        }

        /// <summary>Records a bonus room as visited for this world.</summary>
        public void MarkBonusRoomVisited(int world) => _bonusRoomsVisited.Add(world);

        // ── Idea 9: Grand-finale unlock ────────────────────────────────────────
        /// <summary>
        /// True when all main worlds have been cleared, enabling the
        /// grand-finale Bowser/Warlord level to appear on the map.
        /// Idea 9 (Game Director).
        /// </summary>
        public bool GrandFinaleUnlocked { get; private set; }

        /// <summary>
        /// Checks world-clear state and unlocks the grand finale if all worlds done.
        /// Idea 9 (Game Director).
        /// </summary>
        public void CheckGrandFinale(int totalWorlds = 3)
        {
            if (GrandFinaleUnlocked) return;
            var g = Game.Instance;
            for (int w = 1; w <= totalWorlds; w++)
                if (!g.IsWorldCleared(w)) return;

            GrandFinaleUnlocked = true;
            DebugLogger.LogInfo("GameDirector.GrandFinale", "Grand finale unlocked!");
            EventBus.Publish(new GrandFinaleUnlockedEvent());
        }

        // ── Idea 10: Director debug summary ───────────────────────────────────
        /// <summary>
        /// Returns a human-readable snapshot of all game-director state.
        /// Displayed in the dev menu and QA report.
        /// Idea 10 (Game Director).
        /// </summary>
        public string GetDebugSummary()
        {
            var g = Game.Instance;
            return
                $"World:       {g?.WorldNumber}-{g?.LevelNumber}\n" +
                $"Lives:       {g?.CurrentLives}\n" +
                $"Coins:       {g?.CoinCount}\n" +
                $"WarpWhistle: {WarpWhistleCount}\n" +
                $"HammerBros:  [{string.Join(", ", HammerBrosNodes)}]\n" +
                $"KingCoins:   {_kingCoins.Count}\n" +
                $"GrandFinale: {GrandFinaleUnlocked}\n" +
                $"Fanfare:     {(IsFanfarePlaying ? $"{WorldClearFanfareTimer:F1}s" : "idle")}\n";
        }
    }

    // ── Event: Grand Finale unlocked ──────────────────────────────────────────
    /// <summary>
    /// Published when all worlds are cleared and the grand-finale level is revealed.
    /// Idea 9 (Game Director).
    /// </summary>
    public sealed class GrandFinaleUnlockedEvent { }
}
