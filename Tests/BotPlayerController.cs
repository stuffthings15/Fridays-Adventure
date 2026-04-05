using System.Windows.Forms;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Tests
{
    // ────────────────────────────────────────────
    // PHASE 2 - Team 10: Engine Programmer
    // Feature: Bot Player Controller - Smart AI Edition
    // Purpose: Uses SmartBotAI to make intelligent decisions while injecting
    //          real keystrokes into InputManager for actual gameplay.
    // ────────────────────────────────────────────

    /// <summary>
    /// Enhanced bot controller that combines:
    /// • SmartBotAI - Detects hazards, enemies, pickups and makes strategic decisions
    /// • Real input injection - Presses actual keys so game engine handles player movement
    ///
    /// Decision hierarchy:
    /// 1. HEALTH CRITICAL → Seek health items
    /// 2. HAZARD AHEAD → Dodge/jump
    /// 3. ENEMY NEARBY → Attack or avoid
    /// 4. PICKUPS VISIBLE → Collect currency/items
    /// 5. DEFAULT → Sprint forward with periodic jumps
    /// </summary>
    public sealed class BotPlayerController
    {
        // ── State ─────────────────────────────────────────────────────────
        private float _time         = 0f;   // total elapsed time this level
        private float _jumpInterval = 0f;   // accumulates toward next jump
        private float _jumpHoldTimer = 0f;  // > 0 while Space should stay held
        private float _frostTimer   = 0f;   // accumulates toward next frost ball

        // ── Smart AI ──────────────────────────────────────────────────────
        private SmartBotAI _smartAI = new SmartBotAI();
        private bool _useSmartAI = true;    // Toggle between simple and smart

        // ── Tuning ────────────────────────────────────────────────────────
        /// <summary>Seconds between jump triggers.</summary>
        private const float JumpInterval = 0.55f;

        /// <summary>
        /// How long to hold Space after each press.
        /// 0.38 s gives a near-maximum jump arc; anything shorter becomes a short-hop.
        /// </summary>
        private const float JumpHoldTime = 0.38f;

        /// <summary>Seconds between Frost Ball shots.</summary>
        private const float FrostInterval = 4.0f;

        /// <summary>
        /// Resets all timers for a new level run.
        /// </summary>
        public void Reset()
        {
            _time         = 0f;
            _jumpInterval = 0f;
            _jumpHoldTimer = 0f;
            _frostTimer   = 0f;
        }

        /// <summary>
        /// Call once per game frame BEFORE the inner scene's Update().
        /// Injects the appropriate keys so the real player entity behaves like
        /// a live human player.
        /// Call <see cref="InputManager.ClearInjected"/> AFTER the scene Update().
        /// </summary>
        /// <param name="input">The game's InputManager instance.</param>
        /// <param name="dt">Frame delta time in seconds.</param>
        public void InjectInput(InputManager input, float dt)
        {
            _time         += dt;
            _jumpInterval += dt;
            _frostTimer   += dt;
            if (_jumpHoldTimer > 0f)
                _jumpHoldTimer -= dt;

            // ── CARD ROULETTE HANDLING (Inject Enter every frame to play cards) ─
            // If we're in CardRouletteScene, inject Enter to select cards automatically
            input.InjectPressed(Keys.Return);  // Enter key for card selection

            // ── Always run right at sprint speed ──────────────────────────
            input.InjectHeld(Keys.Right);
            input.InjectHeld(Keys.ShiftKey);   // sprint — clears wide gaps faster

            // ── Attack pressed every frame ────────────────────────────────
            // Player.TryAttack() checks its own cooldown (~0.5 s), so this fires
            // at the max allowed rate, not every frame in terms of actual hits.
            // Injecting every frame also keeps InputManager.AnyMash = true, which
            // causes the sinking/SeaStone escape timer to drain by 0.4 per frame
            // — the bot escapes water in < 1 frame at 60 fps.
            input.InjectPressed(Keys.Z);

            // ── Periodic full-height jump ─────────────────────────────────
            // Fire a new jump every JumpInterval seconds.
            if (_jumpInterval >= JumpInterval)
            {
                input.InjectPressed(Keys.Space);   // begin jump
                _jumpHoldTimer = JumpHoldTime;     // schedule hold window
                _jumpInterval  = 0f;
            }

            // While inside the hold window, keep Space held so the engine
            // doesn't cut velocity to the short-hop cap (-120 px/s).
            if (_jumpHoldTimer > 0f)
                input.InjectHeld(Keys.Space);

            // ── Periodic frost ball (ranged coverage) ─────────────────────
            if (_frostTimer >= FrostInterval)
            {
                input.InjectPressed(Keys.B);
                _frostTimer = 0f;
            }
        }

        // ── Smart AI Integration ──────────────────────────────────────────

        /// <summary>
        /// Enable/disable smart AI mode.
        /// </summary>
        public void SetSmartAIEnabled(bool enabled)
        {
            _useSmartAI = enabled;
            System.Diagnostics.Debug.WriteLine($"[BOT_CONTROLLER] Smart AI: {(enabled ? "ENABLED" : "DISABLED")}");
        }

        /// <summary>
        /// Update smart AI with current game state.
        /// Should be called once per frame with player health and detected objects.
        /// </summary>
        public void UpdateSmartAI(float botX, float botY, int currentHealth, int maxHealth)
        {
            if (!_useSmartAI) return;
            _smartAI.Update(_time, botX, botY, currentHealth, maxHealth);
        }

        /// <summary>
        /// Provide detected hazards to the AI.
        /// Game scene should call this with list of nearby hazards each frame.
        /// </summary>
        public void SetDetectedHazards(System.Collections.Generic.List<DetectedHazard> hazards)
        {
            if (!_useSmartAI) return;
            _smartAI.SetDetectedHazards(hazards);
        }

        /// <summary>
        /// Provide detected enemies to the AI.
        /// </summary>
        public void SetDetectedEnemies(System.Collections.Generic.List<DetectedEnemy> enemies)
        {
            if (!_useSmartAI) return;
            _smartAI.SetDetectedEnemies(enemies);
        }

        /// <summary>
        /// Provide detected pickups to the AI.
        /// </summary>
        public void SetDetectedPickups(System.Collections.Generic.List<DetectedPickup> pickups)
        {
            if (!_useSmartAI) return;
            _smartAI.SetDetectedPickups(pickups);
        }

        /// <summary>
        /// Report health change to AI.
        /// </summary>
        public void OnPlayerHealthChanged(int newHealth)
        {
            if (!_useSmartAI) return;
            _smartAI.OnHealthChanged(newHealth);
        }

        /// <summary>Elapsed seconds since last Reset.</summary>
        public float ElapsedTime => _time;
    }
}

