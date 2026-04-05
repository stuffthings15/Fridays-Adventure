// ────────────────────────────────────────────────────────────────────────────
// Entities/FrostBallProjectile.cs
// Purpose: Blue ice projectile (Frost Ball) — bounces like a fireball but
//          with cyan/blue visuals and frost trail particles.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Entities
{
    /// <summary>
    /// Blue bouncing frost projectile fired by all characters on X key.
    /// Reuses SMB3-style fireball physics with ice-themed visuals.
    /// </summary>
    public sealed class FrostBallProjectile : Entity
    {
        // ── Physics (same as Fireball) ────────────────────────────────────────
        private const float Gravity    = 600f;
        private const float BounceVY   = -200f;
        private const float Speed      = 280f;
        private const int   MaxBounces = 3;
        private const float MaxLifetime = 4.0f;

        private int   _bounces;
        private float _lifetime;

        // ── Trail particle timer ──────────────────────────────────────────────
        private float _trailTimer;
        private const float TrailInterval = 0.04f;

        /// <summary>Damage dealt to enemies on contact.</summary>
        public int Damage { get; } = 15;

        /// <summary>
        /// Creates a frost ball at the given position travelling in the specified direction.
        /// </summary>
        public FrostBallProjectile(float x, float y, bool facingRight)
            : base(x, y, 14, 14)
        {
            VelocityX   = facingRight ? Speed : -Speed;
            VelocityY   = -100f;   // slight upward arc on spawn
            FacingRight = facingRight;
        }

        // ── Update ────────────────────────────────────────────────────────────
        /// <summary>
        /// Advances physics, handles bouncing, and checks for despawn conditions.
        /// </summary>
        public void UpdateProjectile(float dt, int groundY, int levelWidth,
                                     List<Rectangle> platforms)
        {
            if (!IsActive) return;

            // Gravity
            VelocityY += Gravity * dt;
            X         += VelocityX * dt;
            Y         += VelocityY * dt;
            _lifetime += dt;

            // Frost trail particles (cyan/blue)
            _trailTimer += dt;
            if (_trailTimer >= TrailInterval)
            {
                _trailTimer = 0f;
                ParticleSystem.SpawnBurst(CenterX, CenterY, 2,
                    Color.Cyan, 10f, 40f, 0.3f, 0.6f);
            }

            // Lifetime expiry
            if (_lifetime >= MaxLifetime) { IsActive = false; return; }

            // Level bounds
            if (X < -50 || X > levelWidth + 50) { IsActive = false; return; }

            // Floor / platform bounce
            bool hitFloor = false;
            if (Y + Height >= groundY)
            {
                Y = groundY - Height;
                hitFloor = true;
            }
            else
            {
                foreach (var plat in platforms)
                {
                    if (VelocityY > 0f &&
                        X + Width > plat.Left + 4 && X < plat.Right - 4 &&
                        Y + Height >= plat.Top && Y + Height <= plat.Top + 16)
                    {
                        Y = plat.Top - Height;
                        hitFloor = true;
                        break;
                    }
                }
            }

            if (hitFloor)
            {
                _bounces++;
                if (_bounces >= MaxBounces) { IsActive = false; return; }
                VelocityY = BounceVY * (1f - _bounces * 0.2f);
            }

            // Wall collision
            foreach (var plat in platforms)
            {
                if (!Hitbox.IntersectsWith(plat)) continue;
                if (VelocityY < 0 || Math.Abs(X + Width / 2f - (plat.Left + plat.Width / 2f)) >
                    (Width / 2f + plat.Width / 2f) * 0.6f)
                {
                    IsActive = false;
                    return;
                }
            }
        }

        /// <summary>
        /// Checks overlap with enemies and damages the first one hit.
        /// </summary>
        public bool CheckEnemyHit(List<Enemy> enemies)
        {
            if (!IsActive) return false;

            foreach (var e in enemies)
            {
                if (!e.IsAlive) continue;
                if (!Hitbox.IntersectsWith(e.Hitbox)) continue;

                e.TakeDamage(Damage);
                Game.Instance.PlayerBounty += e.ScoreValue / 2;
                Game.Instance.FloatingText.Spawn(
                    $"-{Damage}", (int)e.CenterX, (int)e.Y - 10, Color.Cyan);

                // Hit burst (blue/ice themed)
                ParticleSystem.SpawnBurst(CenterX, CenterY, 10,
                    Color.LightCyan, 30f, 100f, 0.2f, 0.5f);

                IsActive = false;
                return true;
            }
            return false;
        }

        // ── Draw ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Draws the frost ball as a spinning blue disc with cyan glow.
        /// </summary>
        public override void Draw(Graphics g)
        {
            if (!IsActive) return;

            // Glow halo (cyan)
            using (var br = new SolidBrush(Color.FromArgb(60, 0, 180, 255)))
                g.FillEllipse(br, X - 4, Y - 4, Width + 8, Height + 8);

            // Core disc — blue
            using (var br = new SolidBrush(Color.FromArgb(40, 140, 255)))
                g.FillEllipse(br, X, Y, Width, Height);

            // Bright frost center
            using (var br = new SolidBrush(Color.FromArgb(200, 180, 230, 255)))
                g.FillEllipse(br, X + 3, Y + 3, Width - 6, Height - 6);

            // Outline
            using (var pen = new Pen(Color.FromArgb(160, 100, 200, 255), 1.5f))
                g.DrawEllipse(pen, X, Y, Width, Height);
        }
    }
}
