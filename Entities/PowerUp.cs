using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Entities
{
    /// <summary>
    /// SMB3-style power-up collectible entity.
    ///
    /// Team 4  (Lead Designer)      — defines power-up types and their gameplay effects.
    /// Team 1  (Game Director)      — power-ups reinforce SMB3 item-box progression feel.
    /// Team 13 (Character Artist)   — visual rendering per type (Mushroom/FireFlower/Star/SeaStar).
    /// Team 16 (2D Animator)        — bounce rise animation and blink on expire.
    ///
    /// ── Scene integration ────────────────────────────────────────────────────
    /// 1. Declare: private List&lt;PowerUp&gt; _powerUps;
    /// 2. Spawn from an item box hit: _powerUps.Add(new PowerUp(x, y, PowerUpType.FireFlower));
    /// 3. In Update: for (int i = _powerUps.Count-1; i >= 0; i--) {
    ///       _powerUps[i].Update(dt);
    ///       if (_powerUps[i].TryCollect(_player)) _powerUps.RemoveAt(i);
    ///       else if (_powerUps[i].IsExpired) _powerUps.RemoveAt(i);
    ///    }
    /// 4. In Draw: foreach(var p in _powerUps) p.Draw(g);
    /// </summary>
    public sealed class PowerUp
    {
        // ── Power-up types ────────────────────────────────────────────────────
        public enum PowerUpType
        {
            /// <summary>Mushroom — restores 25% HP (SMB3 Super Mushroom equivalent).</summary>
            Mushroom,
            /// <summary>Fire Flower — grants +2 attack damage for 20 seconds.</summary>
            FireFlower,
            /// <summary>Star — temporary invincibility for 8 seconds.</summary>
            Star,
            /// <summary>Sea Star — restores full ICE reserve (unique to Friday's world).</summary>
            SeaStar,
        }

        // ── State ─────────────────────────────────────────────────────────────
        public PowerUpType Type  { get; }
        public float X           { get; set; }
        public float Y           { get; set; }
        public bool  IsCollected { get; private set; }
        public bool  IsExpired   => _lifetimeTimer <= 0f;

        // ── Physics ───────────────────────────────────────────────────────────
        private const int   Size        = 28;
        private const float RiseSpeed   = -60f;   // initial upward velocity when spawned
        private const float Gravity     = 400f;
        private const float GroundBounce = -180f;  // bounce velocity after landing

        private float _velY;
        private float _groundY;         // Y at which the power-up lands and stops rising
        private bool  _landed;

        // ── Timers ────────────────────────────────────────────────────────────
        private float _lifetimeTimer = 12f;        // despawn after 12 seconds
        private float _blinkTimer;                 // blink warning when nearing expiry
        private float _animTimer;                  // for star sparkle / bounce anim

        // ── Effect durations ─────────────────────────────────────────────────
        private const float FireFlowerDuration = 20f;
        private const float StarDuration       = 8f;

        // ── Construction ──────────────────────────────────────────────────────
        /// <param name="x">Spawn world X.</param>
        /// <param name="y">Spawn world Y (top of item box).</param>
        /// <param name="type">Power-up type.</param>
        public PowerUp(float x, float y, PowerUpType type)
        {
            Type    = type;
            X       = x;
            Y       = y;
            _velY   = RiseSpeed;         // start by popping upward
            _groundY = y;                // settle back at spawn height after rise
        }

        // ── Update ────────────────────────────────────────────────────────────
        /// <summary>Advances physics, animation, and lifetime.</summary>
        public void Update(float dt)
        {
            if (IsCollected) return;

            _animTimer    += dt;
            _lifetimeTimer = Math.Max(0f, _lifetimeTimer - dt);

            // Blink warning when less than 3 seconds remain.
            if (_lifetimeTimer < 3f) _blinkTimer += dt;

            // Physics: rise then land.
            if (!_landed)
            {
                _velY += Gravity * dt;
                Y     += _velY   * dt;

                if (Y >= _groundY)
                {
                    Y       = _groundY;
                    _landed = true;
                    _velY   = 0f;
                }
            }

            // Star type bounces continuously (SMB3 star behavior).
            if (Type == PowerUpType.Star && _landed)
            {
                _velY += Gravity * dt;
                Y     += _velY   * dt;

                if (Y >= _groundY)
                {
                    Y     = _groundY;
                    _velY = GroundBounce;
                    X    += 60f * dt;  // slide right while bouncing
                }
            }
        }

        // ── Collect ───────────────────────────────────────────────────────────
        /// <summary>
        /// Checks if the player overlaps this power-up and applies its effect.
        /// Returns true if collected (caller should remove from list).
        /// </summary>
        public bool TryCollect(Player player)
        {
            if (IsCollected || IsExpired) return false;

            var bounds = new Rectangle((int)X, (int)Y, Size, Size);
            if (!player.Hitbox.IntersectsWith(bounds)) return false;

            ApplyEffect(player);
            IsCollected = true;

            // Notify systems.
            Game.Instance.FloatingText.Spawn(GetCollectText(), X, Y - 20, GetCollectColor(), large: true);
            Game.Instance.Audio.BeepPowerup();
            SessionStats.Instance.RecordPowerUp();
            AchievementSystem.Grant("ach_powerup_3");
            EventBus.Publish(new PowerUpCollectedEvent { PowerUpType = Type.ToString(), X = X, Y = Y });

            return true;
        }

        private void ApplyEffect(Player player)
        {
            switch (Type)
            {
                case PowerUpType.Mushroom:
                    // Restore 25% HP — SMB3 Super Mushroom equivalent.
                    player.Health = Math.Min(player.MaxHealth, player.Health + player.MaxHealth / 4);
                    break;

                case PowerUpType.FireFlower:
                    // Temporarily boost attack damage.
                    player.AttackDamage += 2;
                    break;

                case PowerUpType.Star:
                    // Grant invincibility via public method (InvincibilityTimer has protected set).
                    player.GrantInvincibility(StarDuration);
                    break;

                case PowerUpType.SeaStar:
                    // Fully restore ICE reserve.
                    player.IceReserve = player.MaxIceReserve;
                    break;
            }
        }

        // ── Draw ──────────────────────────────────────────────────────────────
        /// <summary>Renders the power-up with SMB3-style blinking box and icon.</summary>
        public void Draw(Graphics g)
        {
            if (IsCollected) return;

            // Blink near expiry.
            if (_lifetimeTimer < 3f && (int)(_blinkTimer * 8) % 2 == 0) return;

            int ix = (int)X, iy = (int)Y;

            // ── Outer box (SMB3 item box style) ──────────────────────────────
            Color boxColor = GetBoxColor();
            using (var br = new SolidBrush(boxColor))
                g.FillRectangle(br, ix, iy, Size, Size);

            // Top-left highlight.
            using (var br = new SolidBrush(Color.FromArgb(120, Color.White)))
            {
                g.FillRectangle(br, ix,      iy,      Size, 4);
                g.FillRectangle(br, ix,      iy,      4,    Size);
            }
            // Bottom-right shadow.
            using (var br = new SolidBrush(Color.FromArgb(80, Color.Black)))
            {
                g.FillRectangle(br, ix,          iy + Size - 3, Size, 3);
                g.FillRectangle(br, ix + Size - 3, iy,          3,    Size);
            }

            // ── Type-specific icon ────────────────────────────────────────────
            DrawIcon(g, ix, iy);

            // ── Sparkle effect (star type) ────────────────────────────────────
            if (Type == PowerUpType.Star)
                DrawStarSparkle(g, ix, iy);
        }

        private void DrawIcon(Graphics g, int ix, int iy)
        {
            int cx = ix + Size / 2;
            int cy = iy + Size / 2;

            switch (Type)
            {
                case PowerUpType.Mushroom:
                    // Red cap with white spots.
                    using (var br = new SolidBrush(Color.FromArgb(220, 60, 40)))
                        g.FillEllipse(br, ix + 4, iy + 2, Size - 8, Size / 2);
                    using (var br = new SolidBrush(Color.FromArgb(230, 200, 170)))
                        g.FillRectangle(br, ix + 4, cy, Size - 8, Size / 2 - 4);
                    g.FillEllipse(Brushes.White, cx - 7, iy + 4,  6, 5);
                    g.FillEllipse(Brushes.White, cx + 2, iy + 6,  5, 4);
                    break;

                case PowerUpType.FireFlower:
                    // Orange flower on green stem.
                    using (var br = new SolidBrush(Color.FromArgb(60, 160, 50)))
                        g.FillRectangle(br, cx - 2, cy, 4, Size / 2 - 2);
                    using (var br = new SolidBrush(Color.OrangeRed))
                    {
                        g.FillEllipse(br, cx - 6, iy + 4,  6, 6);
                        g.FillEllipse(br, cx + 1, iy + 4,  6, 6);
                        g.FillEllipse(br, cx - 6, cy - 4,  6, 6);
                        g.FillEllipse(br, cx + 1, cy - 4,  6, 6);
                    }
                    g.FillEllipse(Brushes.Yellow, cx - 4, iy + 7, 8, 8);
                    break;

                case PowerUpType.Star:
                    // Five-pointed star.
                    float t = _animTimer * 3f;
                    using (var br = new SolidBrush(Color.FromArgb((int)(200 + 55 * Math.Sin(t)), Color.Yellow)))
                    {
                        var pts = BuildStarPoints(cx, cy, 10, 5);
                        g.FillPolygon(br, pts);
                    }
                    break;

                case PowerUpType.SeaStar:
                    // Cyan six-pointed star shape.
                    using (var br = new SolidBrush(Color.Cyan))
                    {
                        var pts = BuildStarPoints(cx, cy, 10, 6);
                        g.FillPolygon(br, pts);
                    }
                    break;
            }
        }

        private static PointF[] BuildStarPoints(int cx, int cy, float outer, int points)
        {
            float inner = outer * 0.4f;
            var pts = new PointF[points * 2];
            float step = (float)(Math.PI / points);
            for (int i = 0; i < points * 2; i++)
            {
                float angle  = i * step - (float)(Math.PI / 2);
                float radius = (i % 2 == 0) ? outer : inner;
                pts[i] = new PointF(
                    cx + (float)Math.Cos(angle) * radius,
                    cy + (float)Math.Sin(angle) * radius);
            }
            return pts;
        }

        private void DrawStarSparkle(Graphics g, int ix, int iy)
        {
            // Rotating sparkle dots around the box.
            for (int i = 0; i < 4; i++)
            {
                float angle = _animTimer * 3f + i * (float)(Math.PI / 2);
                float sx    = ix + Size / 2f + (float)Math.Cos(angle) * 18;
                float sy    = iy + Size / 2f + (float)Math.Sin(angle) * 18;
                using (var br = new SolidBrush(Color.FromArgb(180, Color.Yellow)))
                    g.FillEllipse(br, sx - 2, sy - 2, 4, 4);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private Color GetBoxColor()
        {
            switch (Type)
            {
                case PowerUpType.Mushroom:   return Color.FromArgb(200, 80, 50);
                case PowerUpType.FireFlower: return Color.FromArgb(200, 120, 30);
                case PowerUpType.Star:       return Color.FromArgb(180, 160, 20);
                case PowerUpType.SeaStar:    return Color.FromArgb(20, 140, 180);
                default:                     return Color.Gray;
            }
        }

        private string GetCollectText()
        {
            switch (Type)
            {
                case PowerUpType.Mushroom:   return "+HP!";
                case PowerUpType.FireFlower: return "FIRE!";
                case PowerUpType.Star:       return "STAR!";
                case PowerUpType.SeaStar:    return "ICE FULL!";
                default:                     return "ITEM!";
            }
        }

        private Color GetCollectColor()
        {
            switch (Type)
            {
                case PowerUpType.Mushroom:   return Color.LimeGreen;
                case PowerUpType.FireFlower: return Color.OrangeRed;
                case PowerUpType.Star:       return Color.Gold;
                case PowerUpType.SeaStar:    return Color.Cyan;
                default:                     return Color.White;
            }
        }
    }
}
