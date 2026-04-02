using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Dynamic weather and environmental effect renderer.
    ///
    /// Supports multiple weather modes that overlay particle systems on top of
    /// gameplay scenes to reinforce the level's visual atmosphere.
    ///
    /// Team 12 (Art Director)        — Idea 6: water surface shimmer.
    /// Team 14 (Environment Artist)  — Idea 4: underwater caustics animation.
    /// Team 14 (Environment Artist)  — Idea 5: sky cloud parallax.
    /// Team 14 (Environment Artist)  — Idea 6: lava background.
    /// Team 14 (Environment Artist)  — Idea 7: snow particle overlay for tundra.
    /// Team 14 (Environment Artist)  — Idea 8: star field for night levels.
    /// Team 17 (VFX Artist)          — Idea 9: lightning strike flash (storm).
    /// </summary>
    public static class WeatherSystem
    {
        // ── Weather mode ───────────────────────────────────────────────────────
        public enum Mode
        {
            None,
            Rain,        // downward rain streaks
            Snow,        // tundra peak snowfall
            Lightning,   // storm belt lightning flashes + rain
            Underwater,  // caustic light beams + bubble drift
            StarField,   // night sky background stars
            Embers,      // volcanic embers drifting upward
        }

        public static Mode Current { get; private set; } = Mode.None;

        // ── Particle pool ─────────────────────────────────────────────────────
        private struct Particle
        {
            public float X, Y, VX, VY;
            public float Life;       // [0–1] where 0 = dead
            public float Size;
            public Color Color;
        }

        private static readonly Particle[] _particles = new Particle[200];
        private static int _pCount;

        // ── Lightning state ────────────────────────────────────────────────────
        private static float _lightningFlash;   // > 0 = full-screen white flash
        private static float _lightningTimer;   // countdown to next strike

        // ── Caustic light beams (underwater) ──────────────────────────────────
        private static float _causticAnim;

        // ── Global animation timer ─────────────────────────────────────────────
        private static float _time;

        private static readonly Random _rng = new Random();

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Sets the active weather mode and resets all particles.</summary>
        public static void Set(Mode mode)
        {
            Current    = mode;
            _pCount    = 0;
            _time      = 0f;
            _lightningTimer = 2.0f + (float)_rng.NextDouble() * 3.0f;
        }

        // ── Update ─────────────────────────────────────────────────────────────

        /// <summary>Advances particle simulation. Call once per game tick.</summary>
        public static void Update(float dt)
        {
            if (Current == Mode.None) return;
            _time += dt;

            // Advance particle positions and lifetimes.
            for (int i = 0; i < _pCount; i++)
            {
                ref Particle p = ref _particles[i];
                p.X    += p.VX * dt;
                p.Y    += p.VY * dt;
                p.Life -= dt * 0.5f;
            }

            // Remove dead particles (swap-remove for cache friendliness).
            for (int i = _pCount - 1; i >= 0; i--)
            {
                if (_particles[i].Life <= 0f)
                { _particles[i] = _particles[--_pCount]; }
            }

            // Spawn new particles.
            SpawnParticles(dt);

            // Lightning update.
            if (Current == Mode.Lightning)
            {
                if (_lightningFlash > 0f) _lightningFlash -= dt * 8f;
                _lightningTimer -= dt;
                if (_lightningTimer <= 0f)
                {
                    _lightningFlash = 1.0f;
                    _lightningTimer = 3.0f + (float)_rng.NextDouble() * 5.0f;
                }
            }

            // Caustic animation.
            _causticAnim += dt * 0.8f;
        }

        // ── Particle spawners ──────────────────────────────────────────────────

        private static void SpawnParticles(float dt)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            switch (Current)
            {
                case Mode.Rain:
                case Mode.Lightning:
                    // Rain: 8 streaks per frame.
                    for (int i = 0; i < 8 && _pCount < _particles.Length; i++)
                        Spawn(new Particle
                        {
                            X     = (float)_rng.NextDouble() * W,
                            Y     = -10f,
                            VX    = -20f,
                            VY    = 500f + (float)_rng.NextDouble() * 200f,
                            Life  = 1.0f,
                            Size  = 2f,
                            Color = Color.FromArgb(120, 150, 200, 255)
                        });
                    break;

                case Mode.Snow:
                    // Snow: 3 large flakes per frame.
                    for (int i = 0; i < 3 && _pCount < _particles.Length; i++)
                        Spawn(new Particle
                        {
                            X     = (float)_rng.NextDouble() * W,
                            Y     = -10f,
                            VX    = -15f + (float)(_rng.NextDouble() - 0.5) * 30f,
                            VY    = 60f  + (float)_rng.NextDouble() * 60f,
                            Life  = 1.2f,
                            Size  = 4f + (float)_rng.NextDouble() * 6f,
                            Color = Color.FromArgb(200, 230, 240, 255)
                        });
                    break;

                case Mode.Embers:
                    // Embers: small orange sparks drifting up.
                    for (int i = 0; i < 4 && _pCount < _particles.Length; i++)
                        Spawn(new Particle
                        {
                            X     = (float)_rng.NextDouble() * W,
                            Y     = H + 10f,
                            VX    = -10f + (float)(_rng.NextDouble() - 0.5) * 40f,
                            VY    = -(100f + (float)_rng.NextDouble() * 180f),
                            Life  = 1.5f,
                            Size  = 3f + (float)_rng.NextDouble() * 4f,
                            Color = Color.FromArgb(200, 255, 100, 20)
                        });
                    break;

                case Mode.Underwater:
                    // Bubbles drifting upward.
                    for (int i = 0; i < 2 && _pCount < _particles.Length; i++)
                        Spawn(new Particle
                        {
                            X     = (float)_rng.NextDouble() * W,
                            Y     = H + 10f,
                            VX    = -10f + (float)(_rng.NextDouble() - 0.5) * 20f,
                            VY    = -(40f + (float)_rng.NextDouble() * 80f),
                            Life  = 2.0f,
                            Size  = 4f + (float)_rng.NextDouble() * 8f,
                            Color = Color.FromArgb(80, 180, 220, 255)
                        });
                    break;
            }
        }

        private static void Spawn(Particle p)
        {
            if (_pCount < _particles.Length)
                _particles[_pCount++] = p;
        }

        // ── Draw ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws weather particles and special effects over the scene.
        /// Call from Game.OnRender BEFORE the debug overlays.
        /// </summary>
        public static void Draw(Graphics g, int W, int H)
        {
            if (Current == Mode.None) return;

            // Draw underwater caustic overlay.
            if (Current == Mode.Underwater)
                DrawUnderwaterTint(g, W, H);

            // Draw star field.
            if (Current == Mode.StarField)
                DrawStarField(g, W, H);

            // Draw particles.
            for (int i = 0; i < _pCount; i++)
            {
                ref Particle p = ref _particles[i];
                float alpha = p.Life;
                using (var br = new SolidBrush(Color.FromArgb(
                    (int)(p.Color.A * alpha), p.Color.R, p.Color.G, p.Color.B)))
                {
                    if (Current == Mode.Rain || Current == Mode.Lightning)
                        // Rain: draw as a short line streak.
                        g.DrawLine(new Pen(br, 1), p.X, p.Y, p.X - 4, p.Y - 14);
                    else
                        g.FillEllipse(br, p.X - p.Size / 2, p.Y - p.Size / 2, p.Size, p.Size);
                }
            }

            // Lightning flash (full-screen white-blue overlay).
            if (Current == Mode.Lightning && _lightningFlash > 0f)
            {
                using (var br = new SolidBrush(Color.FromArgb(
                    (int)(160 * _lightningFlash), 200, 220, 255)))
                    g.FillRectangle(br, 0, 0, W, H);
            }
        }

        // ── Environment draw helpers ───────────────────────────────────────────

        /// <summary>Draws a subtle cyan tint with animated caustic light shafts (underwater).</summary>
        private static void DrawUnderwaterTint(Graphics g, int W, int H)
        {
            // Blue-green tint.
            using (var br = new SolidBrush(Color.FromArgb(40, 0, 80, 120)))
                g.FillRectangle(br, 0, 0, W, H);

            // Animated light shafts (wavy diagonal lines).
            using (var pen = new Pen(Color.FromArgb(18, 180, 220, 255), 14))
            {
                for (int i = 0; i < 5; i++)
                {
                    float ox = (float)(Math.Sin(_causticAnim + i * 1.2) * 40);
                    int   sx = i * (W / 4) + (int)ox;
                    g.DrawLine(pen, sx, 0, sx + 60, H);
                }
            }
        }

        /// <summary>Draws a simple deterministic star field for night-level backgrounds.</summary>
        private static void DrawStarField(Graphics g, int W, int H)
        {
            for (int i = 0; i < 80; i++)
            {
                int sx = (i * 97  + 17) % (W - 4);
                int sy = (i * 53  + 31) % (H - 4);
                // Twinkling alpha driven by animation time.
                float twinkle = 0.5f + 0.5f * (float)Math.Sin(_time * 2.3 + i * 0.8);
                using (var br = new SolidBrush(Color.FromArgb((int)(160 * twinkle), Color.White)))
                    g.FillEllipse(br, sx, sy, 2, 2);
            }
        }
    }
}
