// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 – Multi-Team Implementation
// Entities/Fireball.cs
// Purpose: SMB3-style bouncing fireball projectile and hammer projectile.
// ────────────────────────────────────────────────────────────────────────────
// Team 4  (Lead Game Designer)  – Idea 6:  Fireball entity (Fire Flower attack)
// Team 4  (Lead Game Designer)  – Idea 7:  Floor bounce mechanic (up to 3 bounces)
// Team 4  (Lead Game Designer)  – Idea 8:  Destroyed on wall contact
// Team 4  (Lead Game Designer)  – Idea 9:  Hammer projectile (Hammer Bros variant)
// Team 7  (Gameplay Programmer) – Idea 5:  Parabolic arc with gravity
// Team 7  (Gameplay Programmer) – Idea 6:  Enemy hit detection on overlap
// Team 7  (Gameplay Programmer) – Idea 7:  Bounce velocity damping per bounce
// Team 14 (Environment Artist)  – Idea 5:  Fireball orange disc visual
// Team 14 (Environment Artist)  – Idea 6:  Hammer grey block visual
// Team 17 (VFX Artist)          – Idea 3:  Orange spark trail particles
// Team 17 (VFX Artist)          – Idea 4:  Hit explosion burst on impact
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Entities
{
    // ══════════════════════════════════════════════════════════════════════════
    // Fireball
    // ══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// SMB3-style fireball that bounces along the ground and destroys enemies.
    /// Spawned by <see cref="Player.ShootFireball"/>.
    ///
    /// Team 4  (Lead Game Designer)  — Idea 6–8: entity, bounce, wall destroy.
    /// Team 7  (Gameplay Programmer) — Idea 5–7: physics and hit detection.
    /// Team 14 (Environment Artist)  — Idea 5: orange disc visual.
    /// Team 17 (VFX Artist)          — Idea 3–4: trail + hit burst.
    /// </summary>
    public sealed class Fireball : Entity
    {
        // ── Physics ───────────────────────────────────────────────────────────
        private const float Gravity     = 600f;
        private const float BounceVY    = -200f;    // upward velocity on each bounce
        private const float Speed       = 260f;     // horizontal speed
        private const int   MaxBounces  = 3;        // despawns after 3 floor bounces

        private int   _bounces;
        private float _lifetime;
        private const float MaxLifetime = 4.0f;

        // ── Trail particle timer ──────────────────────────────────────────────
        private float _trailTimer;
        private const float TrailInterval = 0.04f;

        // ── Visual spin ───────────────────────────────────────────────────────
        private float _spinAngle;

        /// <summary>
        /// Creates a fireball at world position travelling in the given direction.
        /// Team 4 (Lead Game Designer) — Idea 6.
        /// </summary>
        /// <param name="x">World-space spawn X.</param>
        /// <param name="y">World-space spawn Y.</param>
        /// <param name="facingRight">True = travels right; false = left.</param>
        public Fireball(float x, float y, bool facingRight)
            : base(x, y, 14, 14)
        {
            VelocityX    = facingRight ? Speed : -Speed;
            VelocityY    = -100f;   // slight upward arc on spawn
            FacingRight  = facingRight;
        }

        // ── Update ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Advances physics and checks for death conditions.
        /// Team 7 (Gameplay Programmer) — Idea 5–7.
        /// </summary>
        public void UpdateProjectile(float dt, int groundY, int levelWidth,
                                     List<Rectangle> platforms)
        {
            if (!IsActive) return;

            // Gravity (Team 7 — Idea 5)
            VelocityY += Gravity * dt;
            X         += VelocityX * dt;
            Y         += VelocityY * dt;
            _lifetime += dt;
            _spinAngle += 540f * dt;

            // Trail particles (Team 17 — Idea 3)
            _trailTimer += dt;
            if (_trailTimer >= TrailInterval)
            {
                _trailTimer = 0f;
                ParticleSystem.SpawnBurst(CenterX, CenterY, 2,
                    Color.OrangeRed, 10f, 40f, 0.3f, 0.6f);
            }

            // Lifetime expiry
            if (_lifetime >= MaxLifetime) { Despawn(); return; }

            // Level bounds — despawn at left/right edge
            if (X < -50 || X > levelWidth + 50) { Despawn(); return; }

            // Floor bounce (Team 7 — Idea 6 / Team 4 — Idea 7)
            bool hitFloor = false;
            if (Y + Height >= groundY)
            {
                Y         = groundY - Height;
                hitFloor  = true;
            }
            else
            {
                // Platform bounce
                foreach (var plat in platforms)
                {
                    if (VelocityY > 0f &&
                        X + Width > plat.Left + 4 && X < plat.Right - 4 &&
                        Y + Height >= plat.Top && Y + Height <= plat.Top + 16)
                    {
                        Y        = plat.Top - Height;
                        hitFloor = true;
                        break;
                    }
                }
            }

            if (hitFloor)
            {
                _bounces++;
                if (_bounces >= MaxBounces) { Despawn(); return; }
                // Damping per bounce (Team 7 — Idea 7)
                VelocityY = BounceVY * (1f - _bounces * 0.2f);
            }

            // Wall collision — horizontal platforms (Team 4 — Idea 8)
            foreach (var plat in platforms)
            {
                if (!Hitbox.IntersectsWith(plat)) continue;
                // Side hit
                if (VelocityY < 0 || Math.Abs(X + Width / 2f - (plat.Left + plat.Width / 2f)) >
                    (Width / 2f + plat.Width / 2f) * 0.6f)
                {
                    Despawn();
                    return;
                }
            }
        }

        /// <summary>
        /// Checks overlap with all enemies and damages the first one hit.
        /// Team 7 (Gameplay Programmer) — Idea 6.
        /// </summary>
        public bool CheckEnemyHit(List<Enemy> enemies, int damage)
        {
            if (!IsActive) return false;

            foreach (var e in enemies)
            {
                if (!e.IsAlive) continue;
                if (!Hitbox.IntersectsWith(e.Hitbox)) continue;

                e.TakeDamage(damage);
                Game.Instance.PlayerBounty += e.ScoreValue / 2;
                Game.Instance.FloatingText.Spawn(
                    $"-{damage}", (int)e.CenterX, (int)e.Y - 10, Color.OrangeRed);

                // Hit burst (Team 17 — Idea 4)
                ParticleSystem.SpawnEnemyDefeat(CenterX, CenterY);

                Despawn();
                return true;
            }
            return false;
        }

        private void Despawn()
        {
            IsActive = false;
        }

        // ── Draw (Team 14 — Idea 5 / Team 17 — Idea 3) ────────────────────────
        /// <summary>
        /// Draws the fireball as a spinning orange disc.
        /// Team 14 (Environment Artist) — Idea 5.
        /// </summary>
        public override void Draw(Graphics g)
        {
            if (!IsActive) return;

            // Glow halo (Team 17 — Idea 3)
            using (var br = new SolidBrush(Color.FromArgb(60, 255, 160, 0)))
                g.FillEllipse(br, X - 4, Y - 4, Width + 8, Height + 8);

            // Core disc — orange→yellow gradient
            using (var br = new System.Drawing.Drawing2D.PathGradientBrush(
                BuildCirclePath((int)CenterX, (int)CenterY, Width / 2)))
            {
                br.CenterColor   = Color.Yellow;
                br.SurroundColors = new[] { Color.OrangeRed };
                g.FillEllipse(
                    new SolidBrush(Color.OrangeRed),
                    X, Y, Width, Height);
            }

            // Bright core
            using (var br = new SolidBrush(Color.FromArgb(200, 255, 255, 100)))
                g.FillEllipse(br, X + 3, Y + 3, Width - 6, Height - 6);
        }

        private System.Drawing.Drawing2D.GraphicsPath BuildCirclePath(int cx, int cy, int r)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(cx - r, cy - r, r * 2, r * 2);
            return path;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Hammer (Hammer Bros projectile)
    // ══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Hammer projectile thrown by Hammer Bro enemies.
    /// Follows a short parabolic arc toward the player.
    ///
    /// Team 4  (Lead Game Designer)  — Idea 9:  Hammer projectile.
    /// Team 7  (Gameplay Programmer) — Idea 5:  Arc physics.
    /// Team 14 (Environment Artist)  — Idea 6:  Gray hammer block visual.
    /// </summary>
    public sealed class Hammer : Entity
    {
        private const float Gravity  = 700f;
        private float _lifetime;
        private const float MaxLife  = 1.8f;
        private float _spinAngle;

        public Hammer(float x, float y, float vx, float vy)
            : base(x, y, 18, 18)
        {
            VelocityX = vx;
            VelocityY = vy;
        }

        public void UpdateProjectile(float dt)
        {
            if (!IsActive) return;
            VelocityY += Gravity * dt;
            X         += VelocityX * dt;
            Y         += VelocityY * dt;
            _spinAngle += 360f * dt;
            _lifetime  += dt;
            if (_lifetime >= MaxLife) IsActive = false;
        }

        public bool CheckPlayerHit(Player player, int damage)
        {
            if (!IsActive) return false;
            if (!Hitbox.IntersectsWith(player.Hitbox)) return false;
            player.TakeDamage(damage);
            IsActive = false;
            return true;
        }

        public override void Draw(Graphics g)
        {
            if (!IsActive) return;

            var state = g.Save();
            g.TranslateTransform(CenterX, CenterY);
            g.RotateTransform(_spinAngle);
            g.TranslateTransform(-Width / 2f, -Height / 2f);

            // Hammer head — gray rectangle
            using (var br = new SolidBrush(Color.FromArgb(150, 150, 160)))
                g.FillRectangle(br, 0, 0, Width, Height * 0.6f);
            // Handle — brown stick
            using (var br = new SolidBrush(Color.FromArgb(130, 80, 30)))
                g.FillRectangle(br, Width * 0.35f, Height * 0.55f, Width * 0.3f, Height * 0.45f);

            g.Restore(state);
        }
    }
}
