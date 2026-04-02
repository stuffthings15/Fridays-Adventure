using System;
using System.Drawing;

namespace Fridays_Adventure.Entities
{
    /// <summary>
    /// Gold berry collectible — increases the player's score (bounty) on pickup.
    /// Inherits from Item for shared collectible logic.
    /// </summary>
    public sealed class Berries : Item
    {
        private float _bob;

        public Berries(float x, float y) : base(x, y, 16, 16, 10) { }

        public override void Update(float dt) { _bob += dt; }

        public override void Draw(Graphics g)
        {
            if (Collected) return;
            float yOff = (float)Math.Sin(_bob * 4) * 3;
            float cx   = X, cy = Y + yOff;

            // ── SMB3-style gold coin ─────────────────────────────────────────
            // Outer bright gold body
            using (var br = new SolidBrush(Color.FromArgb(255, 220, 0)))
                g.FillEllipse(br, cx, cy, 16, 16);
            // Inner darker ring — gives the coin a 3-D edge
            using (var br = new SolidBrush(Color.FromArgb(200, 150, 0)))
                g.FillEllipse(br, cx + 3, cy + 3, 10, 10);
            // Bright coin-face center
            using (var br = new SolidBrush(Color.FromArgb(255, 235, 80)))
                g.FillEllipse(br, cx + 4, cy + 4, 8, 8);
            // Crisp dark outline (SMB3 coins have a defined border)
            using (var pen = new Pen(Color.FromArgb(160, 100, 0), 1.5f))
                g.DrawEllipse(pen, cx, cy, 16, 16);
            // Shimmer highlight (top-left catch-light)
            using (var br = new SolidBrush(Color.FromArgb(210, 255, 255, 200)))
                g.FillEllipse(br, cx + 2, cy + 2, 5, 4);
        }
    }
}
