using System;
using System.Drawing;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Entities
{
    /// <summary>
    /// Simple med-kit pickup used by IslandScene.
    /// Restores one inventory health item when collected.
    /// </summary>
    public sealed class HealthPickup
    {
        public float X { get; set; }
        public float Y { get; set; }
        public bool Collected { get; private set; }

        private const int W = 20;
        private const int H = 20;
        private float _bob;

        public Rectangle Hitbox => new Rectangle((int)X, (int)Y, W, H);

        public HealthPickup(float x, float y)
        {
            X = x;
            Y = y;
        }

        public void Update(float dt)
        {
            if (Collected) return;
            _bob += dt;
        }

        public bool TryCollect(Player player)
        {
            if (Collected) return false;
            if (!player.Hitbox.IntersectsWith(Hitbox)) return false;
            Collected = true;
            return true;
        }

        public void Draw(Graphics g)
        {
            if (Collected) return;
            float by = (float)Math.Sin(_bob * 3f) * 2f;
            int drawY = (int)(Y + by);

            using (var back = new SolidBrush(Color.FromArgb(220, 200, 30, 30)))
                g.FillRectangle(back, X, drawY, W, H);
            using (var cross = new SolidBrush(Color.White))
            {
                g.FillRectangle(cross, X + 8, drawY + 3, 4, 14);
                g.FillRectangle(cross, X + 3, drawY + 8, 14, 4);
            }
            g.DrawRectangle(Pens.White, (int)X, drawY, W, H);
        }
    }
}
