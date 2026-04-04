// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 10: Engine Programmer
// Feature: Engine Expansion Systems Pack
// Purpose: Implements Phase 3 engine-side systems for generation, pooling,
//          replay analysis, dynamic scaling, waypoints, checkpoints, camera,
//          dialogue animation, advanced weather, and shader preset library.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Team 10 Feature 1: Procedural Level Generator.
    /// </summary>
    public static class ProceduralLevelGenerator
    {
        /// <summary>Generates deterministic platform geometry for a seed.</summary>
        /// <remarks>PHASE 3 - Team 10: Procedural Level Generator</remarks>
        public static IReadOnlyList<Rectangle> Generate(int seed, int width, int height, int count = 20)
        {
            return ProceduralGenerationEngine.GeneratePlatforms(seed, width, height, count);
        }
    }

    /// <summary>
    /// Team 10 Feature 2: Advanced Pooling System.
    /// </summary>
    public static class AdvancedPoolingSystem
    {
        /// <summary>Reference wrapper so pooled points can use ObjectPool&lt;T&gt;.</summary>
        public sealed class PooledPoint
        {
            /// <summary>X coordinate.</summary>
            public float X;
            /// <summary>Y coordinate.</summary>
            public float Y;
        }

        private static readonly Dictionary<string, ObjectPool<PooledPoint>> _pointPools =
            new Dictionary<string, ObjectPool<PooledPoint>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Gets/creates a named point pool and rents one value.</summary>
        /// <remarks>PHASE 3 - Team 10: Advanced Pooling System</remarks>
        public static PooledPoint RentPoint(string poolName)
        {
            if (!_pointPools.ContainsKey(poolName))
                _pointPools[poolName] = new ObjectPool<PooledPoint>(
                    factory: () => new PooledPoint(),
                    onGet: p => { p.X = 0; p.Y = 0; },
                    onReturn: p => { p.X = 0; p.Y = 0; });
            return _pointPools[poolName].Get();
        }

        /// <summary>Returns a point to its named pool.</summary>
        /// <remarks>PHASE 3 - Team 10: Advanced Pooling System</remarks>
        public static void ReturnPoint(string poolName, PooledPoint value)
        {
            if (_pointPools.ContainsKey(poolName))
                _pointPools[poolName].Return(value);
        }
    }

    /// <summary>
    /// Team 10 Feature 3: Physics Replay System.
    /// </summary>
    public static class PhysicsReplaySystem
    {
        /// <summary>Returns replay file lines for analysis if file exists.</summary>
        /// <remarks>PHASE 3 - Team 10: Physics Replay System</remarks>
        public static IReadOnlyList<string> LoadReplay(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                return new[] { "Replay file not found." };
            return System.IO.File.ReadAllLines(path);
        }

        /// <summary>Computes a rough replay summary from captured lines.</summary>
        /// <remarks>PHASE 3 - Team 10: Physics Replay System</remarks>
        public static string Summarize(IReadOnlyList<string> lines)
        {
            if (lines == null || lines.Count == 0) return "Replay summary: empty.";
            return $"Replay summary: {lines.Count} frames captured.";
        }
    }

    /// <summary>
    /// Team 10 Feature 4: Dynamic Difficulty Scaling.
    /// </summary>
    public static class DynamicDifficultyScaling
    {
        /// <summary>Returns runtime difficulty multiplier from performance signals.</summary>
        /// <remarks>PHASE 3 - Team 10: Dynamic Difficulty Scaling</remarks>
        public static float GetScale(int deathCount, float playMinutes)
        {
            float baseScale = 1f;
            if (deathCount >= 8) baseScale -= 0.20f;
            if (deathCount >= 15) baseScale -= 0.15f;
            if (playMinutes >= 45f && deathCount <= 3) baseScale += 0.15f;
            return Math.Max(0.6f, Math.Min(1.4f, baseScale));
        }
    }

    /// <summary>
    /// Team 10 Feature 5: Waypoint System.
    /// </summary>
    public static class WaypointSystem
    {
        /// <summary>Waypoint model.</summary>
        public sealed class Waypoint
        {
            /// <summary>Unique waypoint id.</summary>
            public string Id { get; set; }
            /// <summary>Position in world space.</summary>
            public PointF Position { get; set; }
        }

        private static readonly List<Waypoint> _waypoints = new List<Waypoint>();

        /// <summary>Replaces current waypoint list with given values.</summary>
        /// <remarks>PHASE 3 - Team 10: Waypoint System</remarks>
        public static void Set(IEnumerable<Waypoint> points)
        {
            _waypoints.Clear();
            if (points != null) _waypoints.AddRange(points);
        }

        /// <summary>Returns configured waypoints.</summary>
        /// <remarks>PHASE 3 - Team 10: Waypoint System</remarks>
        public static IReadOnlyList<Waypoint> Get() => _waypoints;
    }

    /// <summary>
    /// Team 10 Feature 6: Checkpoint System Extended.
    /// </summary>
    public static class CheckpointSystemExtended
    {
        private static readonly Dictionary<string, PointF> _checkpoints =
            new Dictionary<string, PointF>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Sets/updates a checkpoint marker by id.</summary>
        /// <remarks>PHASE 3 - Team 10: Checkpoint System Extended</remarks>
        public static void Set(string id, PointF position)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            _checkpoints[id] = position;
        }

        /// <summary>Gets a checkpoint marker; returns false if missing.</summary>
        /// <remarks>PHASE 3 - Team 10: Checkpoint System Extended</remarks>
        public static bool TryGet(string id, out PointF position) => _checkpoints.TryGetValue(id, out position);
    }

    /// <summary>
    /// Team 10 Feature 7: Cinematic Camera System.
    /// </summary>
    public static class CinematicCameraSystem
    {
        /// <summary>Simple camera keyframe model.</summary>
        public sealed class Keyframe
        {
            /// <summary>Time in seconds from sequence start.</summary>
            public float Time { get; set; }
            /// <summary>Camera target point.</summary>
            public PointF Target { get; set; }
            /// <summary>Zoom scalar.</summary>
            public float Zoom { get; set; }
        }

        /// <summary>Interpolates camera state from keyframes at time t.</summary>
        /// <remarks>PHASE 3 - Team 10: Cinematic Camera System</remarks>
        public static Keyframe Evaluate(IReadOnlyList<Keyframe> keys, float t)
        {
            if (keys == null || keys.Count == 0) return new Keyframe { Time = 0, Target = new PointF(0, 0), Zoom = 1f };
            var ordered = keys.OrderBy(k => k.Time).ToList();
            if (t <= ordered[0].Time) return ordered[0];
            if (t >= ordered[ordered.Count - 1].Time) return ordered[ordered.Count - 1];

            for (int i = 0; i < ordered.Count - 1; i++)
            {
                var a = ordered[i];
                var b = ordered[i + 1];
                if (t < a.Time || t > b.Time) continue;
                float p = (t - a.Time) / Math.Max(0.0001f, b.Time - a.Time);
                return new Keyframe
                {
                    Time = t,
                    Target = new PointF(a.Target.X + (b.Target.X - a.Target.X) * p, a.Target.Y + (b.Target.Y - a.Target.Y) * p),
                    Zoom = a.Zoom + (b.Zoom - a.Zoom) * p
                };
            }

            return ordered[0];
        }
    }

    /// <summary>
    /// Team 10 Feature 8: Dialogue Animation.
    /// </summary>
    public static class DialogueAnimation
    {
        /// <summary>Returns visible characters count for typewriter animation.</summary>
        /// <remarks>PHASE 3 - Team 10: Dialogue Animation</remarks>
        public static int VisibleChars(string text, float elapsedSeconds, float charsPerSecond = 30f)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int count = (int)(Math.Max(0f, elapsedSeconds) * Math.Max(1f, charsPerSecond));
            return Math.Min(text.Length, count);
        }
    }

    /// <summary>
    /// Team 10 Feature 9: Weather System Advanced.
    /// </summary>
    public static class WeatherSystemAdvanced
    {
        /// <summary>Weather profile model.</summary>
        public sealed class WeatherProfile
        {
            /// <summary>Display name.</summary>
            public string Name { get; set; }
            /// <summary>Wind intensity scalar.</summary>
            public float Wind { get; set; }
            /// <summary>Precipitation intensity scalar.</summary>
            public float Rain { get; set; }
            /// <summary>Fog intensity scalar.</summary>
            public float Fog { get; set; }
        }

        private static readonly Dictionary<string, WeatherProfile> _profiles =
            new Dictionary<string, WeatherProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["clear"] = new WeatherProfile { Name = "Clear", Wind = 0.1f, Rain = 0f, Fog = 0f },
                ["storm"] = new WeatherProfile { Name = "Storm", Wind = 0.8f, Rain = 0.9f, Fog = 0.4f },
                ["mist"]  = new WeatherProfile { Name = "Mist",  Wind = 0.2f, Rain = 0.1f, Fog = 0.8f },
            };

        /// <summary>Gets weather profile by id with clear fallback.</summary>
        /// <remarks>PHASE 3 - Team 10: Weather System Advanced</remarks>
        public static WeatherProfile Get(string id)
        {
            return _profiles.TryGetValue(id ?? "clear", out var p) ? p : _profiles["clear"];
        }
    }

    /// <summary>
    /// Team 10 Feature 10: Shader Library.
    /// </summary>
    public static class ShaderLibrary
    {
        private static readonly string[] _presets =
        {
            "none",
            "crt-scanline",
            "vignette-soft",
            "pixel-bloom-lite",
            "storm-contrast",
            "cinematic-warm"
        };

        /// <summary>Returns available shader preset names.</summary>
        /// <remarks>PHASE 3 - Team 10: Shader Library</remarks>
        public static IReadOnlyList<string> GetPresets() => _presets;
    }
}
