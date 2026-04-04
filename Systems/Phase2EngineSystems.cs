// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 10: Engine Programmer
// Feature: Engine Systems Pack
// Purpose: Implements Phase 2 engine-side runtime systems and diagnostics.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Level streaming lane tracker.
    /// </summary>
    /// <remarks>PHASE 2 - Team 10: Level Streaming System</remarks>
    public static class LevelStreamingSystem
    {
        private static readonly HashSet<string> _loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Marks a level chunk as loaded.</summary>
        /// <remarks>PHASE 2 - Team 10: Level Streaming System</remarks>
        public static void LoadChunk(string chunkId)
        {
            if (!string.IsNullOrWhiteSpace(chunkId)) _loaded.Add(chunkId);
        }

        /// <summary>Returns loaded chunk identifiers.</summary>
        /// <remarks>PHASE 2 - Team 10: Level Streaming System</remarks>
        public static IReadOnlyList<string> Loaded() => _loaded.OrderBy(x => x).ToList();
    }

    /// <summary>
    /// Particle effect pooling using reusable reference entries.
    /// </summary>
    /// <remarks>PHASE 2 - Team 10: Particle Effect Pooling</remarks>
    public static class ParticleEffectPoolingSystem
    {
        /// <summary>Pooled particle packet model.</summary>
        public sealed class ParticlePacket
        {
            /// <summary>X coordinate.</summary>
            public float X;
            /// <summary>Y coordinate.</summary>
            public float Y;
            /// <summary>Lifetime seconds.</summary>
            public float Lifetime;
        }

        private static readonly ObjectPool<ParticlePacket> _pool =
            new ObjectPool<ParticlePacket>(
                factory: () => new ParticlePacket(),
                initialSize: 32,
                onGet: p => { p.X = 0; p.Y = 0; p.Lifetime = 0; },
                onReturn: p => { p.X = 0; p.Y = 0; p.Lifetime = 0; });

        /// <summary>Gets a pooled particle packet.</summary>
        /// <remarks>PHASE 2 - Team 10: Particle Effect Pooling</remarks>
        public static ParticlePacket Get() => _pool.Get();

        /// <summary>Returns a packet to the pool.</summary>
        /// <remarks>PHASE 2 - Team 10: Particle Effect Pooling</remarks>
        public static void Return(ParticlePacket packet) => _pool.Return(packet);

        /// <summary>Returns available pooled packet count.</summary>
        /// <remarks>PHASE 2 - Team 10: Particle Effect Pooling</remarks>
        public static int Available => _pool.Available;
    }

    /// <summary>
    /// Physics prediction helper.
    /// </summary>
    /// <remarks>PHASE 2 - Team 10: Physics Prediction</remarks>
    public static class PhysicsPredictionSystem
    {
        /// <summary>Predicts next position from velocity and timestep.</summary>
        /// <remarks>PHASE 2 - Team 10: Physics Prediction</remarks>
        public static PointF Predict(PointF pos, PointF vel, float dt)
        {
            float t = Math.Max(0f, dt);
            return new PointF(pos.X + vel.X * t, pos.Y + vel.Y * t);
        }
    }

    /// <summary>
    /// Camera shake sequencer.
    /// </summary>
    /// <remarks>PHASE 2 - Team 10: Camera Shake Sequencer</remarks>
    public static class CameraShakeSequencer
    {
        /// <summary>Returns shake offset at elapsed time.</summary>
        /// <remarks>PHASE 2 - Team 10: Camera Shake Sequencer</remarks>
        public static PointF Offset(float elapsed, float amplitude = 6f)
        {
            float a = Math.Max(0f, amplitude);
            float x = (float)Math.Sin(elapsed * 40f) * a;
            float y = (float)Math.Cos(elapsed * 34f) * a * 0.7f;
            return new PointF(x, y);
        }
    }

    /// <summary>
    /// Blur effect strength model.
    /// </summary>
    /// <remarks>PHASE 2 - Team 10: Blur Effect System</remarks>
    public static class BlurEffectSystem
    {
        /// <summary>Returns blur radius from speed.</summary>
        /// <remarks>PHASE 2 - Team 10: Blur Effect System</remarks>
        public static float Radius(float speed)
        {
            return Math.Max(0f, Math.Min(12f, speed * 0.18f));
        }
    }

    /// <summary>
    /// Vignette renderer helper values.
    /// </summary>
    /// <remarks>PHASE 2 - Team 10: Vignette Renderer</remarks>
    public static class VignetteRendererSystem
    {
        /// <summary>Returns vignette opacity from threat level.</summary>
        /// <remarks>PHASE 2 - Team 10: Vignette Renderer</remarks>
        public static int Opacity(float threat)
        {
            float t = Math.Max(0f, Math.Min(5f, threat));
            return (int)Math.Round(30 + t * 30);
        }
    }

    /// <summary>
    /// Camera zoom mechanic helper.
    /// </summary>
    /// <remarks>PHASE 2 - Team 10: Zoom Mechanic</remarks>
    public static class ZoomMechanicSystem
    {
        /// <summary>Returns clamped zoom scalar.</summary>
        /// <remarks>PHASE 2 - Team 10: Zoom Mechanic</remarks>
        public static float Clamp(float zoom)
        {
            return Math.Max(0.75f, Math.Min(1.8f, zoom));
        }
    }

    /// <summary>
    /// Culling visibility utility.
    /// </summary>
    /// <remarks>PHASE 2 - Team 10: Culling System</remarks>
    public static class CullingSystem
    {
        /// <summary>Returns true when world rectangle is visible in camera viewport.</summary>
        /// <remarks>PHASE 2 - Team 10: Culling System</remarks>
        public static bool Visible(Rectangle worldRect, Rectangle cameraRect)
        {
            return worldRect.IntersectsWith(cameraRect);
        }
    }

    /// <summary>
    /// Lighting intensity helper.
    /// </summary>
    /// <remarks>PHASE 2 - Team 10: Lighting System</remarks>
    public static class LightingSystem
    {
        /// <summary>Returns ambient intensity from day-phase [0..1].</summary>
        /// <remarks>PHASE 2 - Team 10: Lighting System</remarks>
        public static float Ambient(float dayPhase)
        {
            float p = Math.Max(0f, Math.Min(1f, dayPhase));
            return 0.35f + (float)Math.Sin(p * Math.PI) * 0.45f;
        }
    }

    /// <summary>
    /// Post-processing pipeline stage registry.
    /// </summary>
    /// <remarks>PHASE 2 - Team 10: Post-Processing Pipeline</remarks>
    public static class PostProcessingPipelineSystem
    {
        private static readonly List<string> _stages = new List<string>
        {
            "tone-map",
            "vignette",
            "scanline",
            "gamma-correct"
        };

        /// <summary>Returns enabled pipeline stage names.</summary>
        /// <remarks>PHASE 2 - Team 10: Post-Processing Pipeline</remarks>
        public static IReadOnlyList<string> Stages() => _stages;
    }
}
