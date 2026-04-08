using System;
using System.Drawing;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Entities
{
    /// <summary>
    /// SMB3-style moving platform — travels between two waypoints at constant speed.
    ///
    /// Team 5  (Level Designer)     — authoring: place via constructor with left/right or top/bottom extents.
    /// Team 7  (Gameplay Programmer)— integration: scenes query Bounds and IsMovingHorizontal for physics.
    /// Team 10 (Engine Optimizer)   — off-screen culling: IsOnScreen check before updating.
    ///
    /// ── Scene integration ────────────────────────────────────────────────────
    /// 1. Declare: private List&lt;MovingPlatform&gt; _movingPlatforms;
    /// 2. Instantiate in OnEnter with extents.
    /// 3. In Update: foreach(var mp in _movingPlatforms) mp.Update(dt, _player);
    /// 4. In Draw:   foreach(var mp in _movingPlatforms) mp.Draw(g);
    /// 5. Include mp.Bounds in platform collision alongside static platforms.
    /// </summary>
    public sealed class MovingPlatform
    {
        // ── Config ────────────────────────────────────────────────────────────
        /// <summary>Platform tile dimensions.</summary>
        public int Width  { get; private set; }
        public int Height { get; } = 16;

        /// <summary>Current top-left world position.</summary>
        public float X { get; private set; }
        public float Y { get; private set; }

        /// <summary>Axis-aligned bounding box for collision.</summary>
        public Rectangle Bounds => new Rectangle((int)X, (int)Y, Width, Height);

        /// <summary>True if the platform moves along the X axis, false for Y axis.</summary>
        public bool IsMovingHorizontal { get; }

        // ── Movement state ────────────────────────────────────────────────────
        private readonly float _startA, _startB;   // extent A and extent B (world coords)
        private readonly float _speed;             // pixels per second
        private int            _direction = 1;     // +1 forward, -1 backward

        // ── Velocity this frame (used by rider physics) ───────────────────────
        /// <summary>Platform velocity X this frame — add to player X when riding.</summary>
        public float VelocityX { get; private set; }
        /// <summary>Platform velocity Y this frame — add to player Y when riding.</summary>
        public float VelocityY { get; private set; }

        // ── Construction ──────────────────────────────────────────────────────
        /// <summary>
        /// Creates a moving platform.
        /// </summary>
        /// <param name="x">Initial world X.</param>
        /// <param name="y">Initial world Y.</param>
        /// <param name="width">Tile width (typically 64–128).</param>
        /// <param name="extentA">Lower bound of movement range (left or top).</param>
        /// <param name="extentB">Upper bound of movement range (right or bottom).</param>
        /// <param name="speed">Movement speed in pixels/second.</param>
        /// <param name="horizontal">True = horizontal movement; false = vertical.</param>
        public MovingPlatform(float x, float y, int width,
                              float extentA, float extentB,
                              float speed = 80f, bool horizontal = true)
        {
            X      = x;
            Y      = y;
            Width  = width;
            _startA          = Math.Min(extentA, extentB);
            _startB          = Math.Max(extentA, extentB);
            _speed           = speed;
            IsMovingHorizontal = horizontal;
        }

        /// <summary>
        /// Scales world-space positions and extents by a uniform factor.
        /// Called by IslandScene.ApplyLevelScale() after level construction.
        /// Team 5 (Level Designer) — scale support.
        /// </summary>
        public void ApplyScale(float scale)
        {
            X     *= scale;
            Y     *= scale;
            Width  = (int)(Width * scale);
            ApplyScaleInternal(scale);
        }

        // Backing mutable copies of extents, patched by ApplyScale.
        private float _scaledA, _scaledB;
        private bool  _scaleApplied;

        private void ApplyScaleInternal(float scale)
        {
            if (!_scaleApplied)
            {
                _scaledA      = _startA * scale;
                _scaledB      = _startB * scale;
                _scaleApplied = true;
            }
        }

        // Override extent accessors to use scaled values when present.
        private float EffectiveA => _scaleApplied ? _scaledA : _startA;
        private float EffectiveB => _scaleApplied ? _scaledB : _startB;

        // ── Update ────────────────────────────────────────────────────────────
        /// <summary>
        /// Moves the platform and carries a riding player with it.
        /// </summary>
        /// <param name="dt">Delta time in seconds.</param>
        /// <param name="player">Active player — pass null to skip rider physics.</param>
        public void Update(float dt, Player player)
        {
            float move = _speed * _direction * dt;
            VelocityX = 0f;
            VelocityY = 0f;

            // Use scaled extents when ApplyScale has been called.
            float a = EffectiveA;
            float b = EffectiveB;

            if (IsMovingHorizontal)
            {
                X += move;
                VelocityX = move;

                if (X <= a) { X = a; _direction =  1; }
                if (X >= b - Width) { X = b - Width; _direction = -1; }
            }
            else
            {
                Y += move;
                VelocityY = move;

                if (Y <= a) { Y = a; _direction =  1; }
                if (Y >= b) { Y = b; _direction = -1; }
            }

            // Carry the player if they are standing on top.
            if (player != null && IsPlayerRiding(player))
            {
                player.X += VelocityX;
                player.Y += VelocityY;
            }
        }

        // ── Rider detection ───────────────────────────────────────────────────
        /// <summary>
        /// Returns true when the player is standing on top of this platform.
        /// </summary>
        private bool IsPlayerRiding(Player player)
        {
            var pb = player.Hitbox;
            // Player feet must be within 4px of the platform top, and horizontally overlapping.
            int platformTop = (int)Y;
            int feetY = pb.Bottom;
            return feetY >= platformTop - 4 && feetY <= platformTop + 8 &&
                   pb.Right > (int)X && pb.Left < (int)X + Width;
        }

        // ── Draw ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Draws the platform using Kenney CC0 wood tile (tile_wood_plank.png).
        /// Falls back to SMB3-style GDI brick rendering if sprite is unavailable.
        /// </summary>
        public void Draw(Graphics g)
        {
            int ix = (int)X, iy = (int)Y;

            // ── Kenney CC0 wood plank tile ────────────────────────────────────
            Bitmap woodTile = SpriteManager.GetScaled("tile_wood_plank.png", 18, 18);
            if (woodTile != null)
            {
                // Base fill behind tiles
                using (var br = new SolidBrush(Color.FromArgb(160, 100, 50)))
                    g.FillRectangle(br, ix, iy, Width, Height);

                // Tile the wood sprite across the platform surface
                for (int tx = ix; tx < ix + Width; tx += 18)
                {
                    for (int ty = iy; ty < iy + Height; ty += 18)
                    {
                        int dw = Math.Min(18, ix + Width - tx);
                        int dh = Math.Min(18, iy + Height - ty);
                        g.DrawImage(woodTile, tx, ty, dw, dh);
                    }
                }

                // Top highlight strip
                using (var br = new SolidBrush(Color.FromArgb(80, 255, 255, 200)))
                    g.FillRectangle(br, ix, iy, Width, 3);
            }
            else
            {
                // Fallback: GDI SMB3 brown brick
                using (var br = new SolidBrush(Color.FromArgb(160, 100, 50)))
                    g.FillRectangle(br, ix, iy, Width, Height);
                using (var br = new SolidBrush(Color.FromArgb(200, 130, 70)))
                    g.FillRectangle(br, ix, iy, Width, 4);
                using (var br = new SolidBrush(Color.FromArgb(110, 60, 28)))
                    g.FillRectangle(br, ix, iy + Height - 3, Width, 3);
                using (var pen = new Pen(Color.FromArgb(100, 70, 30), 1))
                    for (int bx = ix; bx < ix + Width; bx += 32)
                        g.DrawLine(pen, bx, iy + 4, bx, iy + Height - 3);
            }

            // ── Movement indicator arrow ──────────────────────────────────────
            DrawDirectionArrow(g, ix, iy);
        }

        private void DrawDirectionArrow(Graphics g, int ix, int iy)
        {
            using (var br = new SolidBrush(Color.FromArgb(180, Color.White)))
            {
                int cx = ix + Width / 2;
                int cy = iy + 5;

                if (IsMovingHorizontal)
                {
                    // Left-right arrows.
                    g.FillRectangle(br, cx - 8, cy, 16, 3);
                    g.FillPolygon(br, new[] { new Point(cx - 12, cy + 1), new Point(cx - 8, cy - 2), new Point(cx - 8, cy + 4) });
                    g.FillPolygon(br, new[] { new Point(cx + 12, cy + 1), new Point(cx + 8, cy - 2), new Point(cx + 8, cy + 4) });
                }
                else
                {
                    // Up-down arrows.
                    g.FillRectangle(br, cx - 1, cy - 2, 3, 10);
                    g.FillPolygon(br, new[] { new Point(cx, cy - 4), new Point(cx - 3, cy - 1), new Point(cx + 3, cy - 1) });
                    g.FillPolygon(br, new[] { new Point(cx, cy + 8), new Point(cx - 3, cy + 5), new Point(cx + 3, cy + 5) });
                }
            }
        }
    }
}
