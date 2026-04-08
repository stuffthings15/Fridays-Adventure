using System;
using System.Drawing;
using Fridays_Adventure.Data;

namespace Fridays_Adventure.Entities
{
    /// <summary>
    /// Gold berry collectible — increases the player's score (bounty) on pickup.
    /// Uses Kenney CC0 coin sprite (item_coin.png) instead of GDI ellipses.
    /// Inherits from Item for shared collectible logic.
    /// </summary>
    public sealed class Berries : Item
    {
        private float _bob;
        private float _baseY;

        /// <summary>Cached pre-scaled coin sprite, generated once at first draw.</summary>
        private static Bitmap _coinSprite;

        public Berries(float x, float y) : base(x, y, 16, 16, 10)
        {
            _baseY = y;
        }

        /// <summary>
        /// Re-anchors the berry's bob origin to the current Y position.
        /// Must be called after any external position scaling (e.g. LevelScale)
        /// so the bob animation stays aligned to the new world coordinates.
        /// </summary>
        public void SyncBaseY() { _baseY = Y; }

        public override void ApplyLevelScale(float scale)
        {
            X *= scale;
            Y *= scale;
            Width = (int)(Width * scale);
            Height = (int)(Height * scale);
            // Invalidate cached sprite so it regenerates at the new size
            _coinSprite = null;
            SyncBaseY();
        }

        public override void Update(float dt)
        {
            _bob += dt;
            // Keep logical hitbox aligned to visual bob so airborne coins remain collectable.
            Y = _baseY + (float)Math.Sin(_bob * 4f) * 3f;
        }

        public override void Draw(Graphics g)
        {
            if (Collected) return;
            float cx = X, cy = Y;
            int w = Width, h = Height;

            // ── Kenney CC0 coin sprite (item_coin.png) ──────────────────────
            // Pre-scale the 18×18 source tile to the berry's current dimensions.
            if (_coinSprite == null)
                _coinSprite = SpriteManager.GetScaled("item_coin.png", w, h);

            if (_coinSprite != null)
            {
                g.DrawImage(_coinSprite, cx, cy, w, h);
            }
            else
            {
                // Fallback: GDI gold coin if sprite file is missing
                using (var br = new SolidBrush(Color.FromArgb(255, 220, 0)))
                    g.FillEllipse(br, cx, cy, w, h);
                using (var pen = new Pen(Color.FromArgb(160, 100, 0), 1.5f))
                    g.DrawEllipse(pen, cx, cy, w, h);
            }
        }
    }
}
