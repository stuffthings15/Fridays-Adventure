using System;
using System.Collections.Generic;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Simple grid-based tilemap renderer — draws a flat tile grid in SMB3-style
    /// blocky appearance for floors, ceilings, and platforms.
    ///
    /// Tiles are defined by a 2D array of <see cref="TileType"/>.  The renderer
    /// draws each cell at a fixed pixel size and supports camera scrolling.
    ///
    /// Team 8  (Systems Programmer)  — Idea 3: tilemap renderer tool.
    /// Team 12 (Art Director)        — Idea 2: SMB3-style blocky tileset colors.
    /// Team 14 (Environment Artist)  — Idea 1: tile-based ground top surface.
    /// Team 10 (Engine Programmer)   — Idea 2: batch draw calls grouped by type.
    /// </summary>
    public static class TileMapRenderer
    {
        // ── Tile types ──────────────────────────────────────────────────────────
        public enum TileType
        {
            Empty     = 0,
            Ground    = 1,   // SMB3 dirt-brown solid block
            BrickTop  = 2,   // SMB3 breakable brick (orange-tan)
            Stone     = 3,   // harder stone wall (gray)
            Ice       = 4,   // ice/tundra tile (light blue)
            Sand      = 5,   // beach/harbor tile (sandy yellow)
            Jungle    = 6,   // dino island foliage-green tile
            Metal     = 7,   // armored tile (dark steel)
        }

        /// <summary>Pixel size of each tile cell (SMB3 = 16px; scaled here to 32px).</summary>
        public const int TileSize = 32;

        // ── Color palettes (SMB3-inspired) ─────────────────────────────────────

        private static Color TileTopColor(TileType t)
        {
            switch (t)
            {
                case TileType.Ground:   return Color.FromArgb(100, 180, 60);   // grass green top
                case TileType.BrickTop: return Color.FromArgb(200, 120, 40);   // brick orange top
                case TileType.Stone:    return Color.FromArgb(140, 140, 150);  // stone gray
                case TileType.Ice:      return Color.FromArgb(160, 220, 255);  // icy light blue
                case TileType.Sand:     return Color.FromArgb(230, 210, 120);  // sandy yellow
                case TileType.Jungle:   return Color.FromArgb(50, 160, 50);    // lush green
                case TileType.Metal:    return Color.FromArgb(80, 100, 120);   // dark metal
                default:                return Color.Transparent;
            }
        }

        private static Color TileBodyColor(TileType t)
        {
            switch (t)
            {
                case TileType.Ground:   return Color.FromArgb(140, 90, 40);    // dirt brown
                case TileType.BrickTop: return Color.FromArgb(180, 90, 30);    // brick body
                case TileType.Stone:    return Color.FromArgb(100, 100, 110);  // stone body
                case TileType.Ice:      return Color.FromArgb(100, 180, 230);  // ice body
                case TileType.Sand:     return Color.FromArgb(200, 180, 90);   // sand body
                case TileType.Jungle:   return Color.FromArgb(40, 110, 30);    // jungle body
                case TileType.Metal:    return Color.FromArgb(50, 60, 80);     // metal body
                default:                return Color.Transparent;
            }
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws the tile grid clipped to the visible camera window.
        /// </summary>
        /// <param name="g">Graphics context.</param>
        /// <param name="tiles">2D grid [row, col] of tile types.</param>
        /// <param name="cameraX">Horizontal camera scroll offset in world pixels.</param>
        /// <param name="screenW">Screen (canvas) width.</param>
        /// <param name="screenH">Screen (canvas) height.</param>
        public static void Draw(Graphics g, TileType[,] tiles, float cameraX, int screenW, int screenH)
        {
            if (tiles == null) return;

            int rows = tiles.GetLength(0);
            int cols = tiles.GetLength(1);

            // Determine the range of columns visible on screen.
            int colStart = Math.Max(0, (int)(cameraX / TileSize) - 1);
            int colEnd   = Math.Min(cols - 1, colStart + screenW / TileSize + 2);

            for (int row = 0; row < rows; row++)
            {
                for (int col = colStart; col <= colEnd; col++)
                {
                    TileType t = tiles[row, col];
                    if (t == TileType.Empty) continue;

                    int px = col * TileSize - (int)cameraX;
                    int py = row * TileSize;

                    DrawTile(g, px, py, t);
                }
            }
        }

        // ── Single tile draw ───────────────────────────────────────────────────

        /// <summary>Draws one tile at screen coordinates (px, py).</summary>
        private static void DrawTile(Graphics g, int px, int py, TileType t)
        {
            Color top  = TileTopColor(t);
            Color body = TileBodyColor(t);

            // Body fill.
            using (var br = new SolidBrush(body))
                g.FillRectangle(br, px, py + 5, TileSize, TileSize - 5);

            // Top strip (grass/surface layer).
            using (var br = new SolidBrush(top))
                g.FillRectangle(br, px, py, TileSize, 6);

            // Bright highlight on top-left edge (SMB3 bevel).
            using (var br = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
                g.FillRectangle(br, px, py, TileSize, 2);
            using (var br = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                g.FillRectangle(br, px, py, 2, TileSize);

            // Dark shadow on bottom-right edge.
            using (var br = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                g.FillRectangle(br, px, py + TileSize - 2, TileSize, 2);
            using (var br = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                g.FillRectangle(br, px + TileSize - 2, py, 2, TileSize);

            // Brick pattern for brick tiles.
            if (t == TileType.BrickTop)
            {
                using (var pen = new Pen(Color.FromArgb(80, 100, 50, 10), 1))
                {
                    // Horizontal mortar lines.
                    g.DrawLine(pen, px, py + TileSize / 2, px + TileSize, py + TileSize / 2);
                    // Vertical mortar (offset on alternating rows rows).
                    int offset = (py / TileSize % 2 == 0) ? 0 : TileSize / 2;
                    for (int bx = offset; bx < TileSize; bx += TileSize)
                        g.DrawLine(pen, px + bx, py, px + bx, py + TileSize / 2);
                }
            }
        }
    }
}
