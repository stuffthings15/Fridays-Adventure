using System;
using System.Drawing;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Screen-shake manager — provides trauma-based camera offset for game-feel impact.
    ///
    /// Team 17 (VFX Artist)    — landing, explosion, and boss-hit screen shake.
    /// Team 9  (UI Programmer) — shake applied in Game.OnRender before scene draw.
    /// Team 7  (Gameplay)      — ground pound landing triggers MaxTrauma shake.
    ///
    /// Based on a "trauma" model: Trigger() adds trauma (0–1); shake magnitude decays
    /// proportional to trauma². This gives a sharp hit followed by smooth fade-out.
    ///
    /// Usage:
    ///   Game.Instance.ScreenShake.Trigger(0.7f);   // boss hit
    ///   Game.Instance.ScreenShake.Trigger(1.0f);   // ground pound land
    ///
    ///   // In render loop, before drawing:
    ///   Game.Instance.ScreenShake.ApplyTranslation(g);
    ///   // ...draw scene...
    ///   Game.Instance.ScreenShake.ResetTranslation(g);
    /// </summary>
    public sealed class ScreenShake
    {
        // ── Config ────────────────────────────────────────────────────────────
        /// <summary>Maximum pixel offset at full trauma.</summary>
        private const float MaxOffset   = 14f;

        /// <summary>How fast trauma decays per second.</summary>
        private const float DecayRate   = 1.8f;

        /// <summary>Frequency multiplier for shake oscillation (higher = faster).</summary>
        private const float Frequency   = 28f;

        // ── State ─────────────────────────────────────────────────────────────
        private float _trauma;          // 0 = still, 1 = maximum shake
        private float _shakeTime;       // accumulates time for pseudo-random offset

        // Stored translation so we can undo it after drawing.
        private float _lastOffsetX;
        private float _lastOffsetY;

        // ── Public API ────────────────────────────────────────────────────────
        /// <summary>
        /// Adds trauma. Values are clamped to [0, 1].
        /// Multiple calls in one frame stack additively.
        /// </summary>
        public void Trigger(float trauma)
        {
            _trauma = Math.Min(1f, _trauma + trauma);
        }

        /// <summary>Whether the camera is currently shaking.</summary>
        public bool IsShaking => _trauma > 0.01f;

        /// <summary>
        /// Advances the trauma decay.  Call once per Update tick.
        /// </summary>
        public void Update(float dt)
        {
            if (_trauma <= 0f) return;
            _shakeTime += dt * Frequency;
            _trauma = Math.Max(0f, _trauma - DecayRate * dt);
        }

        /// <summary>
        /// Translates the Graphics object by the current shake offset.
        /// Must be paired with a call to ResetTranslation.
        /// </summary>
        public void ApplyTranslation(Graphics g)
        {
            if (_trauma <= 0.01f) { _lastOffsetX = _lastOffsetY = 0f; return; }

            // Shake magnitude scales with trauma² for a snappy feel.
            float mag = _trauma * _trauma * MaxOffset;

            // Use sin/cos at different frequencies for pseudo-random offset.
            _lastOffsetX = (float)Math.Sin(_shakeTime * 1.1f) * mag;
            _lastOffsetY = (float)Math.Cos(_shakeTime * 0.7f) * mag;

            g.TranslateTransform(_lastOffsetX, _lastOffsetY);
        }

        /// <summary>Undoes the translation applied by ApplyTranslation.</summary>
        public void ResetTranslation(Graphics g)
        {
            if (_lastOffsetX == 0f && _lastOffsetY == 0f) return;
            g.TranslateTransform(-_lastOffsetX, -_lastOffsetY);
        }
    }
}
