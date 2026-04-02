using System;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Reusable easing functions for smooth UI animations, camera transitions,
    /// power-up effects, and screen-shake curves.
    ///
    /// Team 8 (Systems Programmer) — easing library for designer-friendly animation curves.
    /// Team 16 (2D Animator)       — animation interpolation via ease-in/out.
    /// Team 17 (VFX Artist)        — particle decay and screen-shake envelope.
    ///
    /// All functions map t ∈ [0, 1] → value ∈ [0, 1] unless noted.
    /// </summary>
    public static class EasingFunctions
    {
        // ── Linear ────────────────────────────────────────────────────────────
        /// <summary>No easing — constant rate of change.</summary>
        public static float Linear(float t) => Clamp01(t);

        // ── Quadratic ─────────────────────────────────────────────────────────
        /// <summary>Accelerates from zero velocity (slow start).</summary>
        public static float EaseInQuad(float t)  { t = Clamp01(t); return t * t; }

        /// <summary>Decelerates to zero velocity (slow end).</summary>
        public static float EaseOutQuad(float t) { t = Clamp01(t); return t * (2f - t); }

        /// <summary>Accelerates then decelerates (smooth S-curve).</summary>
        public static float EaseInOutQuad(float t)
        {
            t = Clamp01(t);
            return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
        }

        // ── Cubic ─────────────────────────────────────────────────────────────
        /// <summary>Faster acceleration than quad.</summary>
        public static float EaseInCubic(float t)  { t = Clamp01(t); return t * t * t; }

        /// <summary>Faster deceleration than quad.</summary>
        public static float EaseOutCubic(float t) { t = Clamp01(t); float u = 1f - t; return 1f - u * u * u; }

        /// <summary>Strong smooth S-curve.</summary>
        public static float EaseInOutCubic(float t)
        {
            t = Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 3) / 2f;
        }

        // ── Bounce (SMB3-style coin/power-up land effect) ─────────────────────
        /// <summary>
        /// Bounces at the end — great for coins landing or power-up appearing.
        /// </summary>
        public static float EaseOutBounce(float t)
        {
            t = Clamp01(t);
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1)
                return n1 * t * t;
            else if (t < 2f / d1)
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            else if (t < 2.5f / d1)
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            else
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }

        /// <summary>Bounces at the start.</summary>
        public static float EaseInBounce(float t) => 1f - EaseOutBounce(1f - t);

        // ── Elastic (spring overshoot) ────────────────────────────────────────
        /// <summary>
        /// Spring overshoot on exit — good for star collect, power-up reveal.
        /// </summary>
        public static float EaseOutElastic(float t)
        {
            t = Clamp01(t);
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = (float)(2.0 * Math.PI / 3.0);
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c4) + 1);
        }

        // ── Sine ──────────────────────────────────────────────────────────────
        /// <summary>Gentle sine-wave acceleration.</summary>
        public static float EaseInSine(float t)
        {
            t = Clamp01(t);
            return 1f - (float)Math.Cos(t * Math.PI / 2.0);
        }

        /// <summary>Gentle sine-wave deceleration.</summary>
        public static float EaseOutSine(float t)
        {
            t = Clamp01(t);
            return (float)Math.Sin(t * Math.PI / 2.0);
        }

        // ── Back (overshoot) ──────────────────────────────────────────────────
        /// <summary>
        /// Overshoots target then settles back — used for card/panel slide-in animations.
        /// Team 16 (2D Animator) — Idea 1: satisfying snap-in for level intro cards.
        /// </summary>
        public static float EaseOutBack(float t)
        {
            t = Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        /// <summary>
        /// Pulls back before moving forward — used for power-up charge wind-ups.
        /// Team 16 (2D Animator) — Idea 2: power-up activation wind-up.
        /// </summary>
        public static float EaseInBack(float t)
        {
            t = Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }

        /// <summary>
        /// Overshoots on both entry and exit — strong emphasis snap.
        /// Team 16 (2D Animator) — Idea 3: boss name plate entrance.
        /// </summary>
        public static float EaseInOutBack(float t)
        {
            t = Clamp01(t);
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;
            if (t < 0.5f)
                return ((2f * t) * (2f * t) * ((c2 + 1f) * 2f * t - c2)) / 2f;
            float u = 2f * t - 2f;
            return (u * u * ((c2 + 1f) * u + c2) + 2f) / 2f;
        }

        // ── Interpolation helpers ─────────────────────────────────────────────
        /// <summary>
        /// Linearly interpolates between a and b using eased t.
        /// <code>float x = EasingFunctions.Lerp(startX, endX, EasingFunctions.EaseOutCubic(progress));</code>
        /// </summary>
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);

        /// <summary>Clamps t to [0, 1].</summary>
        public static float Clamp01(float t) => t < 0f ? 0f : (t > 1f ? 1f : t);
    }
}
