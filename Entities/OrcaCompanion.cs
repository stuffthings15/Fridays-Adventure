using System.Drawing;
using Fridays_Adventure.Abilities;

namespace Fridays_Adventure.Entities
{
    /// <summary>
    /// PHASE 2 - Team 7: Gameplay Programmer
    /// Boy Orca — crew companion and heavy brawler. Age: 17 (canon).
    /// Higher HP and attack than Miss Friday, but slower.
    /// Can swim — CannotSwim = false, so water hazards do not apply.
    /// Signature move: TidalSlam (ground-pound shockwave).
    /// 
    /// NOW HAS COMPLETE ABILITY SYSTEM:
    /// - Dash (horizontal dash burst)
    /// - IceWall (creates frozen barriers)
    /// - FlashFreeze (AoE freeze on enemies)
    /// - BreakWall (shockwave attack)
    /// - FrostBall (projectile attacks)
    /// - TidalSlam (signature ground-pound shockwave)
    /// - TryDash works for all characters (Team 7 Idea 7)
    /// </summary>
    public sealed class OrcaCompanion : Character
    {
        private readonly IceWall     _iceWall   = new IceWall();
        private readonly FlashFreeze _flash     = new FlashFreeze();
        private readonly BreakWall   _breakWall = new BreakWall();
        private readonly FrostBall   _frostBall = new FrostBall();
        private readonly TidalSlam   _tidalSlam = new TidalSlam();

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
        public float TidalSlamCooldownProgress   => _tidalSlam.Progress;

        public float IceWallCooldownRemaining     => _iceWall.Cooldown;
        public float FlashFreezeCooldownRemaining => _flash.Cooldown;
        public float BreakWallCooldownRemaining   => _breakWall.Cooldown;
        public float FrostBallCooldownRemaining   => _frostBall.Cooldown;
        public float TidalSlamCooldownRemaining   => _tidalSlam.Cooldown;

        public bool IceWallReady     => _iceWall.IsReady;
        public bool FlashFreezeReady => _flash.IsReady;
        public bool BreakWallReady   => _breakWall.IsReady;
        public bool FrostBallReady   => _frostBall.IsReady;
        public bool TidalSlamReady   => _tidalSlam.IsReady;

        // Slam hitbox — circle approximated as a wide rectangle in front of feet
        public Rectangle SlamHitbox
        {
            get
            {
                if (!TidalSlamReady) return Rectangle.Empty;
                int r = (int)_tidalSlam.Radius;
                return new Rectangle((int)CenterX - r, (int)CenterY - r / 2, r * 2, r);
            }
        }

        public OrcaCompanion(float x, float y) : base(x, y, 44, 64, 160)
        {
            CannotSwim   = false;   // Fishman heritage — swims freely
            MoveSpeed    = 155f;    // Slower than Miss Friday
            JumpForce    = -420f;
            AttackDamage = 20;      // Heavy punches
            Abilities.Add(_tidalSlam);
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

            // Orca's Ice Wall is wider (40px vs 20px)
            float wx = FacingRight ? X + Width + 4 : X - 24;
            wall = new IceWallInstance(wx, 0f, 40);
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

        public bool UseTidalSlam()
        {
            if (!_tidalSlam.IsReady) return false;
            return _tidalSlam.TryUse(this);
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

            // Body — deep navy (canon: dark/navy, grounded, force)
            using (var br = new SolidBrush(Color.FromArgb(24, 44, 82)))
                g.FillRectangle(br, X, Y + 18, Width, Height - 18);
            // White belly patch — orca silhouette
            using (var br = new SolidBrush(Color.WhiteSmoke))
                g.FillRectangle(br, X + 8, Y + 32, Width - 16, Height - 42);
            // Head — deep navy
            using (var br = new SolidBrush(Color.FromArgb(24, 44, 82)))
                g.FillEllipse(br, X + 4, Y, Width - 8, 22);
            // White eye patch on facing side
            g.FillEllipse(Brushes.White, FacingRight ? (int)X + Width - 18 : (int)X + 4,
                          (int)Y + 3, 12, 9);
            // Thin gold strength stripe at shoulder line
            using (var br = new SolidBrush(Color.FromArgb(200, 160, 40)))
                g.FillRectangle(br, X, Y + 18, Width, 3);

            DrawHealthBar(g);
        }
    }
}
