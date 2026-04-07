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
        /// <summary>True when the inner scene is a FortressScene (rising lava, vertical climb).</summary>
        private readonly bool _isFortressScene;
        /// <summary>True when the inner scene is an AirshipLevelScene (auto-scrolling, no left movement).</summary>
        private readonly bool _isAirshipScene;

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

        // ── Pit-crossing state machine ────────────────────────────────────
        // Tracks whether the bot is actively crossing a gap so it can
        // maintain forward movement while airborne instead of stopping.
        private bool  _crossingGap = false;     // true while airborne over a validated gap
        private float _crossingDir = 1f;        // +1 = right, -1 = left
        private float _lastGroundedY;           // Y when the bot last stood on ground
        private float _fallingTimer = 0f;       // how long the bot has been falling
        private const float FALL_WALL_JUMP_TIME = 0.3f;  // start wall-jumping after falling this long

        // ── Post-pit-escape forward drive ─────────────────────────────────
        // After escaping a pit (wall-jump recovery), the bot forces forward
        // movement for a short period to clear the pit area and avoid
        // immediately falling back in.
        private float _pitEscapeForwardTimer = 0f;
        private const float PIT_ESCAPE_FORWARD_TIME = 0.6f; // seconds to force forward after escape

        // ── Combat ────────────────────────────────────────────────────────
        private Enemy _lastEnemy;
        private float _combatStartTime;
        private float _lastAttackTime;
        private const float COMBAT_DASH_TIMEOUT = 1.0f;   // dash if stomp fails for 1 s
        private const float ATTACK_COOLDOWN      = 0.3f;

        // ── Health management ────────────────────────────────────────────
        private float _medkitCooldown = 0f;  // prevents medkit spam

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
        /// <summary>
        /// True when the bot wants a double-jump on this jump sequence.
        /// Used by SkyIsland climbing and large gap crossings.
        /// BotPlayerController uses this to fire a proper two-press sequence.
        /// </summary>
        public bool   ShouldDoubleJump  { get; private set; }
        public bool   ShouldAttack      { get; private set; }
        public bool   ShouldMoveRight   { get; private set; }
        public bool   ShouldMoveLeft    { get; private set; }
        public bool   ShouldDodge       { get; private set; }

        /// <summary>
        /// True when the bot wants to perform a quick dash (C key) this frame.
        /// The dash works both grounded and airborne, grants i-frames, and
        /// has a short cooldown (0.6 s).  Used for closing distance in combat,
        /// escaping hazards, and extending air movement over gaps.
        /// </summary>
        /// <remarks>PHASE 2 - Air dash + reusable dash support</remarks>
        public bool   ShouldDash        { get; private set; }

        /// <summary>
        /// True when the bot wants to fire a Frost Ball (B key) this frame.
        /// Used as a ranged attack against enemies and bosses beyond melee range.
        /// </summary>
        public bool   ShouldFrostBall   { get; private set; }

        /// <summary>
        /// True when the bot wants to swim downward in UnderwaterScene.
        /// BotPlayerController injects Keys.Down when this is set.
        /// </summary>
        /// <remarks>PHASE 2 - Session 112: underwater vertical navigation</remarks>
        public bool   ShouldSwimDown    { get; private set; }

        /// <summary>
        /// Exposes the player's grounded state so BotPlayerController can
        /// reset the jump sequence when the player lands.
        /// </summary>
        public bool   IsPlayerGrounded  => _player != null && _player.IsGrounded;
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
            _isFortressScene   = scene is FortressScene;
            _isAirshipScene    = scene is AirshipLevelScene;

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

            // Try _exitFlag first (IslandScene, AirshipLevel), then _exitZone
            // (SkyIsland, Underwater), then _exitDoor (FortressScene)
            _exitFlagField = t.GetField("_exitFlag", flags)
                          ?? t.GetField("_exitZone", flags)
                          ?? t.GetField("_exitDoor", flags);

            // Open file-based event log
            OpenEventLog();

            LogEvent("BOT_INIT",
                $"Scene={scene.GetType().Name} IsStorm={_isStormScene} " +
                $"IsSky={_isSkyIslandScene} IsUnderwater={_isUnderwaterScene} " +
                $"IsBoss={_isBossScene} IsFortress={_isFortressScene} " +
                $"IsAirship={_isAirshipScene} ExitField={(_exitFlagField?.Name ?? "NONE")} " +
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
            if (_medkitCooldown > 0f) _medkitCooldown -= dt;
            if (_pitEscapeForwardTimer > 0f) _pitEscapeForwardTimer -= dt;

            // Reset outputs
            ShouldJump        = false;
            ShouldDoubleJump  = false;
            ShouldAttack      = false;
            ShouldMoveRight   = true;
            ShouldMoveLeft    = false;
            ShouldDodge       = false;
            ShouldDash        = false;
            ShouldFrostBall   = false;
            ShouldSwimDown    = false;
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

                // ── Health management — use medkit when low ────────────────
                // Call PowerUpInventory.UseHealthItem directly since there's
                // no keyboard shortcut — the HUD uses mouse clicks only.
                if (_player.Health < _player.MaxHealth * 0.4f && _medkitCooldown <= 0f)
                {
                    if (Fridays_Adventure.Systems.PowerUpInventory.UseHealthItem(_player))
                    {
                        _medkitCooldown = 3f;  // don't spam medkits
                        LogEvent("MEDKIT", $"Used medkit at HP={_player.Health}/{_player.MaxHealth}");
                    }
                }

                // ── Hazard avoidance — dodge FireSource and SeaStoneZone ───
                // Check if any active hazard hitbox is within 80px ahead.
                // If so, jump over it while maintaining forward movement.
                if (_player.IsGrounded && !_isSkyIslandScene)
                    RunHazardAvoidance();

                if (_isStormScene)
                    RunStormLogic();
                else if (_isBossScene)
                    RunBossFightLogic();
                else if (_isSkyIslandScene)
                    RunVerticalLogic();
                else if (_isUnderwaterScene)
                    RunUnderwaterLogic();
                else if (_isFortressScene)
                    RunFortressLogic();
                else
                    RunNormalLogic();

                // ── Universal pit / ledge avoidance + gap crossing ─────────────
                // Three phases:
                //   GROUNDED: detect edges and initiate jump WITH forward momentum.
                //   AIRBORNE CROSSING: maintain forward movement to reach the far side.
                //   FALLING RECOVERY: wall-jump or mash to escape if we fell in.
                //
                // DISABLED for SkyIslandScene: jumping off platform edges is
                // intentional — the bot must leap from one cloud to the next
                // higher one.  Pit avoidance would block all climbing.
                if (!_isStormScene && !_isSkyIslandScene)
                {
                    // ── Sinking recovery (WaterPit) — highest priority ─────
                    // When player has Sinking status, mash all inputs to escape.
                    if (_player.HasEffect(StatusEffect.Sinking))
                    {
                        ShouldJump      = true;
                        ShouldMoveRight = true;
                        ShouldMoveLeft  = false;
                        ShouldAttack    = true;  // counts as "mash"
                        LogEvent("SINKING_MASH", "Mashing to escape water pit");
                    }
                    else if (_player.IsGrounded)
                    {
                        // ── Phase 1: GROUNDED — edge detection ────────────────
                        _crossingGap   = false;
                        _fallingTimer  = 0f;
                        _lastGroundedY = _player.Y;

                        bool closeGround = HasGroundAhead(EDGE_PROBE_AHEAD);
                        bool farGround   = HasGroundAhead(EDGE_PROBE_FAR);

                        if (!closeGround)
                        {
                            // Ledge right at our feet
                            if (CanJumpOverGap(0f))
                            {
                                // Jump AND keep moving forward — momentum carries
                                // the bot over the gap. Stopping here was the old bug.
                                ShouldJump       = true;
                                ShouldDoubleJump = true;  // double-jump for max clearance
                                _crossingGap     = true;
                                _crossingDir     = 1f;  // always cross forward (right)
                                // Ensure forward movement is ON so the arc clears the gap
                                ShouldMoveRight = true;
                                ShouldMoveLeft  = false;
                                LogEvent("EDGE_JUMP",
                                    $"No ground {EDGE_PROBE_AHEAD}px ahead — jumping + moving RIGHT to clear gap");
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
                            // Gap approaching — start the jump early with forward momentum
                            float gapDist = FindNearestGapDistance(EDGE_PROBE_FAR);
                            if (gapDist != float.MaxValue && CanJumpOverGap(gapDist))
                            {
                                ShouldJump       = true;
                                ShouldDoubleJump = true;  // double-jump for max clearance
                                _crossingGap     = true;
                                _crossingDir     = 1f;  // always cross forward (right)
                                ShouldMoveRight  = true;
                                ShouldMoveLeft   = false;
                                LogEvent("GAP_PREJUMP",
                                    $"Gap at {gapDist:F0}px — pre-jumping RIGHT to clear");
                            }
                        }
                        else
                        {
                            // Both probes have ground — check WaterPit hazards as fallback
                            float pitX = FindNearestPitAheadX();
                            if (pitX != float.MaxValue)
                            {
                                float gap = pitX - (_player.X + _player.Width);
                                if (gap < 80f)
                                {
                                    // Close to pit — jump to clear it, keep moving forward
                                    ShouldJump       = true;
                                    ShouldDoubleJump = true;
                                    _crossingGap     = true;
                                    _crossingDir     = 1f;
                                    ShouldMoveRight  = true;
                                    ShouldMoveLeft   = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        // ── Phase 2: AIRBORNE — maintain crossing or recover ──

                        // Track falling duration for wall-jump recovery
                        if (_player.VelocityY > 0f)
                            _fallingTimer += _dt;
                        else
                            _fallingTimer = 0f;

                        if (_crossingGap)
                        {
                            // Actively crossing a gap — keep moving toward the far side
                            // This is critical: without this, the bot jumps straight up
                            // and lands in the same spot repeatedly.
                            ShouldMoveRight = _crossingDir > 0f;
                            ShouldMoveLeft  = _crossingDir < 0f;

                            // If we've reached ground on the far side, stop crossing
                            if (HasGroundAhead(EDGE_PROBE_AHEAD))
                            {
                                _crossingGap = false;
                            }
                        }

                        // ── Wall-jump recovery ────────────────────────────────
                        // If falling for too long (fell into a pit), press into
                        // the nearest wall and jump to wall-kick out.
                        // ALWAYS exit pits to the RIGHT (forward) to avoid
                        // jumping backward and re-entering the same hole.
                        bool fellIntoPit = _player.Y > _lastGroundedY + 60f
                                           && _fallingTimer > FALL_WALL_JUMP_TIME;

                        if (fellIntoPit)
                        {
                            // Try wall jump if touching a wall
                            if (_player.IsOnLeftWall || _player.IsOnRightWall)
                            {
                                ShouldJump = true;
                                // Always kick RIGHT (forward) regardless of which
                                // wall we're on.  The old code moved away from the
                                // wall, which sent the bot LEFT when on the right
                                // wall — directly back into the hole.
                                ShouldMoveRight = true;
                                ShouldMoveLeft  = false;
                                // Start the post-escape forward drive so the bot
                                // clears the pit area before resuming normal logic
                                _pitEscapeForwardTimer = PIT_ESCAPE_FORWARD_TIME;
                                LogEvent("WALL_JUMP_RECOVERY",
                                    $"Wall-jumping out of pit FORWARD — OnLeft={_player.IsOnLeftWall} OnRight={_player.IsOnRightWall}");
                            }
                            else
                            {
                                // Not touching a wall yet — press into the LEFT wall
                                // so we can wall-kick RIGHT (forward) out of the pit.
                                // Previously the bot picked the nearest wall, which
                                // could send it backward if the right wall was closer.
                                ShouldMoveLeft  = true;
                                ShouldMoveRight = false;
                                ShouldJump      = true;  // mash jump — catches wall contact
                                LogEvent("PIT_RECOVERY",
                                    $"Falling in pit Y={_player.Y:F0} — pressing LEFT to find wall, will kick RIGHT");
                            }
                        }
                    }
                }

                // ── Post-pit-escape forward drive ─────────────────────────────
                // After wall-jumping out of a pit, force forward movement for a
                // short time.  This prevents the bot from immediately turning
                // around and falling back into the same hole.
                if (_pitEscapeForwardTimer > 0f)
                {
                    ShouldMoveRight = true;
                    ShouldMoveLeft  = false;
                    ShouldJump      = true;  // keep jumping to clear the pit edge
                    CurrentState    = "PIT_ESCAPE_FORWARD";
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

            // ── Global airship guard — auto-scroll levels MUST never go left ─
            // Applied after all logic so combat retreat / item collection
            // can't accidentally send the bot backward into the scrolling edge.
            if (_isAirshipScene && ShouldMoveLeft)
            {
                ShouldMoveLeft  = false;
                ShouldMoveRight = true;
            }
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

            // Jump to stomp when close enough for a head-landing, or to
            // dodge ground attacks periodically.  Don't jump constantly —
            // that wastes time in the air when we could be walking to the boss.
            if (dist < 120f)
            {
                // Close to boss — stomp attempt
                ShouldJump = true;
            }
            else if (_jumpTimer >= 1.2f)
            {
                // Periodic dodge jump while approaching from range
                ShouldJump = true;
                _jumpTimer = 0f;
            }

            // Attack when within melee range
            if (dist < 120f && _elapsedTime - _lastAttackTime > ATTACK_COOLDOWN)
            {
                ShouldAttack    = true;
                _lastAttackTime = _elapsedTime;
            }

            // Frost Ball at range — softens the boss while closing distance
            if (dist > 80f && dist < 300f)
            {
                ShouldFrostBall = true;
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
        ///
        /// PHYSICS (computed from SkyIslandScene.cs):
        ///   Gravity     = 860 px/s²
        ///   JumpForce   = −520 px/s  → single jump peak ≈ 157 px
        ///   Double jump peak ≈ 314 px (jump again after 0.18 s cooldown)
        ///   Platform gap     = 250 px vertical, 18 px thick
        ///   Player           ≈ 48 × 56 px, MoveSpeed = 290 px/s
        ///   Horizontal range per double-jump ≈ 440 px (1.5 s air × 290 px/s)
        ///   LevelWidth  = 900, LevelHeight = 3200
        ///
        /// PLATFORMS ARE SOLID FROM BELOW — jumping under one bonks the head.
        /// The bot must jump from OUTSIDE the target platform's horizontal
        /// span to avoid headbonking, then drift inward during the arc.
        ///
        /// Algorithm:
        ///   1. Find the platform the player is standing on ("current").
        ///   2. Find the next platform above within double-jump height.
        ///   3. Walk to the edge of the CURRENT platform that faces the
        ///      target, so the horizontal distance is minimized.
        ///   4. Jump from that edge — always double-jump.
        ///   5. Drift toward target platform center while airborne.
        ///   6. Land on target.  Repeat until exit zone is reached.
        /// </summary>
        /// <summary>
        /// Vertical platformer strategy for SkyIslandScene.
        ///
        /// PLATFORMS ARE ONE-WAY — the player passes through from below and
        /// lands on top when falling.  No headbonk is possible.  This means
        /// the bot can jump from DIRECTLY UNDER the target platform and will
        /// pass through it, landing on top when VelocityY turns positive.
        ///
        /// Algorithm:
        ///   1. Find the next platform above within double-jump height.
        ///   2. Walk toward the target platform center on the current platform.
        ///   3. Jump + double-jump; drift toward platform center during arc.
        ///   4. The player passes through the platform from below and lands
        ///      on top at the apex / descent.
        ///   5. Repeat until exit zone is reached.
        /// </summary>
        /// <remarks>PHASE 2 - Simplified for one-way platform physics</remarks>
        private void RunVerticalLogic()
        {
            // ── Max horizontal reach during a double-jump arc ─────────
            const float MAX_JUMP_REACH = 400f;

            // ── P1: Kill enemies (only if very close) ─────────────────
            Enemy nearest = FindNearestAliveEnemy();
            if (nearest != null && Math.Abs(nearest.X - _player.X) < 150f
                && Math.Abs(nearest.Y - _player.Y) < 80f)
            {
                RunCombatLogic(nearest);
                return;
            }

            // ── Read platform data from the scene ─────────────────────
            var platforms = GetPlatforms();
            Rectangle exitZone = GetExitFlag();
            float playerCenterX = _player.X + _player.Width / 2f;
            float playerFeetY   = _player.Y + _player.Height;

            if (platforms == null || platforms.Count == 0)
            {
                // No platform data — fallback: jump and move right
                CurrentState = "SKY_NO_DATA";
                ShouldMoveRight = true;
                ShouldJump = _player.IsGrounded;
                ShouldDoubleJump = true;
                return;
            }

            // ── Find the platform the player is currently ON ──────────
            Rectangle currentPlat = Rectangle.Empty;
            foreach (var p in platforms)
            {
                float feetDist = Math.Abs(playerFeetY - p.Top);
                bool hOverlap = (_player.X + _player.Width) > p.Left && _player.X < p.Right;
                if (feetDist < 8f && hOverlap && _player.IsGrounded)
                {
                    currentPlat = p;
                    break;
                }
            }

            // ── Exit zone check — aim directly if close ───────────────
            if (exitZone != Rectangle.Empty)
            {
                float exitDistY = playerFeetY - exitZone.Y;
                if (exitDistY > 0f && exitDistY < 320f)
                {
                    CurrentState = "SKY_GOAL";
                    float dx = (exitZone.X + exitZone.Width / 2f) - playerCenterX;
                    ShouldMoveRight = dx > 10f;
                    ShouldMoveLeft  = dx < -10f;
                    ShouldDoubleJump = true;
                    if (_player.IsGrounded) ShouldJump = true;
                    return;
                }
            }

            // ── Find the next platform ABOVE to climb to ──────────────
            // Must be 10–320 px above player's feet (within double-jump).
            // Pick the CLOSEST one above (highest Y value = smallest gap).
            Rectangle targetPlat = Rectangle.Empty;
            float bestY = float.MinValue;
            foreach (var p in platforms)
            {
                float vDist = playerFeetY - p.Top;
                if (vDist < 10f || vDist > 320f) continue;
                if (p.Top > bestY)
                {
                    bestY = p.Top;
                    targetPlat = p;
                }
            }

            // ── No target platform found ──────────────────────────────
            if (targetPlat == Rectangle.Empty)
            {
                if (!_player.IsGrounded)
                {
                    // Airborne with no target above — land on nearest platform
                    CurrentState = "SKY_DRIFTING";
                    ShouldDoubleJump = true;
                    Rectangle landTarget = FindNearestPlatformBelow(platforms, playerCenterX, playerFeetY);
                    if (landTarget != Rectangle.Empty)
                    {
                        float dx = (landTarget.Left + landTarget.Width / 2f) - playerCenterX;
                        ShouldMoveRight = dx > 5f;
                        ShouldMoveLeft  = dx < -5f;
                    }
                    return;
                }
                // Grounded but nothing above — move to level center and jump
                CurrentState = "SKY_SEARCH";
                float toCenter = 450f - playerCenterX;
                ShouldMoveRight = toCenter > 20f;
                ShouldMoveLeft  = toCenter < -20f;
                ShouldJump = true;
                ShouldDoubleJump = true;
                return;
            }

            // ════════════════════════════════════════════════════════════
            // CORE CLIMB LOGIC — ONE-WAY PLATFORMS
            // Player passes through platforms from below, lands on top
            // when falling.  No headbonk possible, so the bot can jump
            // from directly under the target.
            // ════════════════════════════════════════════════════════════

            float targetCenterX = targetPlat.Left + targetPlat.Width / 2f;
            float distToCenter  = Math.Abs(targetCenterX - playerCenterX);

            if (_player.IsGrounded)
            {
                // ── GROUNDED ──────────────────────────────────────────
                // With one-way platforms the ideal launch position is
                // directly UNDER the target center so the bot rises
                // straight through and lands on top.

                if (distToCenter < 60f)
                {
                    // Close enough to center — JUMP straight up through it
                    CurrentState = "SKY_LAUNCH";
                    ShouldJump = true;
                    ShouldDoubleJump = true;
                    // Slight drift toward exact center during arc
                    float dx = targetCenterX - playerCenterX;
                    ShouldMoveRight = dx > 5f;
                    ShouldMoveLeft  = dx < -5f;

                    LogEvent("SKY_LAUNCH",
                        $"Jump-through from X={_player.X:F0} toward target " +
                        $"[{targetPlat.Left}-{targetPlat.Right}] Y={targetPlat.Top}");
                }
                else if (distToCenter < MAX_JUMP_REACH)
                {
                    // Within reach but not centered — jump with drift
                    CurrentState = "SKY_LAUNCH";
                    ShouldJump = true;
                    ShouldDoubleJump = true;
                    float dx = targetCenterX - playerCenterX;
                    ShouldMoveRight = dx > 0f;
                    ShouldMoveLeft  = dx < 0f;

                    LogEvent("SKY_LAUNCH",
                        $"Angled jump from X={_player.X:F0} dist={distToCenter:F0} " +
                        $"toward [{targetPlat.Left}-{targetPlat.Right}]");
                }
                else
                {
                    // Too far — walk toward the target center first
                    bool walkRight = targetCenterX > playerCenterX;
                    CurrentState = "SKY_WALK_TO_LAUNCH";
                    ShouldMoveRight = walkRight;
                    ShouldMoveLeft  = !walkRight;
                    ShouldJump = false;

                    // Edge guard: if at current platform edge, jump anyway
                    if (currentPlat != Rectangle.Empty)
                    {
                        float margin = 12f;
                        bool atLeftEdge  = _player.X <= currentPlat.Left + margin;
                        bool atRightEdge = (_player.X + _player.Width) >= currentPlat.Right - margin;
                        if ((ShouldMoveLeft && atLeftEdge) || (ShouldMoveRight && atRightEdge))
                        {
                            CurrentState = "SKY_EDGE_JUMP";
                            ShouldJump = true;
                            ShouldDoubleJump = true;
                            float dx = targetCenterX - playerCenterX;
                            ShouldMoveRight = dx > 0f;
                            ShouldMoveLeft  = dx < 0f;
                        }
                    }

                    LogEvent("SKY_WALK",
                        $"Walking toward target center X={targetCenterX:F0} " +
                        $"dist={distToCenter:F0}");
                }
            }
            else
            {
                // ── AIRBORNE: drift toward target platform center ─────
                CurrentState = "SKY_AIRBORNE";
                ShouldDoubleJump = true;

                float dx = targetCenterX - playerCenterX;
                ShouldMoveRight = dx > 5f;
                ShouldMoveLeft  = dx < -5f;

                // Tighten drift when above the target and falling
                bool aboveTarget = (_player.Y + _player.Height) < targetPlat.Top + 20f;
                bool falling     = _player.VelocityY > 0f;
                if (aboveTarget && falling)
                {
                    ShouldMoveRight = dx > 2f;
                    ShouldMoveLeft  = dx < -2f;
                }
            }
        }

        /// <summary>
        /// Finds the nearest platform that is below (or at) the player's
        /// current Y position, for landing recovery when drifting.
        /// </summary>
        /// <remarks>PHASE 2 - SkyIsland bot landing recovery helper</remarks>
        private Rectangle FindNearestPlatformBelow(List<Rectangle> platforms,
            float playerCenterX, float playerFeetY)
        {
            Rectangle best = Rectangle.Empty;
            float bestDist = float.MaxValue;
            foreach (var p in platforms)
            {
                // Platform must be at or below the player
                if (p.Top < playerFeetY - 10f) continue;
                float hDist = 0f;
                if (playerCenterX < p.Left) hDist = p.Left - playerCenterX;
                else if (playerCenterX > p.Right) hDist = playerCenterX - p.Right;
                float vDist = p.Top - playerFeetY;
                float totalDist = hDist + Math.Abs(vDist);
                if (totalDist < bestDist)
                {
                    bestDist = totalDist;
                    best = p;
                }
            }
            return best;
        }

        // ══════════════════════════════════════════════════════════════════
        // UNDERWATER LOGIC — buoyancy movement, seek exit zone
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Underwater navigation for UnderwaterScene.
        /// No gravity-based platforming — the player floats with buoyancy.
        /// Strategy: head toward exit zone, collect air bubbles when O2 is
        /// low, avoid coral hazards, and use frost ball on jellyfish.
        /// </summary>
        /// <remarks>PHASE 2 - Session 111: improved underwater navigation</remarks>
        private void RunUnderwaterLogic()
        {
            // ── P1: Pursue exit zone ──────────────────────────────────
            Rectangle exitZone = GetExitFlag();
            if (exitZone != Rectangle.Empty)
            {
                float exitCX = exitZone.X + exitZone.Width / 2f;
                float exitCY = exitZone.Y + exitZone.Height / 2f;
                float distX = exitCX - (_player.X + _player.Width / 2f);
                float distY = exitCY - (_player.Y + _player.Height / 2f);
                float dist  = (float)Math.Sqrt(distX * distX + distY * distY);

                CurrentState = "UNDERWATER_GOAL";
                ShouldMoveRight = distX > 15f;
                ShouldMoveLeft  = distX < -15f;

                // Swim up (Jump = Space/Up) or down (SwimDown = Down key)
                ShouldJump     = distY < -15f;
                ShouldSwimDown = distY > 15f;

                // Frost ball any jellyfish in the path (100-250px ahead)
                Enemy nearJelly = FindNearestAliveEnemy();
                if (nearJelly != null)
                {
                    float jDist = Math.Abs(nearJelly.X - _player.X);
                    if (jDist < 250f && jDist > 60f)
                        ShouldFrostBall = true;
                }

                LogEvent("UNDERWATER_GOAL",
                    $"Exit ({exitZone.X},{exitZone.Y}) dist={dist:F0} " +
                    $"dX={distX:F0} dY={distY:F0}");
                return;
            }

            // ── P2: Default exploration — swim toward upper-right ─────
            // Exit is typically in the upper-right area of the level.
            // Swim right and periodically up to explore.
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
            // Only engage enemies that are close enough to be a real threat.
            // 150px prevents the bot from chasing distant enemies backward
            // when it should be progressing toward the goal.
            Enemy nearest = FindNearestAliveEnemy();
            if (nearest != null && Math.Abs(nearest.X - _player.X) < 150f)
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
                // Double-jump for extra height to reach elevated platforms.
                // Only jump when ground exists ahead so we don't launch into pits.
                // (Pit/ledge detection is also handled by the universal check in Update.)
                if (_jumpTimer >= JUMP_INTERVAL && HasGroundAhead(EDGE_PROBE_FAR))
                {
                    ShouldJump       = true;
                    ShouldDoubleJump = true;
                    _jumpTimer       = 0f;
                }

                // Air dash to cover ground faster while airborne
                if (!_player.IsGrounded && !_player.IsDashing)
                    ShouldDash = true;

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
            // Double-jump for extra clearance over obstacles and to reach
            // elevated platforms.
            if (_jumpTimer >= JUMP_INTERVAL && HasGroundAhead(EDGE_PROBE_FAR))
            {
                ShouldJump       = true;
                ShouldDoubleJump = true;
                _jumpTimer       = 0f;
            }

            // Air dash while airborne to cover horizontal distance faster
            if (!_player.IsGrounded && !_player.IsDashing)
                ShouldDash = true;

            // Emergency fall recovery — if near the bottom of the screen,
            // mash jump and press into the nearest wall for a wall-kick.
            if (_player.Y > Game.Instance.CanvasHeight - 50f)
            {
                ShouldJump      = true;
                ShouldMoveRight = true;  // press into wall to trigger wall slide/jump
                LogEvent("FALL_RECOVERY", $"Y={_player.Y:F0} — mashing jump + wall seek");
            }

            // ── Airship override: NEVER move left in auto-scroll levels ───
            // AirshipLevelScene auto-scrolls to the right at a constant rate.
            // If the bot moves left, the scrolling left edge kills it.
            if (_isAirshipScene && ShouldMoveLeft)
            {
                ShouldMoveLeft  = false;
                ShouldMoveRight = true;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // FORTRESS LOGIC — vertical climb with rising lava
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// FortressScene has rising lava from below and platforms ascending
        /// toward an exit door near the top.  The bot must climb upward
        /// (not just move right) to stay above the lava and reach the exit.
        /// </summary>
        /// <remarks>PHASE 2 - Session 113: Fortress climbing AI</remarks>
        private void RunFortressLogic()
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // ── P1: Pursue exit door if visible ───────────────────────────
            Rectangle exitDoor = GetExitFlag();
            if (exitDoor != Rectangle.Empty)
            {
                float distX = (exitDoor.X + exitDoor.Width / 2f) - (_player.X + _player.Width / 2f);
                float distY = (exitDoor.Y + exitDoor.Height / 2f) - (_player.Y + _player.Height / 2f);

                // Close to exit? Go directly
                if (Math.Abs(distX) < 200f && Math.Abs(distY) < 200f)
                {
                    CurrentState    = "FORTRESS_EXIT";
                    ShouldMoveRight = distX > 10f;
                    ShouldMoveLeft  = distX < -10f;
                    ShouldJump      = _player.IsGrounded;
                    ShouldDoubleJump = true;
                    LogEvent("FORTRESS_EXIT",
                        $"Exit dX={distX:F0} dY={distY:F0}");
                    return;
                }
            }

            // ── P2: Find the best platform ABOVE current position ─────────
            // Always try to move upward to escape the rising lava.
            var platforms = GetPlatforms();
            float playerCenterX = _player.X + _player.Width / 2f;
            float playerFeet    = _player.Y + _player.Height;

            Rectangle bestAbove = Rectangle.Empty;
            float bestDist = float.MaxValue;

            foreach (var p in platforms)
            {
                // Only consider platforms above the player
                float pTop = p.Y;
                if (pTop >= playerFeet - 10f) continue;  // not above

                // Reachable vertically? (within ~250px above)
                float vertDist = playerFeet - pTop;
                if (vertDist > 300f) continue;

                // Prefer the closest-above platform
                float horizDist = Math.Abs((p.X + p.Width / 2f) - playerCenterX);
                float totalDist = vertDist + horizDist * 0.5f;

                if (totalDist < bestDist)
                {
                    bestDist  = totalDist;
                    bestAbove = p;
                }
            }

            if (bestAbove != Rectangle.Empty)
            {
                float targetCenterX = bestAbove.X + bestAbove.Width / 2f;
                float distToTarget  = targetCenterX - playerCenterX;

                CurrentState    = "FORTRESS_CLIMB";
                ShouldMoveRight = distToTarget > 15f;
                ShouldMoveLeft  = distToTarget < -15f;

                // Jump when grounded (or when close horizontally to the target)
                if (_player.IsGrounded && Math.Abs(distToTarget) < bestAbove.Width)
                {
                    ShouldJump       = true;
                    ShouldDoubleJump = true;
                }

                LogEvent("FORTRESS_CLIMB",
                    $"Target=({bestAbove.X},{bestAbove.Y}) dX={distToTarget:F0} bestDist={bestDist:F0}");
                return;
            }

            // ── P3: No platform above — move toward exit X and keep jumping ─
            // Fallback when at the top or no visible higher platform
            CurrentState    = "FORTRESS_SEARCH";
            ShouldMoveRight = true;
            if (_player.IsGrounded && _jumpTimer >= JUMP_INTERVAL)
            {
                ShouldJump       = true;
                ShouldDoubleJump = true;
                _jumpTimer       = 0f;
            }

            // ── P4: Lava panic — if player is near the lava, mash jump ──
            // Use reflected _lavaY field to detect actual lava proximity
            // instead of a fixed screen fraction.
            float lavaY = float.MaxValue;
            var lavaField = _scene.GetType().GetField("_lavaY",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (lavaField != null)
            {
                object val = lavaField.GetValue(_scene);
                if (val is float f) lavaY = f;
            }
            float distToLava = lavaY - (_player.Y + _player.Height);
            if (distToLava < 200f)
            {
                ShouldJump       = true;
                ShouldDoubleJump = true;
                ShouldMoveRight  = true;
                LogEvent("FORTRESS_LAVA_PANIC",
                    $"Y={_player.Y:F0} lavaY={lavaY:F0} gap={distToLava:F0} — mashing jump");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // HAZARD AVOIDANCE — dodge fire, sea stone, and other environmental hazards
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Scans for nearby hazards (FireSource, SeaStoneZone) and jumps
        /// over them while maintaining forward movement.  Only runs when
        /// grounded on non-Sky levels.
        /// </summary>
        private void RunHazardAvoidance()
        {
            float playerLeft  = _player.X;
            float playerRight = _player.X + _player.Width;
            float playerFeet  = _player.Y + _player.Height;

            foreach (var h in _detectedHazards)
            {
                // Skip water pits — handled by the pit detection system
                if (h.Type == HazardType.WaterPit) continue;

                float hLeft  = h.X;
                float hRight = h.X + h.Width;
                float hTop   = h.Y;

                // Only react to hazards near the player's vertical level
                if (hTop > playerFeet + 40f || hTop < _player.Y - 100f) continue;

                // Check if the hazard is ahead of the player (within 100px)
                bool hazardAhead = false;
                float distToHazard = 0f;

                if (ShouldMoveRight || (!ShouldMoveLeft))
                {
                    // Moving right — check hazards to the right
                    distToHazard = hLeft - playerRight;
                    hazardAhead = distToHazard > -20f && distToHazard < 100f;
                }
                else
                {
                    // Moving left — check hazards to the left
                    distToHazard = playerLeft - hRight;
                    hazardAhead = distToHazard > -20f && distToHazard < 100f;
                }

                if (hazardAhead)
                {
                    // Jump over the hazard — keep moving forward
                    ShouldJump = true;
                    LogEvent("HAZARD_JUMP",
                        $"Jumping over {h.Type} at dist={distToHazard:F0}px");
                    break;  // only handle closest hazard
                }

                // If we're ON TOP of a hazard (overlapping), jump immediately
                bool overlapping = playerRight > hLeft && playerLeft < hRight
                                && playerFeet > hTop && _player.Y < hTop + h.Height;
                if (overlapping)
                {
                    ShouldJump = true;
                    LogEvent("HAZARD_ESCAPE",
                        $"Standing on {h.Type} — jumping to escape");
                    break;
                }
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
            float absDist        = Math.Abs(dirX);

            // ── Health-based combat retreat ────────────────────────────────
            // When HP is critically low (< 25%) and no medkits available,
            // disengage from combat and prioritize survival / goal pursuit.
            // Still fire frost balls at range but don't charge in.
            if (_player.Health < _player.MaxHealth * 0.25f)
            {
                CurrentState = "COMBAT_RETREAT";
                // Move AWAY from enemy to avoid contact damage
                ShouldMoveRight = dirX < 0;
                ShouldMoveLeft  = dirX > 0;
                ShouldJump      = true;   // jump to dodge incoming attacks

                // Fire frost balls at range while retreating
                if (absDist < 300f)
                    ShouldFrostBall = true;

                LogEvent("COMBAT_RETREAT",
                    $"HP={_player.Health}/{_player.MaxHealth} — retreating from {target.GetType().Name}");
                return;
            }

            // ── Pit-safe movement: never charge toward an enemy across a gap ──
            // Check if ground exists between us and the enemy.  If a pit is
            // between us, disengage and let the universal pit handler take over.
            if (_player.IsGrounded && !HasGroundAhead(Math.Min(absDist, EDGE_PROBE_FAR)))
            {
                // There's a gap between us and the enemy — don't pursue
                ShouldMoveRight = false;
                ShouldMoveLeft  = false;
                ShouldFrostBall = absDist < 300f;  // shoot from across the gap
                ShouldAttack    = true;             // ranged attack if possible
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
            if (absDist < 80f && _elapsedTime - _lastAttackTime > ATTACK_COOLDOWN)
            {
                ShouldAttack    = true;
                _lastAttackTime = _elapsedTime;
            }

            // Frost Ball when enemy is beyond melee but within projectile range
            if (absDist > 100f && absDist < 300f)
            {
                ShouldFrostBall = true;
            }

            // After 1 s of failed stomping, use quick dash (C key) to burst
            // through the enemy with i-frames, and fire the character ability
            // (E key) as well for maximum damage/effect.  Both work in the air.
            if (combatDuration >= COMBAT_DASH_TIMEOUT && target.IsAlive)
            {
                ShouldDash  = true;   // C key — quick dash with i-frames (air-usable)
                ShouldDodge = true;   // E key — character ability (WingDash/TidalSlam/Freeze)
                LogEvent("COMBAT_DASH",
                    $"Enemy alive after {combatDuration:F1}s — dashing (C) + ability (E)");
                _combatStartTime = _elapsedTime;
            }

            // Also use double jump during combat to reach enemy heads for stomps
            ShouldDoubleJump = true;
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
            float totalMoved = (_isSkyIslandScene || _isUnderwaterScene || _isBossScene || _isFortressScene)
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

            // ── Sky Island stuck escape: move to a different X position ───
            // On vertical levels, "stuck" usually means the bot is head-bonking
            // the bottom of a platform above.  Moving sideways gets it out from
            // under the platform so RunVerticalLogic can jump beside it.
            if (_isSkyIslandScene)
            {
                // Alternate direction each stuck escape attempt
                bool goLeft = ((int)(_elapsedTime * 2f)) % 2 == 0;
                ShouldMoveRight = !goLeft;
                ShouldMoveLeft  = goLeft;
                ShouldJump      = false;  // don't jump while under a platform

                if (_stuckEscapeTimer <= 0f)
                {
                    _escapingBackward = false;
                    _stuckTimer       = 0f;
                    _stuckAnchorX     = _player.X;
                    _stuckAnchorY     = _player.Y;
                    LogEvent("STUCK_RESOLVED", $"SkyIsland side-step complete — resuming at X={_player.X:F0}");
                }
                return;
            }

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


