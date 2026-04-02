using System;
using System.Collections.Generic;
using System.Drawing;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Multi-layer parallax background renderer — pure GDI+ code-drawn layers.
    ///
    /// Team 12 (Art Director)            — consistent visual depth across all levels.
    /// Team 14 (Environment Artist)      — mountain silhouettes, cloud layers, sky bands.
    /// Team 10 (Engine Optimizer)        — offscreen layer culling, scroll offset caching.
    ///
    /// Usage:
    ///   var bg = new ParallaxBackground();
    ///   bg.AddLayer(new SkyLayer());
    ///   bg.AddLayer(new MountainLayer());
    ///   bg.AddLayer(new CloudLayer());
    ///
    ///   // Each Update tick:
    ///   bg.Scroll(cameraX, cameraY);
    ///
    ///   // Each Draw:
    ///   bg.Draw(g, W, H);
    /// </summary>
    public sealed class ParallaxBackground
    {
        // ── Layer abstraction ─────────────────────────────────────────────────
        public interface IParallaxLayer
        {
            /// <summary>Parallax coefficient: 0=fixed, 1=moves 1:1 with camera.</summary>
            float SpeedX { get; }
            float SpeedY { get; }
            void Draw(Graphics g, int W, int H, float offsetX, float offsetY);
        }

        // ── Registry ──────────────────────────────────────────────────────────
        private readonly List<IParallaxLayer> _layers = new List<IParallaxLayer>();
        private float _cameraX, _cameraY;

        public void AddLayer(IParallaxLayer layer)    { if (layer != null) _layers.Add(layer); }
        public void RemoveLayer(IParallaxLayer layer) { _layers.Remove(layer); }
        public void ClearLayers()                     { _layers.Clear(); }

        /// <summary>Updates camera position for offset calculations.</summary>
        public void Scroll(float cameraX, float cameraY)
        {
            _cameraX = cameraX;
            _cameraY = cameraY;
        }

        /// <summary>Draws all layers back to front.</summary>
        public void Draw(Graphics g, int W, int H)
        {
            foreach (var layer in _layers)
                layer.Draw(g, W, H, _cameraX * layer.SpeedX, _cameraY * layer.SpeedY);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Built-in code-drawn layers (SMB3-style)
        // ─────────────────────────────────────────────────────────────────────

        // ── Sky gradient layer ────────────────────────────────────────────────
        /// <summary>
        /// Fills the canvas with an SMB3-style day/dusk sky gradient.
        /// Speed=0: fixed background.
        /// </summary>
        public sealed class SkyLayer : IParallaxLayer
        {
            public float SpeedX => 0f;
            public float SpeedY => 0f;

            public Color TopColor    { get; set; } = Color.FromArgb(72, 140, 230);
            public Color BottomColor { get; set; } = Color.FromArgb(185, 220, 255);

            public void Draw(Graphics g, int W, int H, float ox, float oy)
            {
                using (var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, W, H), TopColor, BottomColor, 90f))
                    g.FillRectangle(br, 0, 0, W, H);
            }
        }

        // ── Distant mountain silhouette layer ─────────────────────────────────
        /// <summary>
        /// Draws two rows of overlapping mountain silhouettes (dark blue / purple haze).
        /// Speed=0.1: barely moves — creates deep-parallax depth.
        /// </summary>
        public sealed class MountainLayer : IParallaxLayer
        {
            public float SpeedX => 0.10f;
            public float SpeedY => 0f;

            public void Draw(Graphics g, int W, int H, float ox, float oy)
            {
                // Far mountains (darker, bluish)
                DrawRow(g, W, H, (int)(-ox * 0.5f), H - 200, 90, 160, Color.FromArgb(80, 80, 130));
                // Near mountains (lighter, wider peaks)
                DrawRow(g, W, H, (int)(-ox),         H - 140, 70, 110, Color.FromArgb(100, 100, 160));
            }

            private static void DrawRow(Graphics g, int W, int H, int scrollOff,
                                        int baseY, int minH, int maxH, Color color)
            {
                // Deterministic mountain peaks using a fixed-seed pattern.
                int[] heights = { 80, 130, 90, 160, 70, 140, 100, 120, 85, 150, 75, 135, 95, 145, 65 };
                int   step    = W / 8;
                using (var br = new SolidBrush(color))
                {
                    for (int i = -1; i < 12; i++)
                    {
                        int h  = minH + heights[Math.Abs(i) % heights.Length] * (maxH - minH) / 160;
                        int bx = i * step + scrollOff % (step * 2);
                        // Draw a triangle peak.
                        var pts = new Point[]
                        {
                            new Point(bx,               baseY),
                            new Point(bx + step / 2,    baseY - h),
                            new Point(bx + step,        baseY)
                        };
                        g.FillPolygon(br, pts);
                    }
                }
                // Fill below mountains to horizon.
                using (var br = new SolidBrush(color))
                    g.FillRectangle(br, 0, baseY, W, H - baseY);
            }
        }

        // ── Cloud layer ───────────────────────────────────────────────────────
        /// <summary>
        /// Scrolling SMB3-style fluffy clouds.
        /// Speed=0.25: mid-depth, noticeable but slow.
        /// </summary>
        public sealed class CloudLayer : IParallaxLayer
        {
            public float SpeedX => 0.25f;
            public float SpeedY => 0f;

            // Deterministic cloud positions (no runtime RNG needed).
            private static readonly (int rx, int ry, int rw, int rh)[] _clouds =
            {
                (50,  60,  110, 42), (230, 40, 90,  34), (410, 80, 130, 48),
                (600, 50,  100, 36), (760, 70, 120, 44), (900, 35, 95,  32),
            };

            public void Draw(Graphics g, int W, int H, float ox, float oy)
            {
                foreach (var c in _clouds)
                {
                    // Wrap x so clouds tile infinitely.
                    int cx = ((c.rx - (int)ox) % (W + 200) + W + 200) % (W + 200) - 100;
                    DrawCloud(g, cx, c.ry, c.rw, c.rh);
                }
            }

            private static void DrawCloud(Graphics g, int cx, int cy, int cw, int ch)
            {
                // SMB3-style: two overlapping ellipses make a simple cloud puff.
                using (var br = new SolidBrush(Color.FromArgb(220, Color.White)))
                {
                    g.FillEllipse(br, cx,            cy + ch / 4, cw,         ch * 3 / 4);
                    g.FillEllipse(br, cx + cw / 5,   cy,          cw * 3 / 5, ch);
                    g.FillEllipse(br, cx + cw * 2/3, cy + ch / 5, cw * 2 / 5, ch * 3 / 4);
                }
            }
        }

        // ── Star field layer (night / storm scenes) ───────────────────────────
        /// <summary>
        /// Fixed star field for night/boss backgrounds. Speed=0: pinned to sky.
        /// </summary>
        public sealed class StarLayer : IParallaxLayer
        {
            public float SpeedX => 0f;
            public float SpeedY => 0f;

            private static readonly (float fx, float fy, float size)[] _stars;

            static StarLayer()
            {
                // Generate once using deterministic pseudo-random.
                var r = new Random(42);
                _stars = new (float, float, float)[120];
                for (int i = 0; i < _stars.Length; i++)
                    _stars[i] = ((float)r.NextDouble(), (float)r.NextDouble() * 0.65f,
                                 0.5f + (float)r.NextDouble() * 1.5f);
            }

            public void Draw(Graphics g, int W, int H, float ox, float oy)
            {
                foreach (var s in _stars)
                {
                    using (var br = new SolidBrush(Color.FromArgb(200, Color.White)))
                        g.FillEllipse(br, s.fx * W, s.fy * H, s.size, s.size);
                }
            }
        }
    }
}
