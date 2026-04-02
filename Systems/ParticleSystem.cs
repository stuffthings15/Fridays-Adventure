using System;
using System.Collections.Generic;
using System.Drawing;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Burst particle effect system for SMB3-style hit sparks, coin collects,
    /// confetti, explosions, and ability VFX.
    ///
    /// Particles are pooled in a fixed-size array (no heap allocations per frame).
    /// Scenes call the static <c>Spawn*</c> methods; the system self-updates.
    ///
    /// Team 17 (VFX Artist)    — Idea 1: coin sparkle burst.
    /// Team 17 (VFX Artist)    — Idea 2: enemy defeat explosion.
    /// Team 17 (VFX Artist)    — Idea 3: level complete confetti.
    /// Team 17 (VFX Artist)    — Idea 5: ice crystal particles for FlashFreeze.
    /// Team 17 (VFX Artist)    — Idea 6: star trail for invincibility.
    /// Team 17 (VFX Artist)    — Idea 7: boss phase-change smoke burst.
    /// Team 17 (VFX Artist)    — Idea 8: water splash on entering water hazard.
    /// Team 16 (2D Animator)   — Idea 4: coin spin animation (via floating text).
    /// Team 15 (UI/UX Artist)  — Idea 5: score pop-up float-up.
    /// </summary>
    public static class ParticleSystem
    {
        // ── Particle ─────────────────────────────────────────────────────────
        private struct Particle
        {
            public float X, Y;       // world position
            public float VX, VY;     // velocity
            public float Life;       // remaining lifetime [0→1]
            public float Decay;      // life consumed per second
            public float Size;       // initial radius
            public Color Color;      // base color
            public bool  Glow;       // draws a larger soft outer disc
        }

        // ── Pool ───────────────────────────────────────────────────────────────
        private const int MaxParticles = 400;
        private static readonly Particle[] _pool = new Particle[MaxParticles];
        private static int _count;

        // ── Gravity constant ──────────────────────────────────────────────────
        private const float Gravity = 280f;

        private static readonly Random _rng = new Random();

        // ── Public Spawn API ──────────────────────────────────────────────────

        /// <summary>
        /// Berry/coin collection sparkle burst — yellow-gold radial particles.
        /// Team 17 (VFX Artist) — Idea 1.
        /// </summary>
        public static void SpawnCoinSparkle(float x, float y)
        {
            SpawnBurst(x, y, 8, Color.Gold, 60f, 140f, 1.0f, 1.8f, glow: true);
            SpawnBurst(x, y, 4, Color.Yellow, 30f, 80f, 0.6f, 1.2f, glow: false);
        }

        /// <summary>
        /// Enemy defeat explosion — orange-red burst.
        /// Team 17 (VFX Artist) — Idea 2.
        /// </summary>
        public static void SpawnEnemyDefeat(float x, float y)
        {
            SpawnBurst(x, y, 12, Color.OrangeRed, 80f, 220f, 1.2f, 2.4f, glow: true);
            SpawnBurst(x, y, 6,  Color.Orange,    40f, 120f, 0.8f, 1.6f, glow: false);
        }

        /// <summary>
        /// Level clear confetti — multi-colour upward burst.
        /// Team 17 (VFX Artist) — Idea 3.
        /// </summary>
        public static void SpawnConfetti(float x, float y, int count = 40)
        {
            Color[] confettiColors = { Color.Gold, Color.LimeGreen, Color.Cyan, Color.HotPink, Color.Yellow };
            for (int i = 0; i < count && _count < MaxParticles; i++)
            {
                float angle = (float)(_rng.NextDouble() * Math.PI * 2);
                float speed = 80f + (float)_rng.NextDouble() * 200f;
                int   ci    = _rng.Next(confettiColors.Length);
                Spawn(new Particle
                {
                    X     = x,
                    Y     = y,
                    VX    = (float)Math.Cos(angle) * speed,
                    VY    = (float)Math.Sin(angle) * speed - 120f,
                    Life  = 1.0f,
                    Decay = 0.6f + (float)_rng.NextDouble() * 0.4f,
                    Size  = 4f  + (float)_rng.NextDouble() * 6f,
                    Color = confettiColors[ci],
                    Glow  = false
                });
            }
        }

        /// <summary>
        /// Ice crystal burst when FlashFreeze activates.
        /// Team 17 (VFX Artist) — Idea 5.
        /// </summary>
        public static void SpawnIceCrystals(float x, float y)
        {
            SpawnBurst(x, y, 10, Color.LightCyan,    60f, 160f, 1.0f, 2.0f, glow: true);
            SpawnBurst(x, y, 6,  Color.DeepSkyBlue, 30f, 100f, 0.6f, 1.4f, glow: false);
        }

        /// <summary>
        /// Star trail for invincibility — small gold stars.
        /// Team 17 (VFX Artist) — Idea 6.
        /// </summary>
        public static void SpawnStarTrail(float x, float y)
        {
            // Two gold particles drifting upward/sideways — called every few frames.
            for (int i = 0; i < 2 && _count < MaxParticles; i++)
            {
                Spawn(new Particle
                {
                    X     = x + (float)(_rng.NextDouble() - 0.5) * 20,
                    Y     = y + (float)(_rng.NextDouble() - 0.5) * 20,
                    VX    = (float)(_rng.NextDouble() - 0.5) * 60,
                    VY    = -60f - (float)_rng.NextDouble() * 80,
                    Life  = 1.0f,
                    Decay = 2.5f,
                    Size  = 5f,
                    Color = Color.Gold,
                    Glow  = true
                });
            }
        }

        /// <summary>
        /// Boss phase-change smoke burst — grey expanding cloud.
        /// Team 17 (VFX Artist) — Idea 7.
        /// </summary>
        public static void SpawnSmokeBurst(float x, float y)
        {
            SpawnBurst(x, y, 16, Color.FromArgb(180, 140, 140, 140), 30f, 100f, 1.4f, 1.8f, glow: true);
        }

        /// <summary>
        /// Water splash on entering a water hazard.
        /// Team 17 (VFX Artist) — Idea 8.
        /// </summary>
        public static void SpawnWaterSplash(float x, float y)
        {
            SpawnBurst(x, y, 10, Color.FromArgb(200, 100, 180, 255), 60f, 140f, 0.8f, 1.4f, glow: false);
            SpawnBurst(x, y, 4,  Color.White,                         20f,  60f, 0.4f, 0.8f, glow: false);
        }

        // ── Update ─────────────────────────────────────────────────────────────

        /// <summary>Advances all particles. Called by Game.OnTick.</summary>
        public static void Update(float dt)
        {
            for (int i = _count - 1; i >= 0; i--)
            {
                ref Particle p = ref _pool[i];
                p.X    += p.VX * dt;
                p.Y    += p.VY * dt;
                p.VY   += Gravity * dt;
                p.Life -= p.Decay * dt;
                if (p.Life <= 0f)
                    _pool[i] = _pool[--_count];
            }
        }

        // ── Draw ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws all live particles.  Call from Game.OnRender after scene draw.
        /// Pass cameraX=0 if particles use screen space.
        /// </summary>
        public static void Draw(Graphics g, float cameraX = 0f)
        {
            for (int i = 0; i < _count; i++)
            {
                ref Particle p = ref _pool[i];
                float sx   = p.X - cameraX;
                float sy   = p.Y;
                float size = p.Size * p.Life;
                if (size < 0.5f) continue;

                // Optional soft glow (larger translucent disc).
                if (p.Glow)
                {
                    float gs = size * 2.4f;
                    using (var br = new SolidBrush(Color.FromArgb(
                        (int)(40 * p.Life), p.Color.R, p.Color.G, p.Color.B)))
                        g.FillEllipse(br, sx - gs / 2, sy - gs / 2, gs, gs);
                }

                // Core disc.
                using (var br = new SolidBrush(Color.FromArgb(
                    (int)(220 * p.Life), p.Color.R, p.Color.G, p.Color.B)))
                    g.FillEllipse(br, sx - size / 2, sy - size / 2, size, size);
            }
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private static void SpawnBurst(float x, float y, int count,
            Color color, float minSpeed, float maxSpeed,
            float minDecay, float maxDecay, bool glow)
        {
            for (int i = 0; i < count && _count < MaxParticles; i++)
            {
                float angle = (float)(_rng.NextDouble() * Math.PI * 2);
                float speed = minSpeed + (float)_rng.NextDouble() * (maxSpeed - minSpeed);
                Spawn(new Particle
                {
                    X     = x,
                    Y     = y,
                    VX    = (float)Math.Cos(angle) * speed,
                    VY    = (float)Math.Sin(angle) * speed,
                    Life  = 1.0f,
                    Decay = minDecay + (float)_rng.NextDouble() * (maxDecay - minDecay),
                    Size  = 4f + (float)_rng.NextDouble() * 6f,
                    Color = color,
                    Glow  = glow
                });
            }
        }

        private static void Spawn(Particle p)
        {
            if (_count < MaxParticles)
                _pool[_count++] = p;
        }
    }
}
