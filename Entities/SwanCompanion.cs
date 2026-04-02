using System.Drawing;
using Fridays_Adventure.Abilities;

namespace Fridays_Adventure.Entities
{
    /// <summary>
    /// Girl Swan — crew companion and agile speedster.  Age: 14 (canon).
    /// Fastest character, highest jump, but lightest HP pool.
    /// Signature move: WingDash (evasive dash with contact damage).
    /// </summary>
    public sealed class SwanCompanion : Character
    {
        private readonly WingDash _wingDash = new WingDash();

        public float WingDashCooldownProgress  => _wingDash.Progress;
        public float WingDashCooldownRemaining => _wingDash.Cooldown;
        public bool  WingDashReady             => _wingDash.IsReady;
        public bool  IsDashing                 => HasEffect(StatusEffect.Dodging);

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

        public bool UseWingDash()
        {
            if (!_wingDash.IsReady) return false;
            return _wingDash.TryUse(this);
        }

        public override void Update(float dt)
        {
            base.Update(dt);
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
