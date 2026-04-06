using System.Drawing;
using Fridays_Adventure.Abilities;

namespace Fridays_Adventure.Entities
{
    /// <summary>
    /// PHASE 2 - Team 7: Gameplay Programmer
    /// Girl Swan — crew companion and agile speedster. Age: 14 (canon).
    /// Fastest character, highest jump, but lightest HP pool.
    /// Signature move: WingDash (evasive dash with contact damage).
    /// 
    /// NOW HAS COMPLETE ABILITY SYSTEM:
    /// - Dash (horizontal dash burst)
    /// - IceWall (creates frozen barriers)
    /// - FlashFreeze (AoE freeze on enemies)
    /// - BreakWall (shockwave attack)
    /// - FrostBall (projectile attacks)
    /// - WingDash (signature evasive dash)
    /// - TryDash works for all characters (Team 7 Idea 7)
    /// </summary>
    public sealed class SwanCompanion : Character
    {
        private readonly IceWall     _iceWall   = new IceWall();
        private readonly FlashFreeze _flash     = new FlashFreeze();
        private readonly BreakWall   _breakWall = new BreakWall();
        private readonly FrostBall   _frostBall = new FrostBall();
        private readonly WingDash    _wingDash  = new WingDash();

        // Energy costs per ability
        private const float EnergyCostIceWall   = 20f;
        private const float EnergyCostFlash     = 30f;
        private const float EnergyCostBreakWall = 25f;

        // Shared energy pool like Player
        public float Energy    { get; private set; } = 100f;
        public const float MaxEnergy = 100f;

        /// <summary>Attempts to drain energy for an ability.</summary>
        public bool DrainEnergy(float amount)
        {
            if (Energy < amount) return false;
            Energy = System.Math.Max(0f, Energy - amount);
            return true;
        }

        public float IceWallCooldownProgress     => _iceWall.Progress;
        public float FlashFreezeCooldownProgress => _flash.Progress;
        public float BreakWallCooldownProgress   => _breakWall.Progress;
        public float FrostBallCooldownProgress   => _frostBall.Progress;
        public float WingDashCooldownProgress    => _wingDash.Progress;

        public float IceWallCooldownRemaining     => _iceWall.Cooldown;
        public float FlashFreezeCooldownRemaining => _flash.Cooldown;
        public float BreakWallCooldownRemaining   => _breakWall.Cooldown;
        public float FrostBallCooldownRemaining   => _frostBall.Cooldown;
        public float WingDashCooldownRemaining    => _wingDash.Cooldown;

        public bool IceWallReady     => _iceWall.IsReady;
        public bool FlashFreezeReady => _flash.IsReady;
        public bool BreakWallReady   => _breakWall.IsReady;
        public bool FrostBallReady   => _frostBall.IsReady;
        public bool WingDashReady    => _wingDash.IsReady;

        public bool IsDashing        => HasEffect(StatusEffect.Dodging);

        // Hitbox in front of Swan during an active dash
        public Rectangle DashHitbox
        {
            get
            {
                if (!IsDashing) return Rectangle.Empty;
                return FacingRight
                    ? new Rectangle((int)X + Width, (int)Y + 4, 48, Height - 8)
                    : new Rectangle((int)X - 48,    (int)Y + 4, 48, Height - 8);
            }
        }

        public SwanCompanion(float x, float y) : base(x, y, 32, 58, 110)
        {
            CannotSwim   = true;
            MoveSpeed    = 230f;    // Fastest character in the crew
            JumpForce    = -480f;   // Highest jumper
            AttackDamage = 12;
            Abilities.Add(_wingDash);
        }

        // ── ABILITY METHODS (now includes all abilities) ──────────────────────

        /// <summary>Horizontal dash burst (Team 7 Idea 7).</summary>
        private float _dashTimer;
        private const float DashDuration  = 0.56f;
        private const float DashSpeed     = 500f;
        private float _dashCooldown;
        private const float DashCooldown  = 0.6f;
        public bool IsDashActive { get; private set; }

        public bool TryDash()
        {
            if (IsDashActive || _dashCooldown > 0f) return false;
            IsDashActive = true;
            _dashTimer   = DashDuration;
            _dashCooldown = DashCooldown;
            VelocityX    = FacingRight ? DashSpeed : -DashSpeed;
            ApplyEffect(StatusEffect.Dodging, DashDuration);
            return true;
        }

        public bool UseIceWall(out IceWallInstance wall)
        {
            wall = null;
            if (!_iceWall.IsReady || !DrainEnergy(EnergyCostIceWall)) return false;
            _iceWall.TryUse(this);

            float wx = FacingRight ? X + Width + 4 : X - 24;
            wall = new IceWallInstance(wx, 0f, 20);
            wall.Y = (Y + Height) - wall.Height;
            return true;
        }

        public bool UseFlashFreeze()
        {
            if (!_flash.IsReady || !DrainEnergy(EnergyCostFlash)) return false;
            _flash.TryUse(this);
            return true;
        }

        public bool UseBreakWall()
        {
            if (!_breakWall.IsReady || !DrainEnergy(EnergyCostBreakWall)) return false;
            bool used = _breakWall.TryUse(this);
            return used;
        }

        public bool TryShootFrostBall()
        {
            if (!_frostBall.IsReady) return false;
            _frostBall.TryUse(this);
            return true;
        }

        public bool UseWingDash()
        {
            if (!_wingDash.IsReady) return false;
            return _wingDash.TryUse(this);
        }

        public override void Update(float dt)
        {
            base.Update(dt);

            // Update dash timer
            if (IsDashActive)
            {
                _dashTimer -= dt;
                if (_dashTimer <= 0f) IsDashActive = false;
            }

            // Update dash cooldown
            if (_dashCooldown > 0f) _dashCooldown -= dt;

            // Passive energy regen
            Energy = System.Math.Min(MaxEnergy, Energy + 8f * dt);
        }

        protected override void DrawPlaceholder(Graphics g)
        {
            bool flash = IsInvincible && ((int)(InvincibilityTimer * 12) % 2 == 0);
            if (flash) return;

            // Body — white (canon: white/pink/gold, agile)
            g.FillRectangle(Brushes.White, X, Y + 18, Width, Height - 18);
            // Wing-band — rose pink (replaces amber for canon palette)
            using (var br = new SolidBrush(Color.FromArgb(232, 103, 138)))
                g.FillRectangle(br, X - 6, Y + 18, Width + 12, 8);
            // Gold neck collar
            using (var br = new SolidBrush(Color.Goldenrod))
                g.FillRectangle(br, X + 4, Y + 14, Width - 8, 5);
            // Head — snow white
            g.FillEllipse(Brushes.Snow, X + 5, Y, Width - 10, 22);
            // Gold beak dot on facing side
            using (var br = new SolidBrush(Color.Goldenrod))
                g.FillEllipse(br, FacingRight ? (int)X + Width - 8 : (int)X + 1,
                              (int)Y + 8, 7, 6);

            DrawHealthBar(g);
        }
    }
}
