using System;
using System.Drawing;
using Fridays_Adventure.Abilities;

namespace Fridays_Adventure.Entities
{
    public sealed class Player : Character
    {
        public int  IceReserve    { get; set; } = 100;
        public int  MaxIceReserve { get; }      = 100;
        public bool IsSuppressed  => HasEffect(StatusEffect.Suppressed);
        public float MeltRisk     { get; set; }

        private readonly IceWall     _iceWall   = new IceWall();
        private readonly FlashFreeze _flash     = new FlashFreeze();
        private readonly BreakWall   _breakWall = new BreakWall();

        public float IceWallCooldownProgress     => _iceWall.Progress;
        public float FlashFreezeCooldownProgress => _flash.Progress;
        public float BreakWallCooldownProgress   => _breakWall.Progress;
        public bool  IceWallReady    => _iceWall.IsReady;
        public bool  FlashFreezeReady => _flash.IsReady;
        public bool  BreakWallReady  => _breakWall.IsReady;

        // Attack hitbox extends in facing direction
        public Rectangle AttackHitbox
        {
            get
            {
                if (!IsAttacking) return Rectangle.Empty;
                return FacingRight
                    ? new Rectangle((int)X + Width, (int)Y + 8, 42, Height - 16)
                    : new Rectangle((int)X - 42,    (int)Y + 8, 42, Height - 16);
            }
        }

        public Player(float x, float y) : base(x, y, 36, 56, 100)
        {
            CannotSwim    = true;
            MoveSpeed     = 195f;
            JumpForce     = -450f;
            AttackDamage  = 14;
            Abilities.Add(_iceWall);
            Abilities.Add(_flash);
            Abilities.Add(_breakWall);
        }

        public bool UseIceWall(out IceWallInstance wall)
        {
            wall = null;
            if (IsSuppressed || !_iceWall.IsReady) return false;
            _iceWall.TryUse(this);
            float wx = FacingRight ? X + Width + 4 : X - 24;
            wall = new IceWallInstance(wx, Y - 32);
            return true;
        }

        public bool UseFlashFreeze()
        {
            if (IsSuppressed || !_flash.IsReady) return false;
            _flash.TryUse(this);
            return true;
        }

        public bool UseBreakWall()
        {
            if (!_breakWall.IsReady) return false;
            _breakWall.TryUse(this);
            return true;
        }

        public override void TakeDamage(int amount)
        {
            if (Engine.Game.Instance.GodMode) return;
            base.TakeDamage(amount);
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            if (!IsSuppressed)
                IceReserve = Math.Min(MaxIceReserve, IceReserve + (int)(7 * dt));
            if (HasEffect(StatusEffect.Burning))
                MeltRisk = Math.Min(1f, MeltRisk + dt * 0.3f);
            else
                MeltRisk = Math.Max(0f, MeltRisk - dt * 0.1f);
        }

        protected override void DrawPlaceholder(Graphics g)
        {
            bool flash = IsInvincible && ((int)(InvincibilityTimer * 12) % 2 == 0);
            if (flash) return;

            // Body
            g.FillRectangle(Brushes.SteelBlue, X, Y + 20, Width, Height - 20);
            // Head
            g.FillEllipse(Brushes.PeachPuff, X + 6, Y, Width - 12, 24);
            // Scarf
            using (var br = new SolidBrush(Color.DarkCyan))
                g.FillRectangle(br, X - 4, Y + 20, Width + 8, 10);
            // Suppression tint
            if (IsSuppressed)
                using (var br = new SolidBrush(Color.FromArgb(80, Color.Yellow)))
                    g.FillRectangle(br, X, Y, Width, Height);

            DrawHealthBar(g);
        }
    }
}
