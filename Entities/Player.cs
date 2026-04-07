using System;
using System.Drawing;
using System.Drawing.Imaging;
using Fridays_Adventure.Abilities;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Entities
{
    public sealed class Player : Character
    {
        /// <summary>Active playable archetype captured at construction time.</summary>
        public PlayableCharacter Archetype { get; }

        public int  IceReserve    { get; set; } = 100;
        public int  MaxIceReserve { get; }      = 100;
        public bool IsSuppressed  => HasEffect(StatusEffect.Suppressed);
        public float MeltRisk     { get; set; }

        // ── Phase 2: Team 4 — Stamina system ──────────────────────────────────
        /// <summary>Current sprint stamina value [0..MaxStamina].</summary>
        public float Stamina { get; private set; } = 100f;
        /// <summary>Maximum stamina for sprinting.</summary>
        public float MaxStamina { get; } = 100f;
        /// <summary>True while sprint is actively consuming stamina this frame.</summary>
        public bool IsSprinting { get; private set; }

        // ── Phase 2: Team 4 — Energy Meter (Idea 1) ───────────────────────────
        /// <summary>
        /// Shared energy pool drained by all special abilities.
        /// Regenerates passively at 8 units/sec (full recharge ≈ 12 s).
        /// Phase 2 — Team 4 (Lead Game Designer) Idea 1: Energy Meter System.
        /// </summary>
        public float Energy    { get; private set; } = 100f;
        /// <summary>Maximum energy capacity.</summary>
        public const float MaxEnergy = 100f;

        // Energy costs per ability
        private const float EnergyCostIceWall   = 20f;
        private const float EnergyCostCharAbility= 30f;
        private const float EnergyCostBreakWall  = 25f;

        /// <summary>
        /// Attempts to drain <paramref name="amount"/> energy.
        /// Returns true and deducts energy if sufficient; false otherwise.
        /// </summary>
        public bool DrainEnergy(float amount)
        {
            if (Energy < amount) return false;
            Energy = Math.Max(0f, Energy - amount);
            return true;
        }

        /// <summary>Maximum number of jumps before landing (2 = double jump).</summary>
        public int MaxJumps       { get; set; } = 2;

        /// <summary>Jumps remaining this airborne cycle. Reset to MaxJumps on landing.</summary>
        public int JumpsRemaining { get; set; } = 2;

        private readonly IceWall     _iceWall   = new IceWall();
        private readonly FlashFreeze _flash     = new FlashFreeze();
        private readonly BreakWall   _breakWall = new BreakWall();
        // Character-unique E-key abilities (null for MissFriday who uses FlashFreeze)
        private readonly TidalSlam   _tidalSlam = new TidalSlam();
        private readonly WingDash    _wingDash  = new WingDash();
        // Frost Ball — blue ice projectile on X key (1s cooldown, all characters)
        private readonly FrostBall   _frostBall = new FrostBall();

        public float IceWallCooldownProgress     => _iceWall.Progress;
        public float FlashFreezeCooldownProgress =>
            Archetype == PlayableCharacter.Orca  ? _tidalSlam.Progress :
            Archetype == PlayableCharacter.Swan  ? _wingDash.Progress  :
            _flash.Progress;
        public float BreakWallCooldownProgress   => _breakWall.Progress;
        public float IceWallCooldownRemaining     => _iceWall.Cooldown;
        public float FlashFreezeCooldownRemaining =>
            Archetype == PlayableCharacter.Orca  ? _tidalSlam.Cooldown :
            Archetype == PlayableCharacter.Swan  ? _wingDash.Cooldown  :
            _flash.Cooldown;
        public float BreakWallCooldownRemaining   => _breakWall.Cooldown;
        public bool  IceWallReady    => _iceWall.IsReady;
        public bool  FlashFreezeReady =>
            Archetype == PlayableCharacter.Orca  ? _tidalSlam.IsReady :
            Archetype == PlayableCharacter.Swan  ? _wingDash.IsReady  :
            _flash.IsReady;
        public bool  BreakWallReady  => _breakWall.IsReady;

        // ── Frost Ball HUD properties ─────────────────────────────────────────
        /// <summary>Cooldown fill progress for Frost Ball (0=empty, 1=ready).</summary>
        public float FrostBallCooldownProgress  => _frostBall.Progress;
        /// <summary>Seconds remaining on Frost Ball cooldown.</summary>
        public float FrostBallCooldownRemaining => _frostBall.Cooldown;
        /// <summary>True when Frost Ball is ready to fire.</summary>
        public bool  FrostBallReady             => _frostBall.IsReady;

        /// <summary>Radius of Orca's Tidal Slam AOE.</summary>
        public float TidalSlamRadius => _tidalSlam.Radius;
        /// <summary>Damage dealt by Orca's Tidal Slam.</summary>
        public int   TidalSlamDamage => _tidalSlam.SlamDamage;
        /// <summary>Horizontal speed of Swan's Wing Dash.</summary>
        public float WingDashSpeed   => _wingDash.DashSpeed;

        // ── Team 7: Wall Jump ─────────────────────────────────────────────────
        /// <summary>True when the player is pressing against a left wall this frame.</summary>
        public bool IsOnLeftWall  { get; set; }
        /// <summary>True when the player is pressing against a right wall this frame.</summary>
        public bool IsOnRightWall { get; set; }
        /// <summary>True while the player is in the middle of a wall-jump arc.</summary>
        public bool IsWallJumping { get; private set; }
        private float _wallJumpTimer;
        private const float WallJumpDuration = 0.22f;

        // ── Phase 2 Team 7 #1: Wall Slide ────────────────────────────────────
        /// <summary>True while the player is sliding down a wall (Phase 2 T7 #1).</summary>
        public bool IsWallSliding { get; set; }
        /// <summary>Maximum fall speed while wall-sliding.</summary>
        public const float WallSlideSpeed = 55f;

        // ── Phase 2 Team 7 #2: Air Dash ──────────────────────────────────────
        /// <summary>True during the air-dash burst frame window (Phase 2 T7 #2).</summary>
        public bool AirDashActive { get; private set; }
        private float _airDashTimer;
        private const float AirDashDuration = 0.18f;
        private const float AirDashForce    = 420f;
        /// <summary>Whether the air dash has been used since last landing.</summary>
        public bool AirDashUsed { get; set; }

        // ── Phase 2 Team 4 #6: Parry ─────────────────────────────────────────
        /// <summary>Remaining seconds of the active parry window (Phase 2 T4 #6).</summary>
        public float ParryWindowTimer { get; private set; }
        private const float ParryWindowDuration = 0.14f;  // tight SMB3-style window
        /// <summary>True if the parry window is open this frame.</summary>
        public bool IsParrying => ParryWindowTimer > 0f;

        // ── Phase 2 Team 17 #1: Movement Trail ───────────────────────────────
        private readonly System.Collections.Generic.Queue<System.Drawing.PointF> _trail
            = new System.Collections.Generic.Queue<System.Drawing.PointF>();
        private float _trailSampleTimer;
        private const float TrailSampleInterval = 0.04f;
        private const int   TrailMaxPoints      = 8;

        // ── Team 7: Ground Pound ──────────────────────────────────────────────
        /// <summary>True while the player is actively ground-pounding downward.</summary>
        public bool IsGroundPounding { get; private set; }
        private float _groundPoundTimer;
        private const float GroundPoundCooldown = 0.5f;

        // ── Team 7: Coyote Time ───────────────────────────────────────────────
        /// <summary>Seconds remaining where a jump is still allowed after walking off a ledge.</summary>
        public float CoyoteTimeRemaining { get; private set; }
        private const float CoyoteTimeDuration = 0.10f;   // 6 frames at 60 fps
        private bool _wasGroundedLastFrame;

        // ── Team 7: Combo tracking ────────────────────────────────────────────
        /// <summary>How many enemies have been stomped in the current unbroken chain.</summary>
        public int StompChain { get; set; }

        /// <summary>
        /// Seconds remaining before the stomp chain decays back to zero.
        /// Phase 2 — Team 4 (Lead Game Designer) Idea 2: combo multiplier decay.
        /// </summary>
        public float StompChainTimer { get; private set; }
        private const float StompChainWindow = 2.5f;

        /// <summary>
        /// Registers a stomp in the combo chain and refreshes the decay timer.
        /// </summary>
        public void RegisterStompChain()
        {
            StompChain++;
            StompChainTimer = StompChainWindow;
        }

        /// <summary>
        /// Resets the stomp chain and timer.
        /// </summary>
        public void ResetStompChain()
        {
            StompChain = 0;
            StompChainTimer = 0f;
        }

        // ── Team 4: Lead Game Designer — character passive flags ──────────────
        /// <summary>
        /// When true (Orca archetype), placed Ice Walls are 40 px wide instead of 20 px.
        /// Team 4 (Lead Game Designer) — Idea 6: Orca IceWall wider than default.
        /// </summary>
        public bool OrcaWideWall      { get; private set; }

        /// <summary>
        /// When true (Swan archetype), Flash Freeze duration is multiplied by 1.25.
        /// Team 4 (Lead Game Designer) — Idea 7: Swan FlashFreeze lasts longer.
        /// </summary>
        public bool SwanExtendedFreeze { get; private set; }

        // ── Team 4: Lead Game Designer — P-Meter speed boost system ──────────
        /// <summary>
        /// Seconds the player has continuously run at near-full ground speed.
        /// Reaches PMeterThreshold to activate the SMB3-style speed boost.
        /// Team 4 (Lead Game Designer) — Idea 1: P-Meter run boost.
        /// </summary>
        public float PMeterCharge   { get; set; }

        /// <summary>True once PMeterCharge has filled — grants a 40 % speed multiplier.</summary>
        public bool  PMeterActive   => PMeterCharge >= PMeterThreshold;

        /// <summary>Seconds of continuous full-speed running needed to activate P-meter.</summary>
        private const float PMeterThreshold = 1.5f;

        // ── Team 7: Gameplay Programmer — Swan glide passive ──────────────────
        /// <summary>
        /// True while Swan is actively gliding (jump button held while airborne).
        /// Set by gameplay scenes; cleared when the glide duration expires.
        /// Team 7 (Gameplay Programmer) — Idea 2: Swan glide reduces fall gravity.
        /// </summary>
        public bool  IsGliding      { get; set; }
        private float _glideTimer;
        private const float GlideDuration = 1.8f;  // maximum glide window in seconds

        /// <summary>
        /// When true (set by gameplay scene), Orca's next landing triggers a ground-pound shockwave.
        /// Team 7 (Gameplay Programmer) — Idea 1: Orca ground pound AOE.
        /// </summary>
        public bool PendingGroundPoundShockwave { get; set; }

        // ── Auto-use health item when low ───────────────────────────────────────
        private bool _autoUsedHealth;

        // ── Team 17: VFX Artist — ability cast glow ───────────────────────────
        /// <summary>
        /// Seconds of ability-cast glow VFX remaining.
        /// Set to 0.3f whenever an ability activates; drives a brief colored outline in Draw.
        /// Team 17 (VFX Artist) — Idea 2: Ice ability cast cyan ring.
        /// </summary>
        public float AbilityCastGlowTimer { get; set; }

        /// <summary>
        /// Seconds of white damage-flash remaining (set on TakeDamage, drives overlay in Draw).
        /// Team 17 (VFX Artist) — Idea 3: Damage received white flash.
        /// </summary>
        public float DamageFlashTimer { get; private set; }

        // ── Constructor ────────────────────────────────────────────────────────
        /// <summary>
        /// Constructs a Player at world position (x, y) with 48×54 base dimensions
        /// and 100 base HP. Level scenes scale these by LevelScale (1.5×) to 72×81,
        /// which fits under platforms spaced 90–140 px above ground.
        /// The active archetype is resolved from Game.Instance so all
        /// gameplay scenes automatically use the crew screen selection.
        ///
        /// Team 1  (Game Director)       — character identity resolved once for every scene.
        /// Team 4  (Lead Game Designer)  — per-archetype stat scaling applied here.
        /// Team 7  (Gameplay Programmer) — passive ability flags initialized per archetype.
        /// Team 13 (Character Artist)    — archetype drives unique DrawPlaceholder visuals.
        /// </summary>
        public Player(float x, float y)
            : base(x, y, 48, 54, 100)
        {
            // Resolve archetype from game singleton; fall back to Miss Friday if unavailable.
            Archetype = Game.Instance?.SelectedCharacter ?? PlayableCharacter.MissFriday;

            // Per-archetype stat tweaks — Lead Game Designer ideas 2 & 3.
            switch (Archetype)
            {
                case PlayableCharacter.Orca:
                    // Orca: durable brawler — higher HP and attack, slightly slower.
                    MaxHealth    = 130; Health = 130;
                    MoveSpeed    = 160f;
                    AttackDamage = 14;
                    OrcaWideWall = true;          // Ice Wall is 40 px wide (idea 6)
                    break;

                case PlayableCharacter.Swan:
                    // Swan: nimble flyer — lower HP, higher speed, and longer freeze.
                    MaxHealth = 90;  Health = 90;
                    MoveSpeed = 210f;
                    JumpForce = -480f;            // slightly stronger jump for aerial gameplay
                    SwanExtendedFreeze = true;    // Flash Freeze lasts 25 % longer (idea 7)
                    break;

                // PlayableCharacter.MissFriday: balanced defaults from Character base ctor.
            }

            ApplySelectedSprite();

            // Register core abilities — all characters tick cooldowns for IceWall + BreakWall + FrostBall.
            Abilities.Add(_iceWall);
            Abilities.Add(_breakWall);
            Abilities.Add(_frostBall);   // Frost Ball available to all characters (X key)

            // E-key ability differs per archetype: Orca=TidalSlam, Swan=WingDash, Friday=FlashFreeze.
            switch (Archetype)
            {
                case PlayableCharacter.Orca: Abilities.Add(_tidalSlam); break;
                case PlayableCharacter.Swan: Abilities.Add(_wingDash);  break;
                default:                     Abilities.Add(_flash);     break;
            }
        }

        // ── AttackHitbox (Gameplay Programmer — compile fix + combat system) ──
        /// <summary>
        /// Forward-facing melee hitbox active during the attack animation window.
        /// Mirrors Enemy.AttackHitbox in shape so hit-detection is symmetrical.
        /// Returns Rectangle.Empty when the player is not attacking.
        /// Team 7 (Gameplay Programmer) — combat hit detection.
        /// </summary>
        public Rectangle AttackHitbox
        {
            get
            {
                if (!IsAttacking) return Rectangle.Empty;
                return FacingRight
                    ? new Rectangle((int)X + Width,  (int)Y + Height / 2 - 8, 32, 16)
                    : new Rectangle((int)X - 32,     (int)Y + Height / 2 - 8, 32, 16);
            }
        }

        // ── Sprite loading ────────────────────────────────────────────────────
        /// <summary>
        /// Loads and caches the character-specific sprite for this archetype and assigns
        /// it to <see cref="Entity.Sprite"/> so gameplay scenes render the actual model.
        /// Uses <see cref="SpriteManager.GetScaled"/> so the bitmap is shared across scenes.
        ///
        /// Asset mapping:
        ///   Orca       → Orca.png
        ///   Swan       → Swan.png
        ///   MissFriday → player_missfriday.png
        ///
        /// Team 13 (Character Artist) — archetype-specific sprite pipeline.
        /// </summary>
        public void ApplySelectedSprite()
        {
            // Prefer model art first so gameplay matches the intended character models.
            // SpriteManager resolves both Assets\Sprites\<file> and Assets\<file>.
            string[] candidates;
            switch (Archetype)
            {
                case PlayableCharacter.Orca:
                    candidates = new[]
                    {
                        "Models/Orca/Orca.png",
                        "Character Models/Boy Orca/Orca.png",
                        "player_Orca.png",
                    };
                    break;

                case PlayableCharacter.Swan:
                    candidates = new[]
                    {
                        "Models/Swan/Swan.png",
                        "Character Models/Girl Swan/Swan.png",
                        "player_Swan.png",
                    };
                    break;

                default:
                    candidates = new[]
                    {
                        "player_Miss_Friday.png",
                        "player_missfriday.png",
                    };
                    break;
            }

            Bitmap sprite = null;
            foreach (var file in candidates)
            {
                sprite = SpriteManager.GetScaled(file, Width, Height);
                if (sprite != null) break;
            }
            Sprite = sprite;
        }

        /// <summary>
        /// Overrides Entity.Draw so the VFX pipeline in DrawPlaceholder always runs,
        /// even when a real sprite has been loaded via ApplySelectedSprite().
        /// Entity.Draw skips DrawPlaceholder when Sprite != null, which would
        /// drop all ability glows, damage flashes, and lean animations.
        /// </summary>
        public override void Draw(Graphics g) => DrawPlaceholder(g);

        // ── Ability methods (with character-specific modifiers) ───────────────

        public bool UseIceWall(out IceWallInstance wall)
        {
            wall = null;
            // Phase 2 T4 #1: Energy gate — ability requires energy as well as cooldown.
            if (IsSuppressed || !_iceWall.IsReady || !DrainEnergy(EnergyCostIceWall)) return false;
            _iceWall.TryUse(this);

            float wx = FacingRight ? X + Width + 4 : X - 24;

            // Orca's Ice Wall is 40 px wide for better zoning (Lead Game Designer — idea 6).
            int wallW = OrcaWideWall ? 40 : 20;

            // Anchor wall base to player's feet so it does not appear floating in the air.
            wall = new IceWallInstance(wx, 0f, wallW);
            wall.Y = (Y + Height) - wall.Height;

            AbilityCastGlowTimer = 0.3f;  // trigger cast glow (VFX Artist)
            return true;
        }

        public bool UseFlashFreeze()
        {
            // Phase 2 T4 #1: Energy gate.
            if (IsSuppressed || !_flash.IsReady || !DrainEnergy(EnergyCostCharAbility)) return false;
            _flash.TryUse(this);
            AbilityCastGlowTimer = 0.3f;  // trigger cast glow (VFX Artist)
            return true;
        }

        /// <summary>
        /// PHASE 2 — E-key ability dispatcher.
        /// Orca  → TidalSlam (ground-pound AOE; scene handles damage radius).
        /// Swan  → WingDash  (directional burst + i-frames).
        /// Friday → FlashFreeze (standard freeze).
        /// Returns true if the ability activated successfully.
        /// </summary>
        /// <summary>
        /// PHASE 2 — E-key ability dispatcher.
        /// Orca  → TidalSlam (ground-pound AOE; scene handles damage radius).
        /// Swan  → WingDash  (directional burst + i-frames).
        /// Friday → FlashFreeze (standard freeze).
        /// Returns true if the ability activated successfully.
        /// Phase 2 T4 #1: All paths check energy via DrainEnergy before activating.
        /// </summary>
        public bool UseCharacterAbility()
        {
            // Phase 2 T4 #1: Energy gate for the character-unique ability.
            if (!DrainEnergy(EnergyCostCharAbility)) return false;
            AbilityCastGlowTimer = 0.3f;
            switch (Archetype)
            {
                case PlayableCharacter.Orca:
                    if (!_tidalSlam.IsReady)  { Energy = Math.Min(MaxEnergy, Energy + EnergyCostCharAbility); return false; }
                    _tidalSlam.TryUse(this);
                    // Apply dash forward (7x longer distance)
                    VelocityX = FacingRight ? 3500f : -3500f;
                    VelocityY = -100f; // slight upward
                    ApplyEffect(StatusEffect.Dodging, 0.3f); // brief i-frames
                    return true;
                case PlayableCharacter.Swan:
                    if (!_wingDash.IsReady)  { Energy = Math.Min(MaxEnergy, Energy + EnergyCostCharAbility); return false; }
                    _wingDash.TryUse(this);
                    return true;
                default:
                    // FlashFreeze already drained energy above; pass 0 to skip double-drain.
                    if (IsSuppressed || !_flash.IsReady) { Energy = Math.Min(MaxEnergy, Energy + EnergyCostCharAbility); return false; }
                    _flash.TryUse(this);
                    return true;
            }
        }

        /// <summary>
        /// Attempts to fire a Frost Ball projectile (X key).
        /// Returns true if the frost ball was successfully fired.
        /// 1-second cooldown, available to all characters.
        /// </summary>
        public bool TryShootFrostBall()
        {
            if (!_frostBall.IsReady) return false;
            _frostBall.TryUse(this);
            AbilityCastGlowTimer = 0.3f;
            return true;
        }

        /// <summary>
        /// Activates an air dash in the player's facing direction.
        /// Phase 2 — Team 7 #2: Air Dash.
        /// </summary>
        public void DoAirDash()
        {
            AirDashActive  = true;
            AirDashUsed    = true;
            _airDashTimer  = AirDashDuration;
            VelocityX      = FacingRight ? AirDashForce : -AirDashForce;
            VelocityY      = -60f;         // tiny upward nudge, feels snappy
            GrantIFrames(AirDashDuration); // brief i-frames while dashing
        }

        /// <summary>
        /// Opens the parry window on dodge start.
        /// Phase 2 — Team 4 #6: Parry System.
        /// </summary>
        public void OpenParryWindow()
        {
            ParryWindowTimer = ParryWindowDuration;
        }

        /// <summary>
        /// Checks whether an incoming attack can be parried.
        /// Returns true and expends the window if so.
        /// </summary>
        public bool TryParry()
        {
            if (!IsParrying) return false;
            ParryWindowTimer = 0f;
            GrantIFrames(0.5f);
            AbilityCastGlowTimer = 0.3f;
            return true;
        }

        /// <summary>
        /// Wall-jump: launches the player away from the wall and sets the jump lock timer.
        /// Team 7 (Gameplay Programmer).
        /// </summary>
        public void DoWallJump(bool wallOnRight)
        {
            // Push away from the wall with horizontal kick
            VelocityX      = wallOnRight ? -MoveSpeed * 1.4f : MoveSpeed * 1.4f;
            VelocityY      = JumpForce * 0.90f;
            IsGrounded     = false;
            JumpsRemaining = 0;  // consumed — no further jumps until landing
            IsWallJumping  = true;
            _wallJumpTimer = WallJumpDuration;
            IsOnLeftWall   = false;
            IsOnRightWall  = false;
        }

        /// <summary>
        /// Ground-pound: slam straight down at high speed.
        /// Team 7 (Gameplay Programmer).
        /// </summary>
        public void StartGroundPound()
        {
            if (IsGrounded || IsGroundPounding) return;
            IsGroundPounding  = true;
            VelocityY         = 700f;   // fast downward slam
            VelocityX         = 0f;
            _groundPoundTimer = GroundPoundCooldown;
        }

        /// <summary>
        /// Returns the freeze duration modifier for this character.
        /// Swan gets 25% extra freeze time.
        /// </summary>
        public float FreezeDurationMultiplier => SwanExtendedFreeze ? 1.25f : 1.0f;

        /// <summary>
        /// Returns a freeze duration adjusted for the selected character archetype.
        /// Swan gets longer freeze duration via FreezeDurationMultiplier.
        /// </summary>
        public float GetFlashFreezeDuration(float baseDuration) => baseDuration * FreezeDurationMultiplier;

        /// <summary>
        /// Orca gets stronger Break Wall shockwave damage than the default archetype.
        /// </summary>
        public int BreakWallShockwaveDamage => Archetype == PlayableCharacter.Orca ? 12 : 8;

        public bool UseBreakWall()
        {
            // Phase 2 T4 #1: Energy gate for Break Wall.
            if (!_breakWall.IsReady || !DrainEnergy(EnergyCostBreakWall)) return false;
            bool used = _breakWall.TryUse(this);
            if (used) AbilityCastGlowTimer = 0.3f;  // trigger cast glow (VFX Artist)
            return used;
        }

        public override void TakeDamage(int amount)
        {
            if (IsInvincible && amount > 0) return;   // Team 7 — Idea 8: i-frames block damage
            // GodMode (dev / bot testing) makes the player completely immune to damage.
            if (Engine.Game.Instance?.GodMode == true) return;
            base.TakeDamage(amount);
            if (amount > 0)
            {
                DamageFlashTimer = 0.12f;
                GrantIFrames(1.4f);   // 1.4 s i-frames after a hit (SMB3-style flash window)
            }
        }

        /// <summary>
        /// Applies stamina drain/regen for sprinting.
        /// Phase 2 — Team 4 (Lead Game Designer) Idea 8: stamina system.
        /// </summary>
        public void TickStamina(bool wantsSprint, float dt)
        {
            IsSprinting = wantsSprint && Stamina > 0.01f;
            if (IsSprinting)
                Stamina = Math.Max(0f, Stamina - 28f * dt);

            if (Stamina <= 0.01f)
                IsSprinting = false;
        }

        public override void Update(float dt)
        {
            base.Update(dt);

            // Reset jump counter when grounded (enables double jump next airborne cycle)
            if (IsGrounded)
            {
                JumpsRemaining   = MaxJumps;
                IsGroundPounding = false;
                IsGliding        = false;
                _glideTimer      = 0f;

                // If this is the first frame of landing after a ground pound, trigger shake.
                if (!_wasGroundedLastFrame && _groundPoundTimer <= 0f)
                {
                    if (Engine.Game.Instance != null)
                        Engine.Game.Instance.ScreenShake.Trigger(0.5f);
                }

                _groundPoundTimer = 0f;
            }

            // Coyote time: allow a jump for a brief window after walking off a ledge.
            if (_wasGroundedLastFrame && !IsGrounded)
                CoyoteTimeRemaining = CoyoteTimeDuration;
            else if (IsGrounded)
                CoyoteTimeRemaining = 0f;
            else
                CoyoteTimeRemaining = Math.Max(0f, CoyoteTimeRemaining - dt);

            _wasGroundedLastFrame = IsGrounded;

            // Wall-jump lock-out timer decay.
            if (_wallJumpTimer > 0f) { _wallJumpTimer -= dt; if (_wallJumpTimer <= 0f) IsWallJumping = false; }

            // Ground pound cooldown.
            if (_groundPoundTimer > 0f) _groundPoundTimer -= dt;

            // ── P-Meter speed boost (Lead Game Designer — Idea 1) ────────────
            // Accumulates while the player is running at near-full speed on the ground.
            // Drains quickly when stopping or changing direction.
            if (IsGrounded && Math.Abs(VelocityX) >= MoveSpeed * 0.9f)
                PMeterCharge = Math.Min(PMeterThreshold + 0.2f, PMeterCharge + dt);
            else
                PMeterCharge = Math.Max(0f, PMeterCharge - dt * 2.5f);

            // ── Swan glide decay (Gameplay Programmer — Idea 2) ──────────────
            // Scene sets IsGliding=true when jump is held; we enforce the duration cap here.
            if (IsGliding && !IsGrounded)
            {
                _glideTimer += dt;
                if (_glideTimer >= GlideDuration)
                {
                    IsGliding   = false;
                    _glideTimer = 0f;
                }
            }

            // ── Ability cast glow decay (VFX Artist — Idea 2) ────────────────
            if (AbilityCastGlowTimer > 0f) AbilityCastGlowTimer -= dt;

            // ── Damage flash decay (VFX Artist — Idea 3) ─────────────────────
            if (DamageFlashTimer > 0f) DamageFlashTimer -= dt;

            if (!IsSuppressed)
                IceReserve = Math.Min(MaxIceReserve, IceReserve + (int)(7 * dt));
            if (HasEffect(StatusEffect.Burning))
                MeltRisk = Math.Min(1f, MeltRisk + dt * 0.3f);
            else
                MeltRisk = Math.Max(0f, MeltRisk - dt * 0.1f);
            
            // Stamina passively recovers if not sprinting this frame.
                if (!IsSprinting)
                    Stamina = Math.Min(MaxStamina, Stamina + 10f * dt);

                // ── Phase 2 T4 #1: Energy regen (8 units/sec; suppressed stops it) ─
                if (!IsSuppressed)
                    Energy = Math.Min(MaxEnergy, Energy + 8f * dt);

            // ── Combo multiplier decay (Phase 2 — Team 4 Idea 2) ───────────────
            if (StompChainTimer > 0f)
            {
                StompChainTimer -= dt;
                if (StompChainTimer <= 0f)
                    ResetStompChain();
            }

            // ── Phase 2 T7 #2: Air Dash timer ────────────────────────────────
            if (AirDashActive)
            {
                _airDashTimer -= dt;
                if (_airDashTimer <= 0f) AirDashActive = false;
            }

            // ── Phase 2 T4 #6: Parry window decay ────────────────────────────
            if (ParryWindowTimer > 0f)
                ParryWindowTimer = Math.Max(0f, ParryWindowTimer - dt);

            // ── Reset air dash when grounded ──────────────────────────────────
            if (IsGrounded) AirDashUsed = false;

            // ── Phase 2 T17 #1: Trail position sampling ───────────────────────
            _trailSampleTimer += dt;
            if (_trailSampleTimer >= TrailSampleInterval)
            {
                _trailSampleTimer = 0f;
                // Only trail when moving fast or dashing
                bool fastEnough = Math.Abs(VelocityX) > MoveSpeed * 0.8f || AirDashActive || PMeterActive;
                if (fastEnough)
                {
                    _trail.Enqueue(new System.Drawing.PointF(X, Y));
                    while (_trail.Count > TrailMaxPoints)
                        _trail.Dequeue();
                }
                else if (_trail.Count > 0)
                {
                    _trail.Dequeue();
                }
            }

            // ── Auto-use health item when health drops below 30 ──────────────────
            if (Health >= 30)
                _autoUsedHealth = false;
            else if (Health < 30 && !_autoUsedHealth && PowerUpInventory.HealthItemCount > 0)
            {
                if (PowerUpInventory.UseHealthItem(this, 30))
                {
                    SMB3Hud.ShowToast("Health item from inventory used automatically.");
                    _autoUsedHealth = true;
                }
            }

            UpdateGameplayTeam7(dt);
        }

        // ── Team 7: Wall Jump ─────────────────────────────────────────────────
        /// <summary>
        /// Performs a wall jump — launches the player away from the wall.
        /// Returns true if the wall jump was executed.
        /// </summary>
        public bool TryWallJump()
        {
            if (!IsOnLeftWall && !IsOnRightWall) return false;
            if (IsGrounded) return false;

            // Kick away from whichever wall we're touching.
            float kickX = IsOnRightWall ? -MoveSpeed * 1.4f : MoveSpeed * 1.4f;
            VelocityX      = kickX;
            VelocityY      = JumpForce * 0.85f;
            IsWallJumping  = true;
            _wallJumpTimer = WallJumpDuration;
            JumpsRemaining = MaxJumps;   // refresh jumps on wall jump (SMB3 feel)

            Systems.AchievementSystem.Grant("ach_wall_jump");
            return true;
        }

        // ── Team 7: Ground Pound ──────────────────────────────────────────────
        /// <summary>
        /// Initiates a ground pound — cancels vertical velocity and slams downward.
        /// Returns true if the ground pound was started.
        /// </summary>
        public bool TryGroundPound()
        {
            if (IsGrounded || IsGroundPounding) return false;
            if (_groundPoundTimer > 0f) return false;

            IsGroundPounding = true;
            VelocityY        = 700f;    // aggressive downward slam
            VelocityX        = 0f;      // stop horizontal drift during pound
            _groundPoundTimer = GroundPoundCooldown;

            // Orca: flag a shockwave AOE on landing (Gameplay Programmer — Idea 1).
            if (Archetype == PlayableCharacter.Orca)
                PendingGroundPoundShockwave = true;

            // Achievement: ground pound for the first time
            AchievementSystem.Grant("ach_ground_pound");

            if (Engine.Game.Instance != null)
                Engine.Game.Instance.ScreenShake.Trigger(0.3f);

            return true;
        }

        // ── Team 7: Swan Glide ────────────────────────────────────────────────
        /// <summary>
        /// Activates Swan's glide — reduces downward velocity and slows fall.
        /// Should be called by gameplay scenes when jump is held while Swan is airborne.
        /// Returns true if glide was activated.
        /// Team 7 (Gameplay Programmer) — Idea 2: Swan glide passive.
        /// </summary>
        public bool TryStartGlide()
        {
            if (Archetype != PlayableCharacter.Swan) return false;
            if (IsGrounded || IsGliding) return false;
            if (_glideTimer > 0f) return false;

            IsGliding = true;
            return true;
        }

        // ── Team 7: Coyote Jump ───────────────────────────────────────────────
        /// <summary>
        /// Returns true when the player can jump using coyote time
        /// (just walked off a ledge within the grace window).
        /// </summary>
        public bool CanCoyoteJump() => CoyoteTimeRemaining > 0f && !IsGrounded && JumpsRemaining == MaxJumps;

        // ── Team 7 — Idea 3: Swim state / water physics ───────────────────────

        /// <summary>
        /// True while the player is inside a water hazard zone.
        /// When swimming: gravity is halved, max fall speed is reduced,
        /// and jump force produces a shorter upward stroke.
        /// Team 7 (Gameplay Programmer) — Idea 3.
        /// </summary>
        public bool IsSwimming { get; set; }

        /// <summary>
        /// Gravity multiplier applied while the player is inside water.
        /// Reduces fall acceleration for that floaty underwater feel.
        /// Team 7 (Gameplay Programmer) — Idea 3.
        /// </summary>
        public float GravityMultiplier => IsSwimming ? 0.35f : 1.0f;

        /// <summary>Max fall speed cap while swimming (pixels/second).</summary>
        public float MaxFallSpeed      => IsSwimming ? 160f   : 800f;

        // ── Team 7 — Idea 4: Vine climb ───────────────────────────────────────

        /// <summary>
        /// True while the player is gripping a vine block and climbing.
        /// Gravity is suppressed; VelocityY is set by vertical input instead.
        /// Team 7 (Gameplay Programmer) — Idea 4.
        /// </summary>
        public bool IsClimbingVine { get; set; }

        /// <summary>Vine climb speed in pixels/second (upward or downward).</summary>
        public const float VineClimbSpeed = 120f;

        // ── Team 7 — Idea 5: Conveyor belt / surface velocity ─────────────────

        /// <summary>
        /// Horizontal velocity (px/s) imparted by the surface the player stands on.
        /// Conveyor belts set this each frame; cleared when the player is airborne.
        /// Team 7 (Gameplay Programmer) — Idea 5.
        /// </summary>
        public float SurfaceVelocityX { get; set; }

        // ── Team 7 — Idea 6: Crouch / slide ──────────────────────────────────

        /// <summary>
        /// True while the player is crouching (Down held on ground).
        /// Reduces hitbox height and blocks attacks.
        /// Team 7 (Gameplay Programmer) — Idea 6.
        /// </summary>
        public bool IsCrouching { get; set; }

        /// <summary>
        /// True while performing a slide dash (Crouch + Run on ground).
        /// Briefly grants i-frames and a forward velocity burst.
        /// Team 7 (Gameplay Programmer) — Idea 6.
        /// </summary>
        public bool IsSliding   { get; private set; }
        private float _slideTimer;
        private const float SlideDuration = 0.35f;

        /// <summary>
        /// Starts a slide if the player is grounded, crouching, and has speed.
        /// Returns true when the slide begins.
        /// Team 7 (Gameplay Programmer) — Idea 6.
        /// </summary>
        public bool TrySlide()
        {
            if (!IsGrounded || IsSliding || Math.Abs(VelocityX) < 60f) return false;
            IsSliding     = true;
            _slideTimer   = SlideDuration;
            VelocityX    *= 1.6f;                     // burst of speed
            ApplyEffect(StatusEffect.Dodging, SlideDuration);   // brief i-frames
            return true;
        }

        // ── Team 7 — Idea 7: Quick dash ────────────────────────────────────────

        /// <summary>
        /// True while the player is in the active frames of a quick dash.
        /// Team 7 (Gameplay Programmer) — Idea 7.
        /// </summary>
        public bool IsDashing  { get; private set; }
        private float _dashTimer;
        private const float DashDuration  = 0.56f;  // 280px at 500px/s = 7 player lengths
        private const float DashSpeed     = 500f;
        private float _dashCooldown;
        private const float DashCooldown  = 0.6f;

        /// <summary>
        /// Performs a horizontal dash burst.
        /// Returns true when the dash starts.
        /// Team 7 (Gameplay Programmer) — Idea 7.
        /// </summary>
        public bool TryDash()
        {
            if (IsDashing || _dashCooldown > 0f) return false;
            IsDashing   = true;
            _dashTimer  = DashDuration;
            _dashCooldown = DashCooldown;
            VelocityX   = FacingRight ? DashSpeed : -DashSpeed;
            ApplyEffect(StatusEffect.Dodging, DashDuration);   // dash i-frames
            return true;
        }

        // ── Team 7 — Idea 8: I-frames / invincibility timer ───────────────────

        /// <summary>
        /// Seconds of invincibility remaining.
        /// Increases on TakeDamage to prevent rapid double-hits (Mega Man rule).
        /// Also used by slide and dash for brief i-frames.
        /// Team 7 (Gameplay Programmer) — Idea 8.
        /// </summary>
        public new float InvincibilityTimer { get; private set; }

        /// <summary>True while InvincibilityTimer > 0.</summary>
        public new bool IsInvincible => InvincibilityTimer > 0f;

        // ── Team 7 — Idea 9: Combo timer / decay ─────────────────────────────

        /// <summary>
        /// Seconds remaining on the active combo window.
        /// Each enemy stomp or hit refreshes this timer.
        /// At 0 the stomp chain is broken.
        /// Team 7 (Gameplay Programmer) — Idea 9.
        /// </summary>
        public float ComboTimer  { get; private set; }
        private const float ComboWindow = 2.0f;   // seconds between hits to keep combo

        /// <summary>
        /// Refreshes the combo timer on a successful hit.
        /// Team 7 (Gameplay Programmer) — Idea 9.
        /// </summary>
        public void RefreshCombo()
        {
            ComboTimer = ComboWindow;
        }

        // ── Team 7 — Idea 10: One-way platform flag ────────────────────────────

        /// <summary>
        /// True when the player is dropping through a one-way platform
        /// (Down + Jump pressed on a semi-solid tile).
        /// Cleared once the player fully passes below the platform.
        /// Team 7 (Gameplay Programmer) — Idea 10.
        /// </summary>
        public bool IsPassingThroughPlatform { get; set; }
        private float _platformPassTimer;
        private const float PlatformPassDuration = 0.25f;

        /// <summary>
        /// Initiates a one-way platform drop.
        /// Team 7 (Gameplay Programmer) — Idea 10.
        /// </summary>
        public bool TryDropThroughPlatform()
        {
            if (!IsGrounded || IsPassingThroughPlatform) return false;
            IsPassingThroughPlatform = true;
            _platformPassTimer       = PlatformPassDuration;
            VelocityY                = 80f;   // small downward nudge to clear the tile
            return true;
        }

        // ── Knockback recovery helper (Team 7 — Idea 1 complement) ────────────

        /// <summary>
        /// Grants invincibility frames after taking damage to prevent repeated hits.
        /// Called by TakeDamage internally.
        /// Team 7 (Gameplay Programmer) — Idea 8.
        /// </summary>
        private void GrantIFrames(float duration)
        {
            InvincibilityTimer = Math.Max(InvincibilityTimer, duration);
        }

        // NOTE: Override TakeDamage to hook i-frames (applied below the existing override)

        // ── Update additions ───────────────────────────────────────────────────
        // These are appended to the existing Update(float dt) override.
        // Call this helper from Update() after base.Update(dt).
        private void UpdateGameplayTeam7(float dt)
        {
            // Idea 8: invincibility timer decay
            if (InvincibilityTimer > 0f)
                InvincibilityTimer = Math.Max(0f, InvincibilityTimer - dt);

            // Idea 6: slide timer / cleanup
            if (IsSliding)
            {
                _slideTimer -= dt;
                if (_slideTimer <= 0f) IsSliding = false;
            }

            // Idea 7: dash timer / cleanup
            if (IsDashing)
            {
                _dashTimer -= dt;
                if (_dashTimer <= 0f) IsDashing = false;
            }
            if (_dashCooldown > 0f) _dashCooldown -= dt;

            // Idea 9: combo timer decay
            if (ComboTimer > 0f)
            {
                ComboTimer -= dt;
                if (ComboTimer <= 0f)
                    StompChain = 0;  // combo expired → chain breaks
            }

            // Idea 10: platform pass-through timer
            if (IsPassingThroughPlatform)
            {
                _platformPassTimer -= dt;
                if (_platformPassTimer <= 0f)
                    IsPassingThroughPlatform = false;
            }

            // Idea 5: conveyor belt — clear surface velocity when airborne
            if (!IsGrounded) SurfaceVelocityX = 0f;

            // Idea 3: water — cap fall speed
            if (IsSwimming && VelocityY > MaxFallSpeed)
                VelocityY = MaxFallSpeed;
        }
        
        protected override void DrawPlaceholder(Graphics g)
        {
            // ── Phase 2 T17 #1: Movement trail ghost ──────────────────────────
            var pts = _trail.ToArray();
            for (int ti = 0; ti < pts.Length; ti++)
            {
                float frac  = (float)(ti + 1) / (pts.Length + 1);
                int   alpha = (int)(60 * frac);
                using (var br = new SolidBrush(Color.FromArgb(alpha, AirDashActive ? Color.Cyan : Color.White)))
                    g.FillRectangle(br, pts[ti].X, pts[ti].Y, Width, Height);
            }

            // ── Mega Man hit-flash (blink during invincibility frames) ────────
            bool isStarInvincible = PowerUpInventory.IsInvincible;
            if (isStarInvincible)
            {
                // Rainbow cycle during star mode instead of simple blink
                float hue = (float)(Engine.Game.Instance?.Stats.PlaySeconds * 3.0 ?? 0) % 1.0f;
                using (var br = new SolidBrush(HueToColor(hue, 180)))
                    g.FillRectangle(br, X - 4, Y - 4, Width + 8, Height + 8);
            }
            else
            {
                bool flash = IsInvincible && ((int)(InvincibilityTimer * 12) % 2 == 0);
                if (flash) return;
            }

            // ── Team 16: 2D Animator — velocity lean ─────────────────────────
            // Lean body forward when running; degree proportional to horizontal speed.
            float leanFactor = VelocityX / Math.Max(1f, MoveSpeed);
            float leanDeg    = leanFactor * 12f;   // max ±12° lean

            // ── Team 13: Character Artist — Orca idle bob ─────────────────────
            // When Orca is idle on the ground, apply a gentle sine-wave vertical offset.
            float bobOffset = 0f;
            if (Archetype == PlayableCharacter.Orca && IsGrounded && Math.Abs(VelocityX) < 5f)
                bobOffset = (float)(Math.Sin(Engine.Game.Instance?.Stats.PlaySeconds * 3.0 ?? 0) * 2.5);

            // Draw everything inside a rotation transform for the velocity lean.
            var state = g.Save();
            g.TranslateTransform(X + Width / 2f, Y + Height / 2f + bobOffset);
            g.RotateTransform(leanDeg);
            g.TranslateTransform(-(X + Width / 2f), -(Y + Height / 2f + bobOffset));

            // ── Team 13: Character Artist — dodge transparency ────────────────
            // Player is slightly transparent during a dodge roll for visual feedback.
            float bodyAlpha = HasEffect(StatusEffect.Dodging) ? 180f : 255f;

            // ── Character body — sprite if loaded, otherwise geometric placeholder ──
            // ApplySelectedSprite() sets Sprite to the archetype asset (Orca/Swan/MissFriday).
            // When available the sprite is drawn with dodge-transparency; the VFX layers
            // below (ice glow, P-meter, cast glow, etc.) are still applied on top.
            if (Sprite != null)
            {
                using (var ia = new ImageAttributes())
                {
                    // Apply dodge transparency via ColorMatrix alpha scaling.
                    var cm = new ColorMatrix();
                    cm.Matrix33 = bodyAlpha / 255f;
                    ia.SetColorMatrix(cm);
                    g.DrawImage(Sprite,
                        new Rectangle((int)X, (int)Y, Width, Height),
                        0, 0, Sprite.Width, Sprite.Height,
                        GraphicsUnit.Pixel, ia);
                }
            }
            else if (Archetype == PlayableCharacter.Orca)
            {
                using (var br = new SolidBrush(Color.FromArgb((int)bodyAlpha, 24, 44, 82)))
                    g.FillRectangle(br, X, Y + 20, Width, Height - 20);
                g.FillEllipse(Brushes.WhiteSmoke, X + 6, Y + bobOffset, Width - 12, 24);
                using (var br = new SolidBrush(Color.FromArgb(200, 160, 40)))
                    g.FillRectangle(br, X - 2, Y + 26, Width + 4, 5);
            }
            else if (Archetype == PlayableCharacter.Swan)
            {
                // ── Team 13: Swan wing-stretch when airborne ──────────────────
                // Wings extend sideways while Swan is in the air or gliding.
                int wingSpread = (!IsGrounded) ? 8 : 2;
                g.FillRectangle(Brushes.White, X, Y + 20, Width, Height - 20);
                g.FillEllipse(Brushes.Snow, X + 6, Y, Width - 12, 24);
                using (var br = new SolidBrush(Color.FromArgb(232, 103, 138)))
                    g.FillRectangle(br, X - 2, Y + 26, Width + 4, 6);
                using (var br = new SolidBrush(Color.Goldenrod))
                    g.FillRectangle(br, X + 4, Y + 14, Width - 8, 4);
                // Wing outline
                if (!IsGrounded)
                    using (var pen = new Pen(Color.FromArgb(140, 255, 255, 255), 2))
                    {
                        g.DrawLine(pen, X - wingSpread, (int)Y + 30, (int)X + Width / 2, (int)Y + 20);
                        g.DrawLine(pen, X + Width + wingSpread, (int)Y + 30, (int)X + Width / 2, (int)Y + 20);
                    }
            }
            else
            {
                // Miss Friday default body palette.
                using (var br = new SolidBrush(Color.FromArgb((int)bodyAlpha, 30, 107, 191)))
                    g.FillRectangle(br, X, Y + 20, Width, Height - 20);
                g.FillEllipse(Brushes.AliceBlue, X + 6, Y, Width - 12, 24);
                using (var br = new SolidBrush(Color.FromArgb(232, 193, 74)))
                    g.FillRectangle(br, X - 4, Y, Width + 8, 7);
                using (var br = new SolidBrush(Color.FromArgb(204, 51, 51)))
                    g.FillRectangle(br, X - 2, Y + 26, Width + 4, 6);
            }

            // ── Ice-power glow — pulses cyan when reserve is above 40 % ──────
            float icePct = (float)IceReserve / MaxIceReserve;
            if (icePct > 0.4f)
                using (var br = new SolidBrush(Color.FromArgb((int)(50 * icePct), Color.Cyan)))
                    g.FillRectangle(br, X, Y, Width, Height);

            // ── Suppression tint ──────────────────────────────────────────────
            if (IsSuppressed)
                using (var br = new SolidBrush(Color.FromArgb(80, Color.Yellow)))
                    g.FillRectangle(br, X, Y, Width, Height);

            // ── Team 4 / 17: P-Meter active glow (speed boost visual) ─────────
            // A bright gold shimmer surrounds the player when the P-Meter is maxed.
            if (PMeterActive)
                using (var br = new SolidBrush(Color.FromArgb(60, Color.Gold)))
                    g.FillRectangle(br, X - 3, Y - 3, Width + 6, Height + 6);

            // ── Team 17: VFX — ability cast glow (cyan outline flash) ─────────
            if (AbilityCastGlowTimer > 0f)
            {
                float glowAlpha = AbilityCastGlowTimer / 0.3f;
                using (var pen = new Pen(Color.FromArgb((int)(160 * glowAlpha), Color.Cyan), 3))
                    g.DrawRectangle(pen, X - 2, Y - 2, Width + 4, Height + 4);
            }

            // ── Team 17: VFX — Swan glide aura ───────────────────────────────
            if (IsGliding)
                using (var br = new SolidBrush(Color.FromArgb(40, Color.White)))
                    g.FillRectangle(br, X - 6, Y, Width + 12, Height);

            // ── Mega Man buster arm — visible during attack animation ─────────
            if (IsAttacking)
            {
                int armX = FacingRight ? (int)X + Width : (int)X - 10;
                using (var br = new SolidBrush(Color.FromArgb(190, Color.Cyan)))
                    g.FillRectangle(br, armX, (int)Y + Height / 2 - 6, 10, 12);
                // Muzzle flash tip
                using (var br = new SolidBrush(Color.FromArgb(180, Color.White)))
                    g.FillEllipse(br, FacingRight ? armX + 8 : armX - 5, (int)Y + Height / 2 - 5, 7, 10);
            }

            // ── Dodge speed-lines (Mega Man slide/dodge visual) ───────────────
            if (HasEffect(StatusEffect.Dodging))
            {
                int dir = FacingRight ? -1 : 1;
                using (var pen = new Pen(Color.FromArgb(120, Color.White), 1))
                {
                    for (int i = 0; i < 4; i++)
                        g.DrawLine(pen,
                            X + dir * (8  + i * 7), Y + 10 + i * 10,
                            X + dir * (22 + i * 7), Y + 10 + i * 10);
                }
            }

            // ── Phase 2 T4 #6: Parry window flash (gold frame) ───────────────
            if (IsParrying)
            {
                float pf = ParryWindowTimer / ParryWindowDuration;
                using (var pen = new Pen(Color.FromArgb((int)(220 * pf), Color.Gold), 4))
                    g.DrawRectangle(pen, X - 4, Y - 4, Width + 8, Height + 8);
            }

            // ── Phase 2 T7 #1: Wall-slide indicator (faint blue side streak) ──
            if (IsWallSliding && !IsGrounded)
            {
                bool onRight = IsOnRightWall;
                float sx = onRight ? X + Width - 3 : X;
                using (var br = new SolidBrush(Color.FromArgb(110, 130, 190, 255)))
                    g.FillRectangle(br, sx, Y + 8, 3, Height - 8);
            }

            // ── Phase 2 T7 #2: Air-dash cyan afterimage border ────────────────
            if (AirDashActive)
                using (var pen = new Pen(Color.FromArgb(180, Color.Cyan), 2))
                    g.DrawRectangle(pen, X - 2, Y - 2, Width + 4, Height + 4);

            // Restore the rotation transform before drawing the health bar.
            g.Restore(state);

            DrawHealthBar(g);
        }

        /// <summary>
        /// Converts a HSV hue [0..1) to a fully-saturated RGB Color.
        /// Used for the star-mode rainbow cycle (Phase 2 — Team 16 #7 / Team 17 #1).
        /// </summary>
        private static Color HueToColor(float hue, int alpha)
        {
            float h = hue * 6f;
            float x = 1f - Math.Abs(h % 2f - 1f);
            float r, gr, b;
            if      (h < 1) { r = 1; gr = x; b = 0; }
            else if (h < 2) { r = x; gr = 1; b = 0; }
            else if (h < 3) { r = 0; gr = 1; b = x; }
            else if (h < 4) { r = 0; gr = x; b = 1; }
            else if (h < 5) { r = x; gr = 0; b = 1; }
            else            { r = 1; gr = 0; b = x; }
            return Color.FromArgb(alpha,
                (int)(r  * 255),
                (int)(gr * 255),
                (int)(b  * 255));
        }
    }
}
