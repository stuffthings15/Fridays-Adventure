using System;
using System.Drawing;
using Fridays_Adventure.Entities;

namespace Fridays_Adventure.Hazards
{
    public sealed class WaterPit : Hazard
    {
        private float _waveTimer;
        public WaterPit(float x, float y, int w, int h) : base(x, y, w, h)
            => Type = HazardType.WaterPit;

        public override void Update(float dt) => _waveTimer += dt;

        public override void ApplyEffect(Character c, float dt)
        {
            if (!c.CannotSwim) return;
            c.ApplyEffect(StatusEffect.Sinking, 0.15f);
            c.VelocityX *= 0.25f;
            c.VelocityY  = 200f;
            c.IsGrounded = false;
        }

        public override void Draw(Graphics g)
        {
            using (var br = new SolidBrush(Color.FromArgb(200, 20, 90, 200)))
                g.FillRectangle(br, X, Y, Width, Height);
            using (var pen = new Pen(Color.FromArgb(140, 100, 170, 255), 2))
                for (int row = 0; row < 4; row++)
                {
                    float wy = Y + 6 + row * 14 + (float)Math.Sin(_waveTimer * 2 + row) * 3;
                    g.DrawArc(pen, X + 4, wy, Width - 8, 8, 0, 180);
                }
        }
    }
}
