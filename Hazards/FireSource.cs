using System;
using System.Drawing;
using Fridays_Adventure.Entities;

namespace Fridays_Adventure.Hazards
{
    public sealed class FireSource : Hazard
    {
        public float HeatRadius { get; set; } = 90f;
        private float _anim;

        public FireSource(float x, float y, int w, int h) : base(x, y, w, h)
            => Type = HazardType.FireSource;

        public override void Update(float dt) => _anim += dt;

        public bool IsNear(float cx, float cy)
        {
            float dx = cx - (X + Width  * 0.5f);
            float dy = cy - (Y + Height * 0.5f);
            return Math.Sqrt(dx * dx + dy * dy) <= HeatRadius;
        }

        public override void ApplyEffect(Character c, float dt)
        {
            c.ApplyEffect(StatusEffect.Burning, 0.3f);
            c.TakeDamage((int)(12 * dt));
        }

        public override void Draw(Graphics g)
        {
            float flicker = (float)(Math.Sin(_anim * 9) * 0.15 + 0.85);
            using (var br = new SolidBrush(Color.FromArgb(220, Color.OrangeRed)))
                g.FillRectangle(br, X, Y, Width, Height);
            int fh = (int)(Height * 0.5f * flicker);
            using (var br = new SolidBrush(Color.FromArgb(200, Color.Yellow)))
                g.FillRectangle(br, X + 3, Y - fh, Width - 6, fh + 4);
        }
    }
}
