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
        private float _attackCooldown = 0f;  // Track attack rate
        private GameDialogueHandler _dialogueHandler;
        private ComprehensiveBotActivityLogger _botActivityLogger = null;  // Activity logger

        // ── REAL AI (Replaces fake periodic timers) ──────────────────────
        public ObservableBotAI _realAI = null;  // For compatibility
        public UnifiedComprehensiveBot _comprehensiveBot = null;  // NEW: Unified bot system
        private bool _useRealAI = true;  // ALWAYS use real AI

        // ── Double-jump sequencing ────────────────────────────────────────
        // InjectHeld(Space) adds to BOTH _injectedHeld and _injectedPressed,
        // so JumpPressed fires every frame while Space is held.  This means:
        //   - First jump fires immediately on the first InjectHeld
        //   - SkyIslandScene sets _doubleJumpDelay = 0.18s after first jump
        //   - After 0.18s, _doubleJumpDelay expires → JumpPressed (from held)
        //     fires the second jump automatically
        //   - Holding Space prevents the variable-jump-height cap from
        //     cutting velocity short (cap only fires when JumpHeld=false)
        //
        // So all we need to do is:
        //   1. Press Space to start the sequence
        //   2. Hold Space for long enough to cover both jumps' rise time
        //   3. Release when the sequence is done

        /// <summary>How long to hold Space when a double-jump is requested.
        /// Must cover: first rise (~0.35s) + cooldown (0.18s) + second rise (~0.6s) = ~1.1s.
        /// Cap at 0.9s to let the player start falling for accurate landing.</summary>
        private const float DOUBLE_JUMP_HOLD = 0.85f;

        /// <summary>How long to hold Space for a single jump (full height).</summary>
        private const float SINGLE_JUMP_HOLD = 0.55f;

        /// <summary>True while a jump hold sequence is active.</summary>
        private bool _jumpHoldActive = false;

        /// <summary>Timer counting down the current jump hold duration.</summary>
        private float _jumpHoldRemaining = 0f;

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

        /// <summary>Minimum time between attack inputs (matches player attack cooldown).</summary>
        private const float AttackCooldown = 0.45f;

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
            _jumpHoldActive = false;
            _jumpHoldRemaining = 0f;
            _attackCooldown = 0f;
            _dialogueHandler?.Reset();
            _realAI = null;  // Clear AI (will reinitialize on next scene)
        }

        /// <summary>
        /// Slides the stuck-detection anchor to the player's current X and
        /// zeroes the stuck timer. Called every frame during the level intro
        /// drop so the timer never accumulates while HandleInput is blocked.
        /// </summary>
        public void ResetStuckAnchor() => _comprehensiveBot?.ResetStuckAnchor();

        /// <summary>
        /// Initialize bot for a new scene with UNIFIED COMPREHENSIVE BOT AI.
        /// Call this when level starts!
        /// </summary>
        public void InitializeForScene(Entities.Player player, Scenes.Scene scene, InputManager input, ComprehensiveBotActivityLogger logger = null)
        {
            if (player == null || scene == null)
                throw new System.ArgumentNullException("Player and scene required!");

            // Store the activity logger for comprehensive logging
            _botActivityLogger = logger;

            // Initialize UNIFIED comprehensive bot (with logger for file output)
            _comprehensiveBot = new UnifiedComprehensiveBot(player, scene, logger);
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
            if (_attackCooldown > 0f)
                _attackCooldown -= dt;

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
                bool shouldAttack = _comprehensiveBot.ShouldAttack;
                bool shouldDodge = _comprehensiveBot.ShouldDodge;
                bool moveRight = _comprehensiveBot.ShouldMoveRight;
                bool moveLeft = _comprehensiveBot.ShouldMoveLeft;

                // ── APPLY COMPREHENSIVE BOT DECISIONS ──────────────────────────

                // Move in the correct direction
                if (moveRight && !moveLeft)
                {
                    input.InjectHeld(Keys.Right);
                    input.InjectHeld(Keys.ShiftKey);  // Sprint
                }
                else if (moveLeft && !moveRight)
                {
                    input.InjectHeld(Keys.Left);
                    input.InjectHeld(Keys.ShiftKey);  // Sprint
                }

                // Jump based on ACTUAL detection (combat, platforming, etc.)
                // InjectHeld(Space) fires JumpPressed every frame (InputManager
                // adds held keys to _injectedPressed too).  This means:
                //   - First jump fires on frame 1
                //   - SkyIsland's 0.18s _doubleJumpDelay blocks frames 2-11
                //   - Frame 12: cooldown expires → JumpPressed auto-fires second jump
                //   - Holding Space the whole time prevents variable-jump-height cap
                bool shouldJump = _comprehensiveBot.ShouldJump;
                bool wantDouble = _comprehensiveBot.ShouldDoubleJump;

                if (_jumpHoldActive)
                {
                    _jumpHoldRemaining -= dt;
                    if (_jumpHoldRemaining > 0f)
                    {
                        // Keep holding Space — enables full jump height and
                        // auto-fires the double jump when cooldown expires
                        input.InjectHeld(Keys.Space);
                    }
                    else
                    {
                        // Hold expired — stop jumping
                        _jumpHoldActive = false;
                    }

                    // If the player lands mid-sequence, end the hold early
                    if (_comprehensiveBot.IsPlayerGrounded && _jumpHoldRemaining < (wantDouble ? DOUBLE_JUMP_HOLD : SINGLE_JUMP_HOLD) - 0.1f)
                    {
                        _jumpHoldActive = false;
                        _jumpHoldRemaining = 0f;
                    }
                }
                else if (shouldJump)
                {
                    // Start a new jump sequence
                    input.InjectHeld(Keys.Space);
                    _jumpHoldActive = true;
                    _jumpHoldRemaining = wantDouble ? DOUBLE_JUMP_HOLD : SINGLE_JUMP_HOLD;
                }

                // Attack based on ACTUAL enemy detection
                if (shouldAttack && _attackCooldown <= 0f)
                {
                    input.InjectPressed(Keys.Z);
                    _attackCooldown = AttackCooldown;  // Reset cooldown after attacking
                }

                // Dodge/character ability (E key) based on AI decision
                if (shouldDodge)
                {
                    input.InjectPressed(Keys.E);  // E = WingDash / TidalSlam / FlashFreeze
                }

                // Ice Wall climb (Q key) — bot places a wall to step up to elevated items.
                // Q goes through the normal UseIceWall() path with cooldown + energy checks,
                // exactly as a human would press it.
                if (_comprehensiveBot.ShouldUseIceWall)
                {
                    input.InjectPressed(Keys.Q);
                    System.Diagnostics.Debug.WriteLine("[BOT] Q pressed — placing ice wall to reach elevated item");
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

