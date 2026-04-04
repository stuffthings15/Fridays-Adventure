using System;
using System.Collections.Generic;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Systems
{
    // ─────────────────────────────────────────────────────────────────────────
    //  PowerUpInventory.cs  —  SMB3-style Power-Up & Design Systems
    //
    //  Team 4 (Lead Game Designer) — all 10 ideas implemented below:
    //
    //    Idea 1:  Item reserve box — hold one item to deploy later (SMB3 chest).
    //    Idea 2:  Suit transformation system — apply stat modifiers per suit type.
    //    Idea 3:  Time limit countdown — per-level timer with low-time warning.
    //    Idea 4:  Flagpole score bonus — time remaining converted to points.
    //    Idea 5:  Stomp-chain score multiplier — escalating bonus per chain kill.
    //    Idea 6:  Star coins — 3 collectibles per level tracked in SaveData.
    //    Idea 7:  Toad House item grants — random item inserted into reserve.
    //    Idea 8:  Boss key mechanic — key item required to open boss gate.
    //    Idea 9:  World map node states — locked/open/cleared/starred flags.
    //    Idea 10: P-Wing reserve — special item that skips overworld pathing.
    // ─────────────────────────────────────────────────────────────────────────

    // ── Idea 2: Suit type enum ────────────────────────────────────────────────

    /// <summary>
    /// SMB3-inspired suit / power-up types that can be held in the reserve box.
    /// Idea 2 (Lead Game Designer).
    /// </summary>
    public enum SuitType
    {
        None,
        Mushroom,   // Super — doubles hit points
        FireFlower, // Fire  — enables fireball attack
        Leaf,       // Tanooki / Leaf — adds glide and tail attack
        Star,       // Invincibility star — brief invincibility + speed
        PWing,      // P-Wing — skips overworld navigation
        BossKey,    // Boss Key — unlocks boss gate in world map
    }

    /// <summary>
    /// Central power-up inventory and game-design system manager.
    /// Drives the reserve item box, suit transformations, level timers,
    /// stomp chains, star coins, Toad House grants, and world-map states.
    ///
    /// All SMB3-style game design rules live here so the Lead Game Designer
    /// has a single file to tune without touching engine or scene code.
    /// </summary>
    public static class PowerUpInventory
    {
        // ── Idea 1: Reserve item box ──────────────────────────────────────────

        /// <summary>
        /// The item currently held in the reserve box (SMB3 chest slot).
        /// None = empty.  Players can carry one item at a time.
        /// Idea 1 (Lead Game Designer).
        /// </summary>
        public static SuitType ReserveItem { get; private set; } = SuitType.None;

        /// <summary>
        /// Stores <paramref name="suit"/> in the reserve box.
        /// If a different item is already held, it is swapped out (SMB3 rule).
        /// </summary>
        public static void SetReserve(SuitType suit)
        {
            ReserveItem = suit;
            DebugLogger.LogInfo("PowerUpInventory", $"Reserve set to {suit}");
        }

        /// <summary>
        /// Alias for <see cref="SetReserve"/> used by CardMiniGameScene and other
        /// award screens. Stores <paramref name="suit"/> in the reserve box.
        /// Team 4 (Lead Game Designer) — Idea 1: reserve item box.
        /// </summary>
        public static void SetReserveItem(SuitType suit) => SetReserve(suit);

        /// <summary>
        /// Deploys the reserve item onto the player and empties the box.
        /// Returns the item type that was deployed (None if box was empty).
        /// Idea 1 (Lead Game Designer).
        /// </summary>
        public static SuitType UseReserve()
        {
            var item = ReserveItem;
            ReserveItem = SuitType.None;
            if (item != SuitType.None)
            {
                ApplySuit(item);
                DebugLogger.LogInfo("PowerUpInventory", $"Reserve used: {item}");
            }
            return item;
        }

        // ── Idea 2: Suit transformation ────────────────────────────────────────

        /// <summary>
        /// Currently active suit on the player.
        /// Idea 2 (Lead Game Designer).
        /// </summary>
        public static SuitType ActiveSuit { get; private set; } = SuitType.None;

        /// <summary>Remaining seconds of Star invincibility (0 = inactive).</summary>
        public static float StarTimer { get; private set; }

        /// <summary>
        /// Applies a suit to the player: sets active state and fires upgrade events.
        /// Idea 2 (Lead Game Designer).
        /// </summary>
        public static void ApplySuit(SuitType suit)
        {
            ActiveSuit = suit;
            if (suit == SuitType.Star) StarTimer = 10f;   // 10 s of invincibility
            EventBus.Publish(new PowerUpCollectedEvent { PowerUpType = suit.ToString() });
            DebugLogger.LogInfo("PowerUpInventory", $"Suit applied: {suit}");
        }

        /// <summary>
        /// Downgrades the player one tier on damage.
        /// Star/PWing/BossKey are discarded; Leaf/Fire → Mushroom; Mushroom → None.
        /// Idea 2 (Lead Game Designer).
        /// </summary>
        public static void Downgrade()
        {
            switch (ActiveSuit)
            {
                case SuitType.FireFlower:
                case SuitType.Leaf:
                    ActiveSuit = SuitType.Mushroom;
                    break;
                default:
                    ActiveSuit = SuitType.None;
                    break;
            }
            DebugLogger.LogInfo("PowerUpInventory", $"Suit downgraded → {ActiveSuit}");
        }

        /// <summary>True while the Star invincibility is active.</summary>
        public static bool IsInvincible => StarTimer > 0f;

        // ── Idea 3: Level time limit countdown ────────────────────────────────

        /// <summary>
        /// Seconds remaining on the level timer.  0 = unlimited (no timer).
        /// Idea 3 (Lead Game Designer).
        /// </summary>
        public static float TimeRemaining { get; private set; }

        /// <summary>True when the level timer is running.</summary>
        public static bool TimerActive    { get; private set; }

        /// <summary>True once the timer drops below 100 seconds (urgency threshold).</summary>
        public static bool TimerUrgent    => TimerActive && TimeRemaining < 100f;

        /// <summary>
        /// Starts the level timer with the given number of seconds.
        /// Idea 3 (Lead Game Designer).
        /// </summary>
        public static void StartTimer(float seconds)
        {
            TimeRemaining = seconds;
            TimerActive   = true;
        }

        /// <summary>Stops the level timer (call on level clear or death).</summary>
        public static void StopTimer() => TimerActive = false;

        // ── Idea 5: Stomp-chain multiplier ────────────────────────────────────

        /// <summary>
        /// Current stomp-chain count.  Each consecutive stomp multiplies points.
        /// Resets when the player lands without stomping.
        /// Idea 5 (Lead Game Designer).
        /// </summary>
        public static int StompChain { get; private set; }

        /// <summary>
        /// Calculates the score value for a stomp at the current chain depth.
        /// Chain: 1=100, 2=200, 3=400, 4=800, 5+=1000 + free life after 8.
        /// Idea 5 (Lead Game Designer).
        /// </summary>
        public static int RecordStomp()
        {
            StompChain++;
            int score = Math.Min(100 * (1 << Math.Min(StompChain - 1, 7)), 1000);
            if (StompChain >= 8)
                Game.Instance.AddCoins(1);   // bonus life progression
            return score;
        }

        /// <summary>Resets the stomp chain (call when player lands without a stomp).</summary>
        public static void ResetStompChain() => StompChain = 0;

        // ── Idea 6: Star coins ────────────────────────────────────────────────

        /// <summary>
        /// Maximum star coins available per level (SMB3-style 3 coins per level).
        /// Idea 6 (Lead Game Designer).
        /// </summary>
        public const int StarCoinsPerLevel = 3;

        /// <summary>
        /// Marks star coin <paramref name="index"/> (0–2) as collected for the
        /// current world/level, persisting to SaveData.
        /// Idea 6 (Lead Game Designer).
        /// </summary>
        public static void CollectStarCoin(int index)
        {
            var g = Game.Instance;
            string key = $"starcoin_w{g.WorldNumber}_l{g.LevelNumber}_{index}";
            g.Save.SetInt(key, 1);
            DebugLogger.LogInfo("PowerUpInventory",
                $"Star coin {index + 1}/3 collected (W{g.WorldNumber}-{g.LevelNumber})");
        }

        /// <summary>
        /// Returns true if star coin <paramref name="index"/> has been collected
        /// for the given world and level.
        /// </summary>
        public static bool HasStarCoin(int world, int level, int index)
        {
            return Game.Instance.Save.GetInt($"starcoin_w{world}_l{level}_{index}", 0) == 1;
        }

        /// <summary>
        /// Returns the count of star coins collected for the given world/level.
        /// </summary>
        public static int GetStarCoinCount(int world, int level)
        {
            int count = 0;
            for (int i = 0; i < StarCoinsPerLevel; i++)
                if (HasStarCoin(world, level, i)) count++;
            return count;
        }

        // ── Idea 7: Toad House item grants ────────────────────────────────────

        /// <summary>
        /// Grants a random item (Mushroom, FireFlower, or Leaf) from a Toad House.
        /// Places the item directly in the reserve box.
        /// Idea 7 (Lead Game Designer).
        /// </summary>
        public static SuitType GrantToadHouseItem()
        {
            var rng = new Random();
            var items = new[] { SuitType.Mushroom, SuitType.FireFlower, SuitType.Leaf };
            var item  = items[rng.Next(items.Length)];
            SetReserve(item);
            DebugLogger.LogInfo("PowerUpInventory", $"Toad House granted: {item}");
            return item;
        }

        // ── Idea 8: Boss key mechanic ─────────────────────────────────────────

        /// <summary>
        /// True when the player is holding the Boss Key required to enter a boss gate.
        /// Idea 8 (Lead Game Designer).
        /// </summary>
        public static bool HasBossKey { get; private set; }

        /// <summary>Gives the player the boss key item.</summary>
        public static void PickUpBossKey()
        {
            HasBossKey = true;
            DebugLogger.LogInfo("PowerUpInventory", "Boss key collected.");
        }

        /// <summary>
        /// Consumes the boss key to open the boss gate.
        /// Returns false and does nothing if the player does not have the key.
        /// </summary>
        public static bool UseBossKey()
        {
            if (!HasBossKey) return false;
            HasBossKey = false;
            DebugLogger.LogInfo("PowerUpInventory", "Boss key used — gate opened.");
            return true;
        }

        // ── Idea 9: World map node states ─────────────────────────────────────

        /// <summary>
        /// World map node states stored in SaveData.
        /// State values: 0=locked, 1=open, 2=cleared, 3=starred.
        /// Idea 9 (Lead Game Designer).
        /// </summary>
        public static int GetNodeState(int world, int level)
        {
            return Game.Instance.Save.GetInt($"node_w{world}_l{level}", world == 1 && level == 1 ? 1 : 0);
        }

        /// <summary>Sets a world map node to the given state (0=locked, 1=open, 2=cleared, 3=starred).</summary>
        public static void SetNodeState(int world, int level, int state)
        {
            Game.Instance.Save.SetInt($"node_w{world}_l{level}", state);
        }

        /// <summary>
        /// Clears level W-L: marks it as cleared and unlocks the next node.
        /// Also awards star rating if all 3 star coins were collected.
        /// Idea 9 (Lead Game Designer).
        /// </summary>
        public static void ClearNode(int world, int level)
        {
            int coins  = GetStarCoinCount(world, level);
            int state  = coins == 3 ? 3 : 2;
            SetNodeState(world, level, state);
            SetNodeState(world, level + 1, Math.Max(GetNodeState(world, level + 1), 1));
            DebugLogger.LogInfo("PowerUpInventory", $"Node W{world}-{level} cleared (state={state}).");
        }

        // ── Idea 4: Flagpole score bonus ──────────────────────────────────────

        /// <summary>
        /// Converts remaining time to a score bonus using SMB3 rules:
        ///   Each remaining second = 50 points.
        /// Idea 4 (Lead Game Designer).
        /// </summary>
        public static int CalculateFlagpoleBonus()
        {
            return (int)(TimeRemaining * 50f);
        }

        // ── Idea 10: P-Wing skip ──────────────────────────────────────────────

        /// <summary>
        /// True when the player has a P-Wing in their reserve or active suit.
        /// Idea 10 (Lead Game Designer).
        /// </summary>
        public static bool HasPWing =>
            ActiveSuit == SuitType.PWing || ReserveItem == SuitType.PWing;

        /// <summary>
        /// Consumes the P-Wing to skip overworld navigation to the next level.
        /// Returns true if the skip was consumed.
        /// Idea 10 (Lead Game Designer).
        /// </summary>
        public static bool UsePWing()
        {
            if (ReserveItem == SuitType.PWing)   { ReserveItem = SuitType.None; }
            else if (ActiveSuit == SuitType.PWing) { ActiveSuit = SuitType.None; }
            else return false;
            DebugLogger.LogInfo("PowerUpInventory", "P-Wing used — overworld skip granted.");
            return true;
        }

        // ── Update (advance timers each frame) ────────────────────────────────

        /// <summary>
        /// Advances all time-sensitive subsystems.
        /// Call from the active gameplay scene's Update(float dt) method.
        /// </summary>
        public static void Update(float dt)
        {
            // Idea 3: level timer
            if (TimerActive && TimeRemaining > 0f)
            {
                TimeRemaining -= dt;
                if (TimeRemaining <= 0f)
                {
                    TimeRemaining = 0f;
                    TimerActive   = false;
                    DebugLogger.LogInfo("PowerUpInventory", "Level timer expired.");
                    EventBus.Publish(new PlayerDeathEvent());  // time-up = instant death
                }
            }

            // Idea 2: star timer countdown
            if (StarTimer > 0f)
            {
                StarTimer -= dt;
                if (StarTimer <= 0f)
                    StarTimer = 0f;
            }
        }

        // ── Draw helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Draws the reserve item box on the HUD (top-right corner).
        /// Idea 1 (Lead Game Designer).
        /// </summary>
        public static void DrawReserveBox(Graphics g, int hudX, int hudY)
        {
            // Box outline
            using (var pen = new System.Drawing.Pen(Color.White, 2))
                g.DrawRectangle(pen, hudX, hudY, 32, 32);

            if (ReserveItem == SuitType.None)
            {
                // Empty box shows question-mark
                using (var f = new Font("Courier New", 14, FontStyle.Bold))
                    g.DrawString("?", f, Brushes.Yellow, hudX + 8, hudY + 4);
            }
            else
            {
                // Draw a coloured letter abbreviation
                Color c = SuitColor(ReserveItem);
                using (var br = new SolidBrush(c))
                using (var f  = new Font("Courier New", 10, FontStyle.Bold))
                    g.DrawString(SuitAbbrev(ReserveItem), f, br, hudX + 4, hudY + 7);
            }
        }

        // ── Health item inventory (consumable pickups) ─────────────────────

        /// <summary>
        /// Number of collected health items currently in the player's inventory.
        /// These can be consumed from the inventory screen or HUD.
        /// </summary>
        public static int HealthItemCount { get; private set; }

        /// <summary>
        /// Adds one or more health items to inventory.
        /// </summary>
        public static void AddHealthItem(int amount = 1)
        {
            if (amount <= 0) return;
            HealthItemCount += amount;
            DebugLogger.LogInfo("PowerUpInventory", $"Health items +{amount} (total={HealthItemCount})");
        }

        /// <summary>
        /// Consumes one health item and heals the specified player.
        /// Returns true if an item was used.
        /// </summary>
        public static bool UseHealthItem(Entities.Player player, int healAmount = 30)
        {
            if (player == null) return false;
            if (HealthItemCount <= 0) return false;
            if (player.Health >= player.MaxHealth) return false;

            HealthItemCount--;
            player.Health = Math.Min(player.MaxHealth, player.Health + healAmount);
            DebugLogger.LogInfo("PowerUpInventory", $"Health item used (remaining={HealthItemCount})");
            return true;
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private static Color SuitColor(SuitType suit)
        {
            switch (suit)
            {
                case SuitType.Mushroom:   return Color.Red;
                case SuitType.FireFlower: return Color.OrangeRed;
                case SuitType.Leaf:       return Color.LimeGreen;
                case SuitType.Star:       return Color.Gold;
                case SuitType.PWing:      return Color.Cyan;
                case SuitType.BossKey:    return Color.Yellow;
                default:                  return Color.White;
            }
        }

        private static string SuitAbbrev(SuitType suit)
        {
            switch (suit)
            {
                case SuitType.Mushroom:   return "♥";
                case SuitType.FireFlower: return "F";
                case SuitType.Leaf:       return "L";
                case SuitType.Star:       return "★";
                case SuitType.PWing:      return "P";
                case SuitType.BossKey:    return "K";
                default:                  return "?";
            }
        }
    }
}
