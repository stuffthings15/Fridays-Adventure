using System.Drawing;
using Fridays_Adventure.Abilities;

namespace Fridays_Adventure.Entities
{
    /// <summary>
    /// Boy Orca — crew companion and heavy brawler.  Age: 17 (canon).
    /// Higher HP and attack than Miss Friday, but slower.
    /// Can swim — CannotSwim = false, so water hazards do not apply.
    /// Signature move: TidalSlam (ground-pound shockwave).
    /// </summary>
    public sealed class OrcaCompanion : Character
    {
        private readonly TidalSlam _tidalSlam = new TidalSlam();

        public float TidalSlamCooldownProgress  => _tidalSlam.Progress;
        public float TidalSlamCooldownRemaining => _tidalSlam.Cooldown;
        public bool  TidalSlamReady             => _tidalSlam.IsReady;

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

        public bool UseTidalSlam()
        {
            if (!_tidalSlam.IsReady) return false;
            return _tidalSlam.TryUse(this);
        }

        public override void Update(float dt)
        {
            base.Update(dt);
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
