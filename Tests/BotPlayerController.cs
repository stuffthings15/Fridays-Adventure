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
    /// • RealSmartBotAI - ACTUAL detection-based decisions (NOT timers!)
    /// • BotDialogueHandler - Auto-skips dialogue and narrative boxes
    /// • Real input injection - Presses actual keys so game engine handles player movement
    ///
    /// Decision hierarchy (based on ACTUAL game state):
    /// 1. DIALOGUE/NARRATIVE → Skip/progress automatically
    /// 2. STUCK → Jump and attack to escape
    /// 3. HEALTH CRITICAL → Seek health items (real detection)
    /// 4. ENEMY IN MELEE → Jump on head and attack (real target)
    /// 5. HAZARD AHEAD → Jump and dodge (real detection)
    /// 6. GAP AHEAD → Calculate jump (real geometry)
    /// 7. DISTANT ENEMY → Fire ranged attacks
    /// 8. EXPLORE → Move forward safely
    /// </summary>
    public sealed class BotPlayerController
    {
        // ── State ─────────────────────────────────────────────────────────
        private float _time         = 0f;
        private float _jumpHoldTimer = 0f;
        private GameDialogueHandler _dialogueHandler;

        // ── REAL AI (Replaces fake periodic timers) ──────────────────────
        public ObservableBotAI _realAI = null;  // For compatibility
        public UnifiedComprehensiveBot _comprehensiveBot = null;  // NEW: Unified bot system
        private bool _useRealAI = true;  // ALWAYS use real AI

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

        // ── Enemy stomp detection ────────────────────────────────────────
        private float _enemyStompCooldown = 0f;
        private const float EnemyStompMinDistance = 80f;   // Start checking when enemy < 80px away
        private const float EnemyStompMaxDistance = 200f;  // Stop checking when enemy > 200px away

        /// <summary>
        /// Resets all timers for a new level run.
        /// </summary>
        public void Reset()
        {
            _time         = 0f;
            _jumpHoldTimer = 0f;
            _dialogueHandler?.Reset();
            _realAI = null;  // Clear AI (will reinitialize on next scene)
        }

        /// <summary>
        /// Initialize bot for a new scene with UNIFIED COMPREHENSIVE BOT AI.
        /// Call this when level starts!
        /// </summary>
        public void InitializeForScene(Entities.Player player, Scenes.Scene scene, InputManager input)
        {
            if (player == null || scene == null)
                throw new System.ArgumentNullException("Player and scene required!");

            // Initialize UNIFIED comprehensive bot (replaces all previous systems)
            _comprehensiveBot = new UnifiedComprehensiveBot(player, scene, null);
            _useRealAI = true;

            // Keep ObservableBotAI for compatibility but don't use it
            _realAI = null;

            // Initialize dialogue handler
            if (_dialogueHandler == null)
            {
                _dialogueHandler = new GameDialogueHandler(input);
            }
            _dialogueHandler.SetCurrentScene(scene);

            System.Diagnostics.Debug.WriteLine("[BOT] ═══════════════════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine("[BOT] ✅ UNIFIED COMPREHENSIVE BOT INITIALIZED");
            System.Diagnostics.Debug.WriteLine("[BOT] Scene: " + scene.GetType().Name);
            System.Diagnostics.Debug.WriteLine("[BOT] Player: X=" + player.X + " Y=" + player.Y);
            System.Diagnostics.Debug.WriteLine("[BOT] Systems: Combat, Platforming, Item Collection, Health Mgmt");
            System.Diagnostics.Debug.WriteLine("[BOT] All frame updates will be logged below:");
            System.Diagnostics.Debug.WriteLine("[BOT] ═══════════════════════════════════════════════════════════");
        }

        // ── CardRoulette timing ──────────────────────────────────────────
        private float _cardRouletteInputTimer = 0f;
        private const float CardRouletteInputInterval = 0.5f;  // Press input every 0.5s for card selection

        /// <summary>
        /// Call once per game frame BEFORE the inner scene's Update().
        /// Injects the appropriate keys so the real player entity behaves like
        /// a live human player.
        /// Call <see cref="InputManager.ClearInjected"/> AFTER the scene Update().
        /// </summary>
        /// <param name="input">The game's InputManager instance.</param>
        /// <param name="dt">Frame delta time in seconds.</param>
        public void InitializeDialogueHandler(InputManager input, Scenes.Scene currentScene = null)
        {
            _dialogueHandler = new GameDialogueHandler(input);
            if (currentScene != null)
            {
                _dialogueHandler.SetCurrentScene(currentScene);
            }
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
            _time += dt;
            if (_jumpHoldTimer > 0f)
                _jumpHoldTimer -= dt;

            // ════════════════════════════════════════════════════════════════════
            // PRIORITY 0: HANDLE DIALOGUE - Always check first!
            // ════════════════════════════════════════════════════════════════════
            if (_dialogueHandler != null && _dialogueHandler.Update(dt))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[BOT_INPUT] Dialogue detected - {_dialogueHandler.GetSummary()}");
                return;  // Skip all input until dialogue clears
            }

            // ════════════════════════════════════════════════════════════════════
            // UNIFIED COMPREHENSIVE BOT - Intelligent decisions
            // ════════════════════════════════════════════════════════════════════
            if (_useRealAI && _comprehensiveBot != null)
            {
                // UPDATE comprehensive bot with current game state
                _comprehensiveBot.Update(dt);

                // GET DECISIONS based on ACTUAL detection and analysis
                bool shouldJump = _comprehensiveBot.ShouldJump;
                bool shouldAttack = _comprehensiveBot.ShouldAttack;
                bool shouldDodge = _comprehensiveBot.ShouldDodge;
                bool moveRight = _comprehensiveBot.ShouldMoveRight;

                // ── APPLY COMPREHENSIVE BOT DECISIONS ──────────────────────────

                // Move right based on state
                if (moveRight)
                {
                    input.InjectHeld(Keys.Right);
                    input.InjectHeld(Keys.ShiftKey);  // Sprint
                }

                // Jump based on ACTUAL detection (combat, platforming, etc.)
                if (shouldJump)
                {
                    input.InjectPressed(Keys.Space);
                    _jumpHoldTimer = 0.35f;  // Hold for jump arc
                }

                // Hold space during jump
                if (_jumpHoldTimer > 0f)
                {
                    input.InjectHeld(Keys.Space);
                }

                // Attack based on ACTUAL enemy detection
                if (shouldAttack)
                {
                    input.InjectPressed(Keys.Z);
                }

                // Dodge based on hazard detection
                if (shouldDodge)
                {
                    input.InjectPressed(Keys.X);  // Dodge key
                }

                return;  // ← Always use comprehensive bot
            }

            // If bot not initialized, do nothing (don't fake it!)
            System.Diagnostics.Debug.WriteLine("[BOT_ERROR] Unified Comprehensive Bot not initialized!");
        }

        /// <summary>Elapsed seconds since last Reset.</summary>
        public float ElapsedTime => _time;
    }
}

