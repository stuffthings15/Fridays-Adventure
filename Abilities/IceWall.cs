using System;
using System.Collections.Generic;
using System.Drawing;
using Fridays_Adventure.Data;

namespace Fridays_Adventure.Abilities
{
    public sealed class IceWall : Ability
    {
        public IceWall() : base("Ice Wall", 3f) { }
        protected override void OnUse(Entities.Character caster) { }
    }

    public sealed class IceWallInstance
    {
        public float X      { get; set; }
        public float Y      { get; set; }

        /// <summary>
        /// Visual and collision width of the wall.
        /// Default 20 px (standard).  Orca archetype spawns 40 px walls.
        /// Team 4 (Lead Game Designer) — Idea 6: Orca wide-wall passive.
        /// </summary>
        public int   Width  { get; }
        public int   Height { get; } = 88;
        public float Health { get; set; } = 100f;
        public float Age    { get; private set; }
        private const float MaxAge = 14f;
        public bool IsAlive => Health > 0 && Age < MaxAge;
        public Rectangle Hitbox => new Rectangle((int)X, (int)Y, Width, Height);

        /// <summary>Standard wall with default 20 px width.</summary>
        public IceWallInstance(float x, float y) : this(x, y, 20) { }

        /// <summary>
        /// Wall with a custom pixel width — used by Orca's wide-wall passive.
        /// Team 4 (Lead Game Designer) — Idea 6.
        /// </summary>
        public IceWallInstance(float x, float y, int width)
        {
            X     = x;
            Y     = y;
            Width = Math.Max(10, width);  // clamp to a sensible minimum
        }

        public void Update(float dt, bool nearFire)
        {
            Age += dt * (nearFire ? 4f : 1f);
        }

        public void Draw(Graphics g)
        {
            // Never render a dead wall — guards against any frame where
            // IceSystem.Update hasn't yet pruned the list.
            if (!IsAlive) return;

            float alpha = Math.Max(60, 200 - (Age / MaxAge) * 140);

            // ── Kenney CC0 ice tile (tile_ice.png) ──────────────────────────
            Bitmap iceTile = SpriteManager.GetScaled("tile_ice.png", 18, 18);
            if (iceTile != null)
            {
                // Semi-transparent ice body fill (tinted blue)
                using (var br = new SolidBrush(Color.FromArgb((int)(alpha * 0.3f), 160, 220, 255)))
                    g.FillRectangle(br, X, Y, Width, Height);

                // Tile the ice sprite across the wall surface
                for (int tx = (int)X; tx < (int)X + Width; tx += 18)
                {
                    for (int ty = (int)Y; ty < (int)Y + Height; ty += 18)
                    {
                        int dw = Math.Min(18, (int)X + Width - tx);
                        int dh = Math.Min(18, (int)Y + Height - ty);
                        g.DrawImage(iceTile, tx, ty, dw, dh);
                    }
                }

                // Ice panel border with transparency based on age
                using (var pen = new Pen(Color.FromArgb((int)alpha, 220, 240, 255), 2))
                    g.DrawRectangle(pen, X, Y, Width, Height);
            }
            else
            {
                // Fallback: GDI ice block
                using (var br = new SolidBrush(Color.FromArgb((int)alpha, 160, 220, 255)))
                    g.FillRectangle(br, X, Y, Width, Height);
                using (var pen = new Pen(Color.FromArgb((int)alpha, 220, 240, 255), 2))
                    g.DrawRectangle(pen, X, Y, Width, Height);
                using (var pen = new Pen(Color.FromArgb((int)(alpha * 0.4f), 200, 230, 255), 1))
                    for (int bx = (int)X + 8; bx < X + Width; bx += 8)
                        g.DrawLine(pen, bx, (int)Y + 4, bx, (int)Y + Height - 4);
            }
        }
    }
}
