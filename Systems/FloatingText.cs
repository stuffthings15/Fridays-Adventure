using System;
using System.Collections.Generic;
using System.Drawing;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Manages floating score / damage / status text popups in the game world.
    ///
    /// Team 9  (UI Programmer)  — score-pop animations, input-prompt feedback.
    /// Team 17 (VFX Artist)     — damage numbers, coin collect +1 pop, combo multiplier.
    /// Team 4  (Lead Designer)  — stomp chain bonus numbers, 100-berry extra-life pop.
    ///
    /// Usage:
    ///   Game.Instance.FloatingText.Spawn("+100", x, y, Color.Gold);
    ///   Game.Instance.FloatingText.Spawn("-10",  x, y, Color.OrangeRed, large: true);
    ///
    ///   // Each tick:
    ///   Game.Instance.FloatingText.Update(dt);
    ///
    ///   // Each draw (after scene, before HUD):
    ///   Game.Instance.FloatingText.Draw(g);
    /// </summary>
    public sealed class FloatingTextManager
    {
        // ── Config ────────────────────────────────────────────────────────────
        private const float DefaultLifetime  = 1.0f;
        private const float RiseSpeed        = 48f;     // pixels per second upward
        private const int   MaxEntries       = 64;

        // ── Entry model ───────────────────────────────────────────────────────
        private sealed class Entry
        {
            public string Text;
            public float  X, Y;
            public float  Age, Lifetime;
            public Color  Color;
            public bool   Large;       // large = 14pt, normal = 10pt
            public bool   Active;

            // Pool reset helper.
            public void Reset(string text, float x, float y, Color color, bool large, float lifetime)
            {
                Text      = text;
                X         = x;
                Y         = y;
                Color     = color;
                Large     = large;
                Age       = 0f;
                Lifetime  = lifetime;
                Active    = true;
            }
        }

        // ── Pooled entries ────────────────────────────────────────────────────
        private readonly Entry[] _entries = new Entry[MaxEntries];

        public FloatingTextManager()
        {
            for (int i = 0; i < MaxEntries; i++)
                _entries[i] = new Entry();
        }

        // ── Public API ────────────────────────────────────────────────────────
        /// <summary>
        /// Spawns a floating text label at a world-space position.
        /// </summary>
        /// <param name="text">Text to display (e.g. "+100", "STOMP!", "×3 COMBO").</param>
        /// <param name="x">World X origin.</param>
        /// <param name="y">World Y origin (text rises upward from here).</param>
        /// <param name="color">Fill color of the text.</param>
        /// <param name="large">If true, uses a larger font for emphasis.</param>
        /// <param name="lifetime">How many seconds before the text fades out.</param>
        public void Spawn(string text, float x, float y,
                          Color color, bool large = false, float lifetime = DefaultLifetime)
        {
            // Find an inactive slot (oldest-first recycling).
            Entry target = null;
            float oldest = 0f;
            Entry oldestEntry = null;

            foreach (var e in _entries)
            {
                if (!e.Active) { target = e; break; }
                if (e.Age > oldest) { oldest = e.Age; oldestEntry = e; }
            }

            // Recycle the oldest if no free slot.
            if (target == null) target = oldestEntry;
            if (target == null) return;

            target.Reset(text, x, y, color, large, lifetime);
        }

        // ── Update ────────────────────────────────────────────────────────────
        /// <summary>Advances all active entries. Call once per game tick.</summary>
        public void Update(float dt)
        {
            foreach (var e in _entries)
            {
                if (!e.Active) continue;
                e.Age += dt;
                e.Y   -= RiseSpeed * dt;   // float upward
                if (e.Age >= e.Lifetime)
                    e.Active = false;
            }
        }

        // ── Draw ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Draws all active floating text labels.
        /// Call after the scene draw but before the HUD.
        /// </summary>
        public void Draw(Graphics g)
        {
            foreach (var e in _entries)
            {
                if (!e.Active) continue;

                // Fade out in the last 30% of lifetime.
                float fadeStart = e.Lifetime * 0.7f;
                int alpha = e.Age < fadeStart
                    ? 255
                    : (int)(255 * (1f - (e.Age - fadeStart) / (e.Lifetime - fadeStart)));
                alpha = Math.Max(0, Math.Min(255, alpha));

                float size = e.Large ? 14f : 10f;
                using (var f   = new Font("Courier New", size, FontStyle.Bold))
                using (var br  = new SolidBrush(Color.FromArgb(alpha, e.Color)))
                using (var brS = new SolidBrush(Color.FromArgb(alpha / 2, Color.Black)))
                {
                    // Drop-shadow for readability against any background.
                    g.DrawString(e.Text, f, brS, e.X + 1f, e.Y + 1f);
                    g.DrawString(e.Text, f, br,  e.X,      e.Y);
                }
            }
        }

        /// <summary>Clears all active entries (call on scene change).</summary>
        public void Clear()
        {
            foreach (var e in _entries) e.Active = false;
        }
    }
}
