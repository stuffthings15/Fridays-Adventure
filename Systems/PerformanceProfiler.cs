using System;
using System.Diagnostics;
using System.Drawing;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Per-frame performance profiler.
    ///
    /// Measures real elapsed milliseconds for each Update and Render pass so
    /// the technical team can spot frame-budget overruns without an external
    /// profiler attached.
    ///
    /// Team 2  (Producer)         — Idea 2: frame time display in DevMenu.
    /// Team 3  (Technical Lead)   — Idea 4: performance profiler (update vs. render time).
    /// Team 10 (Engine Programmer) — Idea 10: render pipeline profiling markers.
    ///
    /// ── Usage ────────────────────────────────────────────────────────────────
    ///   Call BeginUpdate() / EndUpdate() around Game.OnTick logic.
    ///   Call BeginRender() / EndRender() around Game.OnRender logic.
    ///   Call DrawOverlay(g, W, H) to show a compact HUD (visible in GodMode).
    /// </summary>
    public static class PerformanceProfiler
    {
        // ── Stopwatches ────────────────────────────────────────────────────────
        private static readonly Stopwatch _updateSw = new Stopwatch();
        private static readonly Stopwatch _renderSw = new Stopwatch();
        private static readonly Stopwatch _frameSw  = Stopwatch.StartNew();

        // ── Smoothed results (exponential moving average) ─────────────────────
        private const float Alpha = 0.15f;   // smoothing factor

        /// <summary>Smoothed total frame time in milliseconds.</summary>
        public static float LastFrameMs  { get; private set; }

        /// <summary>Smoothed update pass time in milliseconds.</summary>
        public static float LastUpdateMs { get; private set; }

        /// <summary>Smoothed render pass time in milliseconds.</summary>
        public static float LastRenderMs { get; private set; }

        // ── Frame tracking ─────────────────────────────────────────────────────
        private static float _rawFrame;

        // ── Profile markers ────────────────────────────────────────────────────

        /// <summary>Call at the start of the game update tick.</summary>
        public static void BeginUpdate()
        {
            // Capture total frame time from the previous tick.
            _rawFrame = (float)_frameSw.Elapsed.TotalMilliseconds;
            _frameSw.Restart();
            LastFrameMs  = LastFrameMs  * (1f - Alpha) + _rawFrame * Alpha;
            _updateSw.Restart();
        }

        /// <summary>Call at the end of the game update tick.</summary>
        public static void EndUpdate()
        {
            _updateSw.Stop();
            float ms = (float)_updateSw.Elapsed.TotalMilliseconds;
            LastUpdateMs = LastUpdateMs * (1f - Alpha) + ms * Alpha;
        }

        /// <summary>Call at the start of the render callback.</summary>
        public static void BeginRender() => _renderSw.Restart();

        /// <summary>Call at the end of the render callback.</summary>
        public static void EndRender()
        {
            _renderSw.Stop();
            float ms = (float)_renderSw.Elapsed.TotalMilliseconds;
            LastRenderMs = LastRenderMs * (1f - Alpha) + ms * Alpha;
        }

        // ── Overlay draw ───────────────────────────────────────────────────────

        /// <summary>
        /// Draws a compact performance HUD in the top-left corner.
        /// Only visible when GodMode is active (non-intrusive for players).
        /// </summary>
        public static void DrawOverlay(Graphics g, int W, int H)
        {
            if (!Game.Instance.GodMode) return;

            using (var br = new SolidBrush(Color.FromArgb(180, 4, 4, 12)))
                g.FillRectangle(br, 2, 2, 200, 52);

            using (var f = new Font("Courier New", 8, FontStyle.Bold))
            {
                // Frame rate estimate.
                float fps = LastFrameMs > 0f ? 1000f / LastFrameMs : 0f;
                Color fpsColor = fps >= 55f ? Color.LimeGreen
                               : fps >= 30f ? Color.Yellow
                               :              Color.OrangeRed;

                using (var fBr = new SolidBrush(fpsColor))
                    g.DrawString($"FPS: {fps:F0}  Frame: {LastFrameMs:F1}ms", f, fBr, 6, 5);

                g.DrawString($"Upd: {LastUpdateMs:F2}ms  Rnd: {LastRenderMs:F2}ms", f, Brushes.LightGray, 6, 19);
                g.DrawString($"Scenes: {Game.Instance.Scenes.Depth}", f, Brushes.DimGray, 6, 33);
            }
        }
    }
}
