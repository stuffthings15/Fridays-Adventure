// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 – Team 4 (Lead Game Designer)
// Entities/StarCoinPickup.cs
// Purpose: One of three hidden star coins per level. Collecting all 3 grants
//          bonus score and contributes to world-gate unlocking.
// ────────────────────────────────────────────────────────────────────────────
// Team 4  (Lead Game Designer)  – Idea 6: Star coins (3 per level)
// Team 7  (Gameplay Programmer) – collision with player
// Team 14 (Environment Artist)  – golden star disc visual
// Team 17 (VFX Artist)          – sparkle burst on collect
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Entities
{
    /// <summary>
    /// One of three star coins placed in a level.  Collecting all three grants
    /// a bonus score and tracks toward world-gate requirements.
    ///
    /// Team 4  (Lead Game Designer)  — Idea 6.
    /// Team 14 (Environment Artist)  — golden disc visual.
    /// Team 17 (VFX Artist)          — sparkle on collection.
    /// </summary>
    public sealed class StarCoinPickup
    {
        // ── Position & dimensions ─────────────────────────────────────────────
        public float X       { get; }
        public float Y       { get; }
        private const int W  = 24;
        private const int H  = 24;

        public Rectangle Hitbox => new Rectangle((int)X, (int)Y, W, H);

        // ── State ─────────────────────────────────────────────────────────────
        public bool Collected { get; private set; }
        private float _spinAngle;
        private float _bobTimer;

        // ── Bonus value (Team 4) ──────────────────────────────────────────────
        public const int BonusScore = 1000;

        public StarCoinPickup(float x, float y)
        {
            X = x;
            Y = y;
        }

        // ── Update (Team 7 — collision) ────────────────────────────────────────
        /// <summary>
        /// Advances animation and checks player collection.
        /// Returns true when collected this frame.
        /// </summary>
        public bool Update(float dt, Player player)
        {
            if (Collected) return false;

            _spinAngle += 180f * dt;
            _bobTimer  += dt;

            if (!player.Hitbox.IntersectsWith(Hitbox)) return false;

            Collected = true;

            // Award score and increment counter
            Game.Instance.PlayerBounty       += BonusScore;
            Game.Instance.StarCoinsThisLevel  = Math.Min(3, Game.Instance.StarCoinsThisLevel + 1);
            Game.Instance.StarCoinsTotal++;

            Game.Instance.FloatingText.Spawn($"+{BonusScore}",
                (int)(X + W / 2f), (int)Y - 16, Color.Gold, large: true);

            // Sparkle burst (Team 17)
            ParticleSystem.SpawnCoinSparkle(X + W / 2f, Y + H / 2f);
            ParticleSystem.SpawnBurst(X + W / 2f, Y + H / 2f, 8,
                Color.Gold, 40f, 120f, 0.8f, 1.4f, glow: true);

            return true;
        }

        // ── Draw (Team 14 — golden star disc) ─────────────────────────────────
        public void Draw(Graphics g, float cameraX)
        {
            if (Collected) return;

            float sx   = X - cameraX;
            float bob  = (float)Math.Sin(_bobTimer * 2.5f) * 4f;
            float sy   = Y + bob;

            // Squash/stretch spin illusion
            float scaleX = Math.Abs((float)Math.Cos(_spinAngle * Math.PI / 180f));
            float drawW  = scaleX * W;
            float drawX  = sx + (W - drawW) / 2f;

            // Outer glow
            using (var br = new SolidBrush(Color.FromArgb(60, 255, 220, 0)))
                g.FillEllipse(br, drawX - 4, sy - 4, drawW + 8, H + 8);

            // Gold disc
            using (var br = new SolidBrush(Color.Gold))
                g.FillEllipse(br, drawX, sy, drawW, H);

            // Star symbol
            if (drawW > 8)
            {
                using (var f = new Font("Courier New", 10, FontStyle.Bold))
                {
                    var sz = g.MeasureString("★", f);
                    using (var br = new SolidBrush(Color.FromArgb(180, 200, 140, 0)))
                        g.DrawString("★", f, br,
                            drawX + (drawW  - sz.Width)  / 2f,
                            sy    + (H      - sz.Height) / 2f);
                }
            }

            // Bright highlight
            using (var br = new SolidBrush(Color.FromArgb(120, 255, 255, 200)))
                g.FillEllipse(br, drawX + 2, sy + 2, drawW * 0.4f, H * 0.35f);
        }
    }
}
