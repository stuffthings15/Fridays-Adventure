using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Hazards;
using Fridays_Adventure.Scenes;

namespace Fridays_Adventure.Tests
{
    // ────────────────────────────────────────────
    // PHASE 2 - Team 10: Engine Programmer
    // Feature: Unified Comprehensive Bot — Full Rewrite
    // Purpose: Complete rewrite that correctly detects the exit flag, handles
    //          lightning evasion in StormScene, escapes Card Roulette, kills all
    //          enemies, collects items, and logs every event to a file.
    // ────────────────────────────────────────────

    /// <summary>
    /// UNIFIED COMPREHENSIVE BOT - Complete Rewrite
    /// 
    /// Priority order every frame:
    ///   1. STUCK ESCAPE  — if stuck > 1.5 s: back up + jump + dash, reset timer
    ///   2. STORM DODGE   — in StormScene, run away from nearby lightning strikes
    ///   3. COMBAT        — kill nearest alive enemy (stomp first, dash after 1 s)
    ///   4. GOAL PURSUIT  — walk to _exitFlag (Rectangle, not entity)
    ///   5. ITEMS         — collect berries / health pickups on the way
    ///   6. PLATFORMING   — move right, jump every 0.5 s to clear gaps
    /// </summary>
    public class UnifiedComprehensiveBot
    {
        // ── References ────────────────────────────────────────────────────
        private readonly Player  _player;
        private readonly Scene   _scene;
        private float _elapsedTime = 0f;
        private float _dt          = 0f;   // current frame delta, stored for use in sub-methods

        // ── Logger ────────────────────────────────────────────────────────
        private readonly ComprehensiveBotActivityLogger _activityLogger;
        private StreamWriter _eventLog;          // file-based event log
        private string       _logPath;

        // ── Scene-type flags ──────────────────────────────────────────────
        private readonly bool _isStormScene;
        private readonly bool _isIslandScene;
        private readonly bool _isSkyIslandScene;
        private readonly bool _isUnderwaterScene;
        private readonly bool _isBossScene;

        // ── Cached reflected fields (avoid per-frame allocation) ──────────
        private readonly System.Reflection.FieldInfo _enemiesField;
        private readonly System.Reflection.FieldInfo _pickupsField;
        private readonly System.Reflection.FieldInfo _berriesField;
        private readonly System.Reflection.FieldInfo _exitFlagField;   // Rectangle (_exitFlag or _exitZone)
        private readonly System.Reflection.FieldInfo _strikesField;    // StormScene lightning
        private readonly System.Reflection.FieldInfo _hazardsField;    // List<Hazard> for pit detection
        private readonly System.Reflection.FieldInfo _bossField;       // Enemy _boss for boss scenes
        private readonly System.Reflection.FieldInfo _platformsField;  // List<Rectangle> ground/platforms
        private readonly System.Reflection.FieldInfo _groundYField;    // int _groundY baseline

        // ── Working lists (reused each frame, no allocation) ──────────────
        private readonly List<Enemy>        _detectedEnemies  = new List<Enemy>();
        private readonly List<HealthPickup> _detectedPickups  = new List<HealthPickup>();
        private readonly List<Entity>       _detectedBerries  = new List<Entity>();
        private readonly List<Hazard>       _detectedHazards  = new List<Hazard>();

        // How far ahead to look for water pits (px). At sprint speed ~270px/s a
        // 300 px window gives ~1.1 s to begin the jump and clear any pit.
        private const float PIT_LOOKAHEAD = 300f;

        // ── Ground-gap scanner constants ──────────────────────────────────
        // EDGE_PROBE_AHEAD: how many pixels ahead of the player's leading
        // edge we sample for solid ground.  If no platform is found at this
        // distance at or below the player's feet, the bot considers it a
        // ledge / death pit and stops + pre-jumps.
        private const float EDGE_PROBE_AHEAD     = 48f;  // close-range ledge probe
        private const float EDGE_PROBE_FAR       = 120f; // mid-range gap scan
        private const float GROUND_SEARCH_DEPTH  = 200f; // how far below feet to search

        // ── Combat ────────────────────────────────────────────────────────
        private Enemy _lastEnemy;
        private float _combatStartTime;
        private float _lastAttackTime;
        private const float COMBAT_DASH_TIMEOUT = 1.0f;   // dash if stomp fails for 1 s
        private const float ATTACK_COOLDOWN      = 0.3f;

        // ── Stuck detection ───────────────────────────────────────────────
        private float _stuckTimer;
        private float _lastX;
        private float _stuckAnchorX;              // player X when the stuck timer last reset
        private float _stuckAnchorY;              // player Y anchor (for vertical/underwater levels)
        private float _stuckEscapeTimer;           // how long to run the escape manoeuvre
        private bool  _escapingBackward;
        private const float STUCK_THRESHOLD      = 1.5f;  // seconds before declaring stuck
        private const float STUCK_ESCAPE_WINDOW  = 1.0f;  // seconds of backward + jump escape
        // Minimum TOTAL distance the player must travel in STUCK_THRESHOLD seconds.
        // Sprint speed ≈ 270px/s ⇒ per-frame ≈ 4.5px at 60fps — per-frame delta is useless.
        // Use a cumulative threshold instead: must move at least 30px over the whole window.
        private const float STUCK_MIN_PROGRESS   = 30f;

        // ── Platforming ───────────────────────────────────────────────────
        private float _jumpTimer;                          // time since last periodic jump
        private const float JUMP_INTERVAL = 0.55f;        // jump every 0.55 s while platforming

        // ── Ice Wall climbing ─────────────────────────────────────────────
        // When a collectible is too high to reach by jumping alone, the bot
        // places an ice wall beside itself (Q key) to step up 88 px and then
        // jumps from the wall top to grab the item.
        private float _elevatedJumpTimer;                  // how long we've been trying to jump to a high item
        private bool  _iceWallPlaced;                      // true after we've pressed Q this climb attempt
        private const float ICE_WALL_JUMP_THRESHOLD = 2.0f;  // seconds of failed jumps before placing wall
        // An item is considered "elevated" when it is more than one jump height above the player.
        // JumpForce=440, Gravity=860 → max jump height ≈ 112 px; use 100 px as the threshold.
        private const float ELEVATED_ITEM_HEIGHT = 100f;

        // ── Item abandon system ───────────────────────────────────────────
        // If the bot cannot collect an item within ITEM_ABANDON_TIMEOUT seconds
        // it gives up on that item for the rest of the level. This prevents
        // infinite jumping under platform-blocked coins at level start.
        private const float ITEM_ABANDON_TIMEOUT = 15f;   // give up after 15 s per item
        private float _itemPursuitTimer = 0f;              // seconds chasing current target
        private int   _currentTargetKey = int.MinValue;   // round(X/16) key for current item
        private readonly System.Collections.Generic.HashSet<int> _abandonedItems =
            new System.Collections.Generic.HashSet<int>();

        // ── Public decision outputs ───────────────────────────────────────
        public bool   ShouldJump        { get; private set; }
        public bool   ShouldAttack      { get; private set; }
        public bool   ShouldMoveRight   { get; private set; }
        public bool   ShouldMoveLeft    { get; private set; }
        public bool   ShouldDodge       { get; private set; }
        /// <summary>
        /// True when the bot wants to place an Ice Wall (Q key) this frame.
        /// Set when an elevated item is unreachable by jumping alone and the
        /// bot has been attempting to jump to it for ICE_WALL_JUMP_THRESHOLD seconds.
        /// </summary>
        public bool   ShouldUseIceWall  { get; private set; }
        public string CurrentState      { get; private set; } = "INIT";

        // ── Compat ────────────────────────────────────────────────────────
        public bool IsStuck => _stuckTimer >= STUCK_THRESHOLD;

        /// <summary>
        /// Slides the stuck anchor to the player's current position and resets the timer.
        /// Called every frame during the level intro drop so the timer never
        /// accumulates while HandleInput is blocked and fires a spurious escape.
        /// </summary>
        public void ResetStuckAnchor()
        {
            if (_player == null) return;
            _stuckAnchorX = _player.X;
            _stuckAnchorY = _player.Y;
            _stuckTimer   = 0f;
        }

        // ══════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════════════════

        public UnifiedComprehensiveBot(Player player, Scene scene,
            ComprehensiveBotActivityLogger logger = null)
        {
            _player        = player ?? throw new ArgumentNullException(nameof(player));
            _scene         = scene  ?? throw new ArgumentNullException(nameof(scene));
            _activityLogger = logger;
            _lastX         = player.X;
            _stuckAnchorX  = player.X;   // anchor starts at spawn position
            _stuckAnchorY  = player.Y;

            // Identify scene type
            _isStormScene      = scene is StormScene;
            _isIslandScene     = scene is IslandScene;
            _isSkyIslandScene  = scene is SkyIslandScene;
            _isUnderwaterScene = scene is UnderwaterScene;
            _isBossScene       = scene is BossScene || scene is WarlordBossScene;

            // Cache reflection fields once — avoids per-frame GetField calls
            var flags = System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance;
            var t = scene.GetType();
            _enemiesField  = t.GetField("_enemies",       flags);
            _pickupsField  = t.GetField("_healthPickups", flags);
            _berriesField  = t.GetField("_berries",       flags);
            _strikesField  = t.GetField("_strikes",       flags);
            _hazardsField  = t.GetField("_hazards",       flags);
            _bossField     = t.GetField("_boss",          flags);
            _platformsField = t.GetField("_platforms",    flags);
            _groundYField   = t.GetField("_groundY",      flags);

            // Try _exitFlag first (IslandScene), then _exitZone (SkyIsland, Underwater)
            _exitFlagField = t.GetField("_exitFlag", flags)
                          ?? t.GetField("_exitZone", flags);

            // Open file-based event log
            OpenEventLog();

            LogEvent("BOT_INIT",
                $"Scene={scene.GetType().Name} IsStorm={_isStormScene} " +
                $"IsSky={_isSkyIslandScene} IsUnderwater={_isUnderwaterScene} " +
                $"IsBoss={_isBossScene} ExitField={(_exitFlagField?.Name ?? "NONE")} " +
                $"BossField={(_bossField != null ? "YES" : "NO")} " +
                $"Player X={player.X:F0} Y={player.Y:F0} HP={player.Health}");
        }

        // ══════════════════════════════════════════════════════════════════
        // MAIN UPDATE
        // ══════════════════════════════════════════════════════════════════

        public void Update(float dt)
        {
            _elapsedTime += dt;
            _dt           = dt;    // store for sub-methods that need delta time
            _jumpTimer   += dt;

            // Reset outputs
            ShouldJump        = false;
            ShouldAttack      = false;
            ShouldMoveRight   = true;
            ShouldMoveLeft    = false;
            ShouldDodge       = false;
            ShouldUseIceWall  = false;

            try
            {
                RefreshLists();
                UpdateStuckDetection(dt);

                if (_escapingBackward)
                {
                    RunStuckEscape(dt);
                    return;
                }

                if (_isStormScene)
                    RunStormLogic();
                else if (_isBossScene)
                    RunBossFightLogic();
                else if (_isSkyIslandScene)
                    RunVerticalLogic();
                else if (_isUnderwaterScene)
                    RunUnderwaterLogic();
                else
                    RunNormalLogic();

                // ── Universal pit / ledge avoidance (runs after ALL state logic) ──
                // Two-layer detection:
                //   Layer 1 (close): probe EDGE_PROBE_AHEAD px ahead for ground.
                //            If no ground → stop walking, pre-jump.
                //   Layer 2 (mid):   probe EDGE_PROBE_FAR px ahead.  If a gap
                //            exists AND a landing exists beyond it, start the jump
                //            early so the bot clears the gap in flight.
                // Also falls back to WaterPit hazard list for extra safety.
                if (!_isStormScene && _player.IsGrounded)
                {
                    bool closeGround = HasGroundAhead(EDGE_PROBE_AHEAD);
                    bool farGround   = HasGroundAhead(EDGE_PROBE_FAR);

                    if (!closeGround)
                    {
                        // Ledge right at our feet — STOP and jump vertically first.
                        // Only move forward once airborne so we don't walk off.
                        if (CanJumpOverGap(0f))
                        {
                            ShouldJump      = true;
                            ShouldMoveRight = false;
                            ShouldMoveLeft  = false;
                            LogEvent("EDGE_STOP",
                                $"No ground {EDGE_PROBE_AHEAD}px ahead — stopping + jumping");
                        }
                        else
                        {
                            // No landing exists — reverse away from the edge
                            ShouldMoveRight = false;
                            ShouldMoveLeft  = true;
                            ShouldJump      = false;
                            LogEvent("EDGE_RETREAT",
                                $"No ground ahead and no landing — retreating");
                        }
                    }
                    else if (!farGround)
                    {
                        // Ground is present close but missing farther ahead — gap
                        // is approaching.  Start jumping early to clear it in flight.
                        float gapDist = FindNearestGapDistance(EDGE_PROBE_FAR);
                        if (gapDist != float.MaxValue && CanJumpOverGap(gapDist))
                        {
                            ShouldJump = true;
                            LogEvent("GAP_PREJUMP",
                                $"Gap at {gapDist:F0}px — pre-jumping to clear");
                        }
                    }
                    else
                    {
                        // Both probes have ground — also check WaterPit hazards
                        // as a fallback (covers hazards the platform list misses).
                        float pitX = FindNearestPitAheadX();
                        if (pitX != float.MaxValue)
                        {
                            ShouldJump = true;
                            float gap = pitX - (_player.X + _player.Width);
                            if (gap < 60f)
                            {
                                ShouldMoveRight = false;
                                ShouldMoveLeft  = false;
                            }
                        }
                    }
                }
                // Airborne over a gap: allow forward movement toward the landing
                else if (!_isStormScene && !_player.IsGrounded)
                {
                    // If the bot is airborne and there IS ground ahead, move forward
                    // to reach the landing.  If there is NO ground, hold position.
                    if (!HasGroundAhead(EDGE_PROBE_FAR) && !HasGroundAhead(EDGE_PROBE_AHEAD))
                    {
                        // Still over the gap — keep moving to reach the far side
                        // (the jump was validated before takeoff).
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvent("BOT_ERROR", ex.Message);
                // Fallback: keep moving right
                ShouldMoveRight = true;
            }

            // Periodic frame log every 2 s
            if (_elapsedTime % 2f < dt + 0.016f)
                LogEvent("FRAME",
                    $"State={CurrentState} Pos=({_player.X:F0},{_player.Y:F0}) " +
                    $"HP={_player.Health:F0}/{_player.MaxHealth:F0} " +
                    $"Enemies={_detectedEnemies.Count} Stuck={IsStuck}");
        }

        // ══════════════════════════════════════════════════════════════════
        // STORM SCENE LOGIC  — survive 25 s by dodging lightning
        // ══════════════════════════════════════════════════════════════════

        private void RunStormLogic()
        {
            CurrentState = "STORM_SURVIVE";

            // Read lightning strikes from StormScene via reflection
            float dangerX = float.MaxValue;
            if (_strikesField != null)
            {
                var strikes = _strikesField.GetValue(_scene) as System.Collections.IList;
                if (strikes != null)
                {
                    foreach (var s in strikes)
                    {
                        if (s == null) continue;
                        // LightningStrike is a private nested class — read X property
                        var xProp = s.GetType().GetField("X");
                        if (xProp == null) xProp = s.GetType().GetField("_x");
                        float sx = xProp != null ? (float)xProp.GetValue(s) : float.MaxValue;
                        if (Math.Abs(sx - _player.X) < Math.Abs(dangerX - _player.X))
                            dangerX = sx;
                    }
                }
            }

            // If a strike is within 120 px, run away from it
            if (dangerX != float.MaxValue && Math.Abs(dangerX - _player.X) < 120f)
            {
                CurrentState = "STORM_DODGE";
                if (dangerX > _player.X)      // strike to the right — run left
                {
                    ShouldMoveLeft  = true;
                    ShouldMoveRight = false;
                }
                else                           // strike to the left — run right
                {
                    ShouldMoveRight = true;
                    ShouldMoveLeft  = false;
                }
                ShouldJump = true;             // also jump to dodge
                LogEvent("STORM_DODGE",
                    $"StrikeX={dangerX:F0} PlayerX={_player.X:F0} — running away");
                return;
            }

            // No nearby threat — stay near centre of the ship
            float centreX = Game.Instance.CanvasWidth * 0.5f;
            float toCentre = centreX - _player.X;
            if (Math.Abs(toCentre) > 60f)
            {
                ShouldMoveRight = toCentre > 0;
                ShouldMoveLeft  = toCentre < 0;
            }
            else
            {
                ShouldMoveRight = false;
                ShouldMoveLeft  = false;
            }

            // Collect health pickups if hurt
            if (_detectedPickups.Count > 0 && _player.Health < _player.MaxHealth)
            {
                var hp = _detectedPickups[0];
                ShouldMoveRight = hp.X > _player.X;
                ShouldMoveLeft  = hp.X < _player.X;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // BOSS FIGHT LOGIC — stay close, attack constantly, dodge telegraphs
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Dedicated boss-fight AI for BossScene / WarlordBossScene.
        /// The goal is to defeat <c>_boss</c> — there is no exit flag.
        /// Strategy: stay within attack range, stomp + melee, dash when
        /// stomp fails, and periodically jump to dodge ground attacks.
        /// </summary>
        private void RunBossFightLogic()
        {
            // Read the boss entity via reflection
            Enemy boss = GetBossEnemy();
            if (boss == null || !boss.IsAlive)
            {
                // Boss dead or not found — idle and let the scene handle victory
                CurrentState    = "BOSS_VICTORY_WAIT";
                ShouldMoveRight = false;
                ShouldMoveLeft  = false;
                return;
            }

            CurrentState = "BOSS_FIGHT";

            // Track combat duration against the boss
            if (_lastEnemy != boss)
            {
                _lastEnemy       = boss;
                _combatStartTime = _elapsedTime;
                LogEvent("BOSS_ENGAGE",
                    $"Boss={boss.GetType().Name} HP={boss.Health}/{boss.MaxHealth} " +
                    $"X={boss.X:F0}");
            }

            float combatDuration = _elapsedTime - _combatStartTime;
            float dirX = boss.X - _player.X;
            float dist = Math.Abs(dirX);

            // Move toward the boss — stay within melee range (~60-100 px)
            if (dist > 100f)
            {
                ShouldMoveRight = dirX > 0;
                ShouldMoveLeft  = dirX < 0;
            }
            else if (dist < 40f)
            {
                // Too close — back off slightly to avoid contact damage
                ShouldMoveRight = dirX < 0;
                ShouldMoveLeft  = dirX > 0;
            }

            // Always try to stomp (jump on head)
            ShouldJump = true;

            // Attack when within melee range
            if (dist < 120f && _elapsedTime - _lastAttackTime > ATTACK_COOLDOWN)
            {
                ShouldAttack    = true;
                _lastAttackTime = _elapsedTime;
            }

            // Dash through the boss every 2 seconds if still alive
            if (combatDuration >= 2.0f && boss.IsAlive)
            {
                ShouldDodge = true;
                _combatStartTime = _elapsedTime;
                LogEvent("BOSS_DASH",
                    $"Boss HP={boss.Health}/{boss.MaxHealth} — dashing through");
            }

            // Collect health pickups if hurt (within wide range)
            if (_player.Health < _player.MaxHealth * 0.5f)
                TryCollectNearbyItems(200f, suppressMovement: false);

            // Periodic log
            if (_elapsedTime % 3f < _dt + 0.016f)
                LogEvent("BOSS_STATUS",
                    $"BossHP={boss.Health}/{boss.MaxHealth} PlayerHP={_player.Health:F0} " +
                    $"Dist={dist:F0}");
        }

        // ══════════════════════════════════════════════════════════════════
        // VERTICAL LOGIC — SkyIslandScene: climb upward to exit at top
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Vertical platformer strategy for SkyIslandScene.
        /// The exit is at the top of the level.  The bot must jump upward
        /// across ascending platforms rather than simply moving right.
        /// </summary>
        private void RunVerticalLogic()
        {
            // ── P1: Kill enemies ──────────────────────────────────────
            Enemy nearest = FindNearestAliveEnemy();
            if (nearest != null && Math.Abs(nearest.X - _player.X) < 200f
                && Math.Abs(nearest.Y - _player.Y) < 120f)
            {
                RunCombatLogic(nearest);
                return;
            }

            // ── P2: Pursue exit zone if found ─────────────────────────
            Rectangle exitZone = GetExitFlag();
            if (exitZone != Rectangle.Empty)
            {
                float distX = exitZone.X + exitZone.Width / 2f - _player.X;
                float distY = exitZone.Y + exitZone.Height / 2f - _player.Y;

                CurrentState = "SKY_GOAL_PURSUIT";
                ShouldMoveRight = distX > 30f;
                ShouldMoveLeft  = distX < -30f;

                // Always jump to climb
                ShouldJump = true;

                LogEvent("SKY_GOAL", $"ExitY={exitZone.Y} DistX={distX:F0} DistY={distY:F0}");
                return;
            }

            // ── P3: Default climb strategy — move toward centre, jump constantly
            CurrentState = "SKY_CLIMBING";
            float centreX = 450f;  // SkyIslandScene LevelWidth=900, centre=450
            float toCentre = centreX - _player.X;

            // Alternate left/right every few seconds to hit different platforms
            float oscillation = (float)Math.Sin(_elapsedTime * 0.4f) * 200f;
            float targetX = centreX + oscillation;
            float toTarget = targetX - _player.X;

            ShouldMoveRight = toTarget > 30f;
            ShouldMoveLeft  = toTarget < -30f;

            // Jump as often as possible to climb
            if (_jumpTimer >= 0.35f)
            {
                ShouldJump = true;
                _jumpTimer = 0f;
            }

            // Collect items opportunistically
            TryCollectNearbyItems(100f, suppressMovement: false);
        }

        // ══════════════════════════════════════════════════════════════════
        // UNDERWATER LOGIC — buoyancy movement, seek exit zone
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Underwater navigation for UnderwaterScene.
        /// No gravity-based platforming — the player floats with buoyancy.
        /// Strategy: head toward exit zone, avoid coral hazards, collect
        /// air bubbles to refill oxygen.
        /// </summary>
        private void RunUnderwaterLogic()
        {
            // ── P1: Pursue exit zone ──────────────────────────────────
            Rectangle exitZone = GetExitFlag();
            if (exitZone != Rectangle.Empty)
            {
                float distX = exitZone.X + exitZone.Width / 2f - _player.X;
                float distY = exitZone.Y + exitZone.Height / 2f - _player.Y;

                CurrentState = "UNDERWATER_GOAL";
                ShouldMoveRight = distX > 20f;
                ShouldMoveLeft  = distX < -20f;

                // Use jump to swim upward when exit is above
                ShouldJump = distY < -20f;

                LogEvent("UNDERWATER_GOAL",
                    $"ExitX={exitZone.X} ExitY={exitZone.Y} DistX={distX:F0} DistY={distY:F0}");
                return;
            }

            // ── P2: Default exploration — swim right and periodically up
            CurrentState    = "UNDERWATER_EXPLORE";
            ShouldMoveRight = true;

            // Periodically swim up to explore vertical space
            if (_jumpTimer >= JUMP_INTERVAL)
            {
                ShouldJump = true;
                _jumpTimer = 0f;
            }

            // Collect items on the way
            TryCollectNearbyItems(200f, suppressMovement: false);
        }

        // ══════════════════════════════════════════════════════════════════
        // NORMAL LEVEL LOGIC
        // ══════════════════════════════════════════════════════════════════

        private void RunNormalLogic()
        {
            // ── P1: Kill enemies ─────────────────────────────────────────
            Enemy nearest = FindNearestAliveEnemy();
            if (nearest != null && Math.Abs(nearest.X - _player.X) < 250f)
            {
                RunCombatLogic(nearest);
                return;
            }

            // ── P2: Pursue goal flag ──────────────────────────────────────
            Rectangle exitFlag = GetExitFlag();
            if (exitFlag != Rectangle.Empty)
            {
                float dist = exitFlag.X - _player.X;
                CurrentState = "GOAL_PURSUIT";
                ShouldMoveRight = dist > 20f;
                ShouldMoveLeft  = dist < -20f;

                // Periodic jump to clear raised platforms and small gaps.
                // Only jump when ground exists ahead so we don't launch into pits.
                // (Pit/ledge detection is also handled by the universal check in Update.)
                if (_jumpTimer >= JUMP_INTERVAL && HasGroundAhead(EDGE_PROBE_FAR))
                {
                    ShouldJump  = true;
                    _jumpTimer  = 0f;
                }

                // Collect nearby items on the way (within 80 px)
                TryCollectNearbyItems(80f, suppressMovement: false);

                LogEvent("GOAL_PURSUIT", $"FlagX={exitFlag.X} Dist={dist:F0}");
                return;
            }

            // ── P3: Collect items ─────────────────────────────────────────
            if (TryCollectNearbyItems(300f, suppressMovement: true))
                return;

            // ── P4: Default platforming — move right, jump periodically ───
            CurrentState    = "PLATFORMING";
            ShouldMoveRight = true;
            // Only jump when the ground ahead is confirmed safe — prevents
            // blind periodic jumps from launching the bot into death pits.
            if (_jumpTimer >= JUMP_INTERVAL && HasGroundAhead(EDGE_PROBE_FAR))
            {
                ShouldJump  = true;
                _jumpTimer  = 0f;
            }

            // Emergency fall recovery
            if (_player.Y > Game.Instance.CanvasHeight - 50f)
            {
                ShouldJump = true;
                LogEvent("FALL_RECOVERY", $"Y={_player.Y:F0}");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // COMBAT
        // ══════════════════════════════════════════════════════════════════

        private void RunCombatLogic(Enemy target)
        {
            CurrentState = "COMBAT";

            // Track how long we've been fighting this enemy
            if (_lastEnemy != target)
            {
                _lastEnemy       = target;
                _combatStartTime = _elapsedTime;
                LogEvent("COMBAT_START",
                    $"New target {target.GetType().Name} X={target.X:F0}");
            }

            float combatDuration = _elapsedTime - _combatStartTime;
            float dirX           = target.X - _player.X;

            // ── Pit-safe movement: never charge toward an enemy across a gap ──
            // Check if ground exists between us and the enemy.  If a pit is
            // between us, disengage and let the universal pit handler take over.
            if (_player.IsGrounded && !HasGroundAhead(Math.Min(Math.Abs(dirX), EDGE_PROBE_FAR)))
            {
                // There's a gap between us and the enemy — don't pursue
                ShouldMoveRight = false;
                ShouldMoveLeft  = false;
                ShouldAttack    = true;   // ranged attack if possible
                LogEvent("COMBAT_PIT_BLOCK",
                    $"Gap between player and enemy at dist={dirX:F0} — holding position");
                return;
            }

            // Move toward enemy
            ShouldMoveRight = dirX > 20f;
            ShouldMoveLeft  = dirX < -20f;

            // Always try to stomp first (jump on head)
            ShouldJump = true;

            // Attack key when nearby
            if (Math.Abs(dirX) < 80f && _elapsedTime - _lastAttackTime > ATTACK_COOLDOWN)
            {
                ShouldAttack    = true;
                _lastAttackTime = _elapsedTime;
            }

            // After 1 s of failed stomping, trigger the character dash ability via
            // the normal E-key input path — same as a human pressing E.
            // Never call _player.TryDash() directly; that bypasses ability checks and awards
            // i-frames the human player would not get outside the normal system.
            if (combatDuration >= COMBAT_DASH_TIMEOUT && target.IsAlive)
            {
                ShouldDodge = true;   // BotPlayerController will inject Keys.E this frame
                LogEvent("COMBAT_DASH",
                    $"Enemy alive after {combatDuration:F1}s — pressing E to dash through");
                _combatStartTime = _elapsedTime;
            }

            LogEvent("COMBAT",
                $"Target={target.GetType().Name} Dist={Math.Abs(dirX):F0} " +
                $"Duration={combatDuration:F1}s");
        }

        // ══════════════════════════════════════════════════════════════════
        // STUCK DETECTION & ESCAPE
        // ══════════════════════════════════════════════════════════════════

        private void UpdateStuckDetection(float dt)
        {
            // Measure total displacement from the anchor point set when the timer last reset.
            // For horizontal levels (IslandScene) only X matters.  For vertical
            // (SkyIslandScene) and underwater levels Y progress is equally important.
            float dx = _player.X - _stuckAnchorX;
            float dy = _player.Y - _stuckAnchorY;
            float totalMoved = (_isSkyIslandScene || _isUnderwaterScene || _isBossScene)
                ? (float)Math.Sqrt(dx * dx + dy * dy)  // 2D distance
                : Math.Abs(dx);                         // horizontal only

            if (totalMoved < STUCK_MIN_PROGRESS)
            {
                // Player hasn't progressed enough since the anchor was set
                _stuckTimer += dt;
                if (_stuckTimer >= STUCK_THRESHOLD && !_escapingBackward)
                {
                    _escapingBackward = true;
                    _stuckEscapeTimer = STUCK_ESCAPE_WINDOW;
                    LogEvent("STUCK_ESCAPE",
                        $"Stuck {_stuckTimer:F1}s at X={_player.X:F0} Y={_player.Y:F0} — escaping");
                }
            }
            else
            {
                // Good progress — reset timer and slide anchor forward
                _stuckTimer   = 0f;
                _stuckAnchorX = _player.X;
                _stuckAnchorY = _player.Y;
            }
            _lastX = _player.X;
        }

        private void RunStuckEscape(float dt)
        {
            CurrentState      = "STUCK_ESCAPE";
            _stuckEscapeTimer -= dt;

            // If a ledge / water pit is very close ahead, hold position and
            // jump rather than running forward into it.
            bool noGroundClose = _player.IsGrounded && !HasGroundAhead(EDGE_PROBE_AHEAD);
            float pitX    = FindNearestPitAheadX();
            bool  pitClose = pitX != float.MaxValue
                             && (pitX - (_player.X + _player.Width)) < 60f;

            if (noGroundClose || pitClose)
            {
                // Stand and jump until airborne over the pit, then move right
                ShouldMoveRight = !_player.IsGrounded;  // move right only while in air
                ShouldMoveLeft  = false;
                ShouldJump      = true;
            }
            else
            {
                // No immediate pit — jump forward over the obstacle
                ShouldMoveRight = true;
                ShouldMoveLeft  = false;
                ShouldJump      = true;
            }

            if (_stuckEscapeTimer <= 0f)
            {
                _escapingBackward = false;
                _stuckTimer       = 0f;
                _stuckAnchorX     = _player.X;
                _stuckAnchorY     = _player.Y;
                LogEvent("STUCK_RESOLVED", $"Forward jump complete — resuming at X={_player.X:F0} Y={_player.Y:F0}");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // ITEM COLLECTION
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Moves toward the nearest uncollected item within <paramref name="range"/> px.
        /// Returns true if an item was targeted (sets movement direction).
        /// If <paramref name="suppressMovement"/> is false, only adjusts direction without
        /// overriding the caller's movement intent.
        /// </summary>
        private bool TryCollectNearbyItems(float range, bool suppressMovement)
        {
            // Track best candidate using plain X/Y floats (HealthPickup is not an Entity)
            bool   foundPickup    = false;
            float  bestPickupX    = 0f;
            float  bestPickupY    = 0f;
            float  bestPickupDist = range;

            foreach (var hp in _detectedPickups)
            {
                int key = (int)(hp.X / 16f);
                if (_abandonedItems.Contains(key)) continue;   // skip already-given-up items
                float d = Math.Abs(hp.X - _player.X);
                if (d < bestPickupDist)
                { bestPickupDist = d; bestPickupX = hp.X; bestPickupY = hp.Y; foundPickup = true; }
            }

            Entity bestBerry     = null;
            float  bestBerryDist = range;
            foreach (var b in _detectedBerries)
            {
                int key = (int)(b.X / 16f);
                if (_abandonedItems.Contains(key)) continue;   // skip abandoned coins/berries
                float d = Math.Abs(b.X - _player.X);
                if (d < bestBerryDist)
                { bestBerryDist = d; bestBerry = b; }
            }

            // Pick whichever is closer
            bool usePickup = foundPickup && bestPickupDist <= bestBerryDist;
            bool useBerry  = bestBerry != null && bestBerryDist < bestPickupDist;

            if (!usePickup && !useBerry)
            {
                // Nothing reachable — reset all item-pursuit state
                _elevatedJumpTimer  = 0f;
                _iceWallPlaced      = false;
                _itemPursuitTimer   = 0f;
                _currentTargetKey   = int.MinValue;
                return false;
            }

            float targetX   = usePickup ? bestPickupX : bestBerry.X;
            float targetY   = usePickup ? bestPickupY : bestBerry.Y;
            float dir       = targetX - _player.X;
            int   targetKey = (int)(targetX / 16f);

            // ── Per-item abandon timer ────────────────────────────────────
            // If we switch to a new target, reset the pursuit clock.
            if (targetKey != _currentTargetKey)
            {
                _currentTargetKey  = targetKey;
                _itemPursuitTimer  = 0f;
                _elevatedJumpTimer = 0f;
                _iceWallPlaced     = false;
            }
            else if (suppressMovement)
            {
                _itemPursuitTimer += _dt;
            }

            // Give up after 10 seconds — log it and blacklist the item for this level
            if (_itemPursuitTimer >= ITEM_ABANDON_TIMEOUT)
            {
                _abandonedItems.Add(targetKey);
                _itemPursuitTimer  = 0f;
                _currentTargetKey  = int.MinValue;
                _elevatedJumpTimer = 0f;
                _iceWallPlaced     = false;
                string abandonLabel = usePickup ? "HealthPickup" : bestBerry?.GetType().Name ?? "Item";
                LogEvent("ITEM_ABANDONED",
                    $"{abandonLabel} at X={targetX:F0} unreachable after {ITEM_ABANDON_TIMEOUT}s — skipping");
                return false;   // fall through to goal pursuit this frame
            }

            // ── Elevated item detection ─────────────────────────────────
            float heightAbovePlayer = _player.Y - targetY;  // positive = item is higher on screen
            bool  itemIsElevated    = heightAbovePlayer > ELEVATED_ITEM_HEIGHT;

            if (itemIsElevated && suppressMovement)
            {
                _elevatedJumpTimer += _dt;

                if (_elevatedJumpTimer >= ICE_WALL_JUMP_THRESHOLD && !_iceWallPlaced
                    && _player.IceWallReady)
                {
                    if (dir > 0) { ShouldMoveRight = true; ShouldMoveLeft = false; }
                    else         { ShouldMoveLeft  = true; ShouldMoveRight = false; }

                    ShouldUseIceWall   = true;
                    _iceWallPlaced     = true;
                    _elevatedJumpTimer = 0f;
                    LogEvent("ICE_WALL_CLIMB",
                        $"Item at Y={targetY:F0} is {heightAbovePlayer:F0}px above — placing ice wall");
                }
                else
                {
                    ShouldMoveRight = dir > 15f;
                    ShouldMoveLeft  = dir < -15f;
                    ShouldJump      = true;
                }
            }
            else
            {
                _elevatedJumpTimer = 0f;
                _iceWallPlaced     = false;
            }

            if (suppressMovement)
            {
                CurrentState    = itemIsElevated ? "ICE_WALL_CLIMB" : "COLLECTING";
                if (!itemIsElevated)
                {
                    ShouldMoveRight = dir > 15f;
                    ShouldMoveLeft  = dir < -15f;
                }
                string label = usePickup ? "HealthPickup" : bestBerry.GetType().Name;
                float  dist  = usePickup ? bestPickupDist : bestBerryDist;
                LogEvent("COLLECT",
                    $"Target {label} X={targetX:F0} Dist={dist:F0} " +
                    $"PursuitTimer={_itemPursuitTimer:F1}s/{ITEM_ABANDON_TIMEOUT}s " +
                    $"Elevated={itemIsElevated}");
            }
            else
            {
                if (dir > 15f)  ShouldMoveRight = true;
                if (dir < -15f) ShouldMoveLeft  = true;
            }
            return suppressMovement;
        }

        // ══════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════

        private void RefreshLists()
        {
            _detectedEnemies.Clear();
            _detectedPickups.Clear();
            _detectedBerries.Clear();
            _detectedHazards.Clear();

            try
            {
                if (_enemiesField != null)
                {
                    var list = _enemiesField.GetValue(_scene) as List<Enemy>;
                    if (list != null) _detectedEnemies.AddRange(list);
                }
                // Boss scenes store the boss in _boss, not in _enemies.
                // Add the boss to the detected list so combat logic can find it.
                if (_bossField != null)
                {
                    var boss = _bossField.GetValue(_scene) as Enemy;
                    if (boss != null && boss.IsAlive && !_detectedEnemies.Contains(boss))
                        _detectedEnemies.Add(boss);
                }
            }
            catch { }

            try
            {
                if (_pickupsField != null)
                {
                    // IslandScene uses Entities.HealthPickup (public class).
                    // StormScene uses its own private nested HealthPickup class.
                    // Try the strong-typed cast first; fall back to duck-typing
                    // via reflection for non-matching types (reads X, Y, Active).
                    var list = _pickupsField.GetValue(_scene) as List<HealthPickup>;
                    if (list != null)
                    {
                        foreach (var p in list)
                            if (p != null && !p.Collected) _detectedPickups.Add(p);
                    }
                    else if (_isStormScene)
                    {
                        // StormScene.HealthPickup is a private nested class —
                        // iterate as IEnumerable, read X/Y/Active via reflection,
                        // and create synthetic HealthPickup wrappers for the bot.
                        var raw = _pickupsField.GetValue(_scene) as System.Collections.IEnumerable;
                        if (raw != null)
                        {
                            foreach (var item in raw)
                            {
                                if (item == null) continue;
                                var itemType = item.GetType();
                                var xField      = itemType.GetField("X");
                                var yField      = itemType.GetField("Y");
                                var activeField = itemType.GetField("Active");
                                if (xField == null || yField == null || activeField == null) continue;
                                bool active = (bool)activeField.GetValue(item);
                                if (!active) continue;
                                float px = (float)xField.GetValue(item);
                                float py = (float)yField.GetValue(item);
                                // Create a synthetic Entities.HealthPickup at the same
                                // position so the bot's item-pursuit logic works.
                                _detectedPickups.Add(new HealthPickup(px, py));
                            }
                        }
                    }
                }
            }
            catch { }

            try
            {
                if (_berriesField != null)
                {
                    var raw = _berriesField.GetValue(_scene) as System.Collections.IEnumerable;
                    if (raw != null)
                        foreach (var b in raw)
                        {
                            if (b is Entity e) _detectedBerries.Add(e);
                        }
                }
            }
            catch { }

            try
            {
                if (_hazardsField != null)
                {
                    var raw = _hazardsField.GetValue(_scene) as System.Collections.IEnumerable;
                    if (raw != null)
                        foreach (var h in raw)
                            if (h is Hazard hz) _detectedHazards.Add(hz);
                }
            }
            catch { }
        }

        /// <summary>
        /// Returns the X of the nearest WaterPit whose LEFT EDGE is within
        /// <see cref="PIT_LOOKAHEAD"/> pixels ahead of the player.
        /// Returns <c>float.MaxValue</c> when no pit is in range.
        /// </summary>
        private float FindNearestPitAheadX()
        {
            float nearest     = float.MaxValue;
            float playerRight = _player.X + _player.Width;
            foreach (var h in _detectedHazards)
            {
                if (h.Type != HazardType.WaterPit) continue;
                float pitLeft = h.X;
                if (pitLeft >= playerRight && pitLeft - playerRight <= PIT_LOOKAHEAD)
                    if (pitLeft < nearest) nearest = pitLeft;
            }
            return nearest;
        }

        // ══════════════════════════════════════════════════════════════════
        // GROUND-GAP DETECTION (tile / platform probing)
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Probes whether solid ground exists at <paramref name="probeX"/> within
        /// <see cref="GROUND_SEARCH_DEPTH"/> pixels below the player's feet.
        /// Checks both the flat ground baseline (<c>_groundY</c>) and every
        /// platform rectangle.  Returns true when a landing surface exists.
        /// </summary>
        private bool HasGroundAt(float probeX)
        {
            float feetY = _player.Y + _player.Height;

            // ── Check the flat ground baseline ────────────────────────────
            int groundY = GetGroundY();
            if (groundY > 0 && feetY <= groundY + 4f && feetY + GROUND_SEARCH_DEPTH >= groundY)
            {
                // Ground exists at this X unless a WaterPit occupies the spot
                if (!IsInsideWaterPit(probeX, groundY))
                    return true;
            }

            // ── Check platform rectangles ─────────────────────────────────
            var platforms = GetPlatforms();
            if (platforms != null)
            {
                foreach (var plat in platforms)
                {
                    // Platform must span this X horizontally
                    if (probeX < plat.Left || probeX > plat.Right) continue;
                    // Platform top must be reachable (below feet, within depth)
                    if (plat.Top >= feetY - 4f && plat.Top <= feetY + GROUND_SEARCH_DEPTH)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given world X at ground level falls inside any
        /// WaterPit hazard (the gaps cut into the ground surface).
        /// </summary>
        private bool IsInsideWaterPit(float worldX, float groundY)
        {
            foreach (var h in _detectedHazards)
            {
                if (h.Type != HazardType.WaterPit) continue;
                if (worldX >= h.X && worldX <= h.X + h.Width)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true when the player's leading edge has solid ground
        /// <paramref name="distance"/> pixels ahead.  This is the primary
        /// edge-detection check that prevents the bot from walking off ledges.
        /// </summary>
        private bool HasGroundAhead(float distance)
        {
            float probeX = ShouldMoveLeft
                ? _player.X - distance
                : _player.X + _player.Width + distance;
            return HasGroundAt(probeX);
        }

        /// <summary>
        /// Scans ahead for the nearest gap (no ground) within the given range.
        /// Returns the distance to the gap edge, or float.MaxValue if none found.
        /// Probes every 16 px for efficiency.
        /// </summary>
        private float FindNearestGapDistance(float maxRange)
        {
            float startX = _player.X + _player.Width;
            for (float dx = 16f; dx <= maxRange; dx += 16f)
            {
                if (!HasGroundAt(startX + dx))
                    return dx;
            }
            return float.MaxValue;
        }

        /// <summary>
        /// Returns true if a safe landing exists on the far side of a gap
        /// at the given distance.  Scans up to 200 px beyond the gap edge
        /// for a platform or ground surface the player could reach mid-jump.
        /// </summary>
        private bool CanJumpOverGap(float gapStartDist)
        {
            float startX = _player.X + _player.Width + gapStartDist;
            // Max horizontal jump distance at sprint speed (~270px/s) with
            // ~0.7 s air time ≈ ~190 px.  Scan in steps.
            for (float dx = 16f; dx <= 200f; dx += 16f)
            {
                if (HasGroundAt(startX + dx))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Reads the <c>_groundY</c> value from the inner scene via reflection.
        /// Returns -1 if unavailable.
        /// </summary>
        private int GetGroundY()
        {
            if (_groundYField == null) return -1;
            try
            {
                object val = _groundYField.GetValue(_scene);
                if (val is int i) return i;
            }
            catch { }
            return -1;
        }

        /// <summary>
        /// Reads the <c>_platforms</c> list from the inner scene via reflection.
        /// Returns null if unavailable.
        /// </summary>
        private List<Rectangle> GetPlatforms()
        {
            if (_platformsField == null) return null;
            try { return _platformsField.GetValue(_scene) as List<Rectangle>; }
            catch { return null; }
        }

        private Enemy FindNearestAliveEnemy()
        {
            Enemy  best = null;
            float  bestDist = float.MaxValue;
            foreach (var e in _detectedEnemies)
            {
                if (e == null || !e.IsAlive) continue;
                float d = Math.Abs(e.X - _player.X);
                if (d < bestDist) { bestDist = d; best = e; }
            }
            return best;
        }

        /// <summary>
        /// Reads the <c>_boss</c> Enemy field from boss scenes via reflection.
        /// Returns null on non-boss scenes or if the boss has not spawned.
        /// </summary>
        private Enemy GetBossEnemy()
        {
            try
            {
                if (_bossField != null)
                {
                    var obj = _bossField.GetValue(_scene);
                    if (obj is Enemy e) return e;
                }
            }
            catch { }
            return null;
        }

        private Rectangle GetExitFlag()
        {
            try
            {
                if (_exitFlagField != null)
                {
                    var obj = _exitFlagField.GetValue(_scene);
                    if (obj is Rectangle r) return r;
                }
            }
            catch { }
            return Rectangle.Empty;
        }

        // ══════════════════════════════════════════════════════════════════
        // FILE-BASED EVENT LOGGING
        // ══════════════════════════════════════════════════════════════════

        private void OpenEventLog()
        {
            try
            {
                string dir = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Directory.CreateDirectory(dir);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _logPath = Path.Combine(dir,
                    $"bot_{_scene.GetType().Name}_{timestamp}.log");

                _eventLog = new StreamWriter(_logPath, append: false) { AutoFlush = true };
                _eventLog.WriteLine(
                    $"=== BOT EVENT LOG  Scene={_scene.GetType().Name} " +
                    $"Started={DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            }
            catch { /* logging is best-effort */ }
        }

        /// <summary>Writes a tagged event to both the file log and Debug output.</summary>
        private void LogEvent(string tag, string message)
        {
            string line = $"[{_elapsedTime:F2}s] [{tag}] {message}";
            try { _eventLog?.WriteLine(line); } catch { }
            System.Diagnostics.Debug.WriteLine($"[BOT] {line}");
            _activityLogger?.LogPlatformingAction(tag, message);
        }

        public string GetStatus() =>
            $"State={CurrentState} HP={_player.Health:F0} " +
            $"Enemies={_detectedEnemies.Count} Stuck={IsStuck} " +
            $"Log={_logPath ?? "none"}";
    }
}


