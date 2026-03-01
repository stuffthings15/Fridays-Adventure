using System;
using System.Drawing;

namespace Fridays_Adventure.Entities
{
    public sealed class Berries : Entity
    {
        public bool Collected { get; set; }
        public int  Value     { get; } = 10;
        private float _bob;

        public Berries(float x, float y) : base(x, y, 16, 16) { }

        public override void Update(float dt) { _bob += dt; }

        public override void Draw(Graphics g)
        {
            if (Collected) return;
            float yOff = (float)Math.Sin(_bob * 4) * 3;
            using (var br = new SolidBrush(Color.Gold))
                g.FillEllipse(br, X + 1, Y + yOff + 1, 14, 14);
            using (var pen = new Pen(Color.DarkGoldenrod, 1))
                g.DrawEllipse(pen, X + 1, Y + yOff + 1, 14, 14);
            using (var br = new SolidBrush(Color.FromArgb(120, 255, 255, 200)))
                g.FillEllipse(br, X + 4, Y + yOff + 3, 6, 6);
        }
    }
}
