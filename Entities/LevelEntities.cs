using System;
using System.Collections.Generic;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Entities
{
    /// <summary>
    /// Spring / bounce pad — launches the player with extra jump force on landing.
    ///
    /// Placed on top of ground tiles and platforms.  When the player lands on the
    /// spring surface, it fires them upward with a configurable force multiplier.
    ///
    /// Team 5  (Level Designer)     — Idea 9: bounce pad for level design variety.
    /// Team 7  (Gameplay Programmer) — physics interaction.
    /// Team 16 (2D Animator)         — spring compression animation.
    /// Team 17 (VFX Artist)          — launch burst particles.
    /// </summary>
    public sealed class SpringPad
    {
        // ── Geometry ────────────────────────────────────────────────────────────
        public Rectangle Bounds { get; }

        // ── Spring force multiplier ───────────────────────────────────────────
        private readonly float _forceMult;  // multiplied against player.JumpForce

        // ── Animation ────────────────────────────────────────────────────────
        private float _compressionTimer;     // > 0 while compressed
        private const float CompressTime = 0.12f;

        // ── VFX ───────────────────────────────────────────────────────────────
        private float _launchGlowTimer;

        /// <summary>
        /// Creates a spring pad at the given rectangle.
        /// </summary>
        /// <param name="bounds">World-space bounds; the top surface is the landing zone.</param>
        /// <param name="forceMult">
        ///   Jump force multiplier (1.5 = 50 % higher than normal jump).
        /// </param>
        public SpringPad(Rectangle bounds, float forceMult = 1.6f)
        {
            Bounds     = bounds;
            _forceMult = forceMult;
        }

        // ── Update ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Checks player collision and applies the spring launch.
        /// Call from each gameplay scene's Update() with the live player reference.
        /// </summary>
        public void UpdateWithPlayer(float dt, Player player)
        {
            // Decay compression and glow timers.
            if (_compressionTimer > 0f) _compressionTimer -= dt;
            if (_launchGlowTimer  > 0f) _launchGlowTimer  -= dt;

            // Only activate if the player is falling onto the top surface.
            var pb = new Rectangle((int)player.X, (int)player.Y, player.Width, player.Height);
            if (!pb.IntersectsWith(Bounds)) return;
            if (player.VelocityY <= 0f) return;  // must be moving downward

            // Player must be above the midpoint of the pad.
            if (player.Y + player.Height > Bounds.Y + Bounds.Height * 0.5f &&
                player.Y + player.Height < Bounds.Y + Bounds.Height + 4)
            {
                Launch(player);
            }
        }

        // ── Launch ─────────────────────────────────────────────────────────────

        private void Launch(Player player)
        {
            player.VelocityY      = player.JumpForce * _forceMult;
            player.IsGrounded     = false;
            player.JumpsRemaining = player.MaxJumps;    // refresh jumps on spring

            _compressionTimer = CompressTime;
            _launchGlowTimer  = 0.25f;

            // Screen shake on big spring.
            if (_forceMult >= 2.0f)
                Game.Instance.ScreenShake.Trigger(0.4f);

            // Floating text score bonus.
            Game.Instance.FloatingText.Spawn("SPRING!", (int)player.X + 8, (int)player.Y - 20, Color.LimeGreen);

            AchievementSystem.Grant("ach_spring");
        }

        // ── Draw ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws the spring pad sprite (SMB3-style coiled spring with launch glow).
        /// Call from scene Draw() with the camera X offset.
        /// </summary>
        public void Draw(Graphics g, float cameraX)
        {
            float sx = Bounds.X - cameraX;
            float sy = Bounds.Y;
            int   w  = Bounds.Width;
            int   h  = Bounds.Height;

            // Compression squish: slightly shorter when activated.
            float squeeze  = _compressionTimer > 0f ? 0.55f : 1.0f;
            float drawH    = h * squeeze;
            float drawY    = sy + (h - drawH);

            // Launch glow aura.
            if (_launchGlowTimer > 0f)
            {
                float alpha = _launchGlowTimer / 0.25f;
                using (var br = new SolidBrush(Color.FromArgb((int)(120 * alpha), Color.LimeGreen)))
                    g.FillEllipse(br, sx - 8, drawY - 8, w + 16, (float)drawH + 16);
            }

            // Base plate — dark gray.
            using (var br = new SolidBrush(Color.FromArgb(60, 60, 70)))
                g.FillRectangle(br, sx, drawY + drawH * 0.6f, w, drawH * 0.4f);

            // Coil — draw 3 arcs for a spring feel.
            using (var pen = new Pen(Color.FromArgb(200, 180, 200, 60), 3))
            {
                float coilH = (float)(drawH * 0.6f) / 3f;
                for (int i = 0; i < 3; i++)
                {
                    float cy = (float)(drawY + i * coilH);
                    g.DrawArc(pen, sx + 2, cy, w - 4, coilH, 0, 180);
                }
            }

            // Top "launch pad" surface.
            using (var br = new SolidBrush(Color.FromArgb(200, 60, 220, 60)))
                g.FillRectangle(br, sx, drawY, w, 6);
            using (var pen = new Pen(Color.LimeGreen, 1))
                g.DrawRectangle(pen, (int)sx, (int)drawY, w, 6);

            // Debug collision box.
            if (RenderDebugModes.ShowCollision)
                RenderDebugModes.DrawCollisionRect(g, new Rectangle((int)sx, Bounds.Y, w, h), "spring");
        }
    }

    // ── Falling Platform ──────────────────────────────────────────────────────

    /// <summary>
    /// A platform that begins to fall once the player has stood on it briefly.
    ///
    /// Team 5  (Level Designer)      — Idea 2: falling / breaking platform variant.
    /// Team 7  (Gameplay Programmer) — physics interaction, fall trigger.
    /// Team 16 (2D Animator)         — shake animation before falling.
    /// </summary>
    public sealed class FallingPlatform
    {
        // ── Geometry ────────────────────────────────────────────────────────────
        public float X      { get; private set; }
        public float Y      { get; private set; }
        public int   Width  { get; }
        public int   Height { get; }
        public Rectangle Bounds => new Rectangle((int)X, (int)Y, Width, Height);

        // ── State machine ──────────────────────────────────────────────────────
        private enum State { Idle, Shaking, Falling, Respawning }
        private State _state = State.Idle;

        private float _shakeTimer;
        private float _fallVelocity;
        private float _respawnTimer;

        private const float ShakeDuration   = 0.45f;  // warning shake before drop
        private const float RespawnTime     = 5.0f;   // seconds to respawn
        private const float FallGravity     = 500f;
        private const float OffscreenY      = 800f;

        private readonly float _originY;              // Y position to respawn to
        private float _shakeOffset;                   // horizontal shake displacement

        public bool IsActive => _state != State.Respawning;

        /// <param name="x">World-space left edge.</param>
        /// <param name="y">World-space top edge.</param>
        /// <param name="w">Width in pixels.</param>
        /// <param name="h">Height in pixels (usually 16–20).</param>
        public FallingPlatform(float x, float y, int w, int h = 16)
        {
            X = x; Y = y; _originY = y;
            Width = w; Height = h;
        }

        // ── Update ─────────────────────────────────────────────────────────────

        public void UpdateWithPlayer(float dt, Player player)
        {
            // Advance state timers.
            switch (_state)
            {
                case State.Shaking:
                    _shakeTimer += dt;
                    _shakeOffset = (float)(Math.Sin(_shakeTimer * 60f) * 3.0);
                    if (_shakeTimer >= ShakeDuration)
                    { _state = State.Falling; _fallVelocity = 0f; }
                    break;

                case State.Falling:
                    _fallVelocity += FallGravity * dt;
                    Y             += _fallVelocity * dt;
                    if (Y > OffscreenY)
                    { _state = State.Respawning; _respawnTimer = RespawnTime; Y = OffscreenY; }
                    break;

                case State.Respawning:
                    _respawnTimer -= dt;
                    if (_respawnTimer <= 0f)
                    { Y = _originY; _shakeOffset = 0f; _state = State.Idle; }
                    break;
            }

            // Trigger shake when player stands on this platform.
            if (_state == State.Idle)
            {
                var pb = new Rectangle((int)player.X, (int)player.Y, player.Width, player.Height);
                bool touching = pb.IntersectsWith(Bounds) && player.IsGrounded;
                if (touching) _state = State.Shaking;
            }
        }

        // ── Draw ───────────────────────────────────────────────────────────────

        public void Draw(Graphics g, float cameraX)
        {
            if (_state == State.Respawning) return;

            float sx = X + _shakeOffset - cameraX;
            float sy = Y;

            // Color varies by state.
            Color top   = _state == State.Shaking ? Color.OrangeRed : Color.FromArgb(160, 100, 60);
            Color body  = Color.FromArgb(100, 70, 40);
            Color edge  = Color.FromArgb(200, 140, 80);

            // Body.
            using (var br = new SolidBrush(body))
                g.FillRectangle(br, sx, sy, Width, Height);

            // Top stripe (SMB3-style brick top).
            using (var br = new SolidBrush(top))
                g.FillRectangle(br, sx, sy, Width, 5);

            // Brick segment lines.
            using (var pen = new Pen(edge, 1))
            {
                for (int bx = 0; bx < Width; bx += 20)
                    g.DrawLine(pen, sx + bx, sy, sx + bx, sy + Height);
                g.DrawLine(pen, sx, sy + Height / 2, sx + Width, sy + Height / 2);
            }

            if (RenderDebugModes.ShowCollision)
                RenderDebugModes.DrawCollisionRect(g, new Rectangle((int)sx, (int)sy, Width, Height), "fall-plat");
        }
    }

    // ── Level Timer ───────────────────────────────────────────────────────────

    /// <summary>
    /// Countdown timer for levels where time pressure applies.
    ///
    /// Team 5  (Level Designer)   — Idea 6: level timer countdown.
    /// Team 4  (Lead Game Designer) — Idea 6: optional timer mode.
    /// Team 1  (Game Director)    — feeds remaining time into CourseClearScene grade.
    /// </summary>
    public sealed class LevelTimer
    {
        /// <summary>Seconds remaining on the clock.  0 = time expired.</summary>
        public int  SecondsLeft  { get; private set; }

        /// <summary>True once the timer has reached zero.</summary>
        public bool TimeExpired  => SecondsLeft <= 0;

        /// <summary>True when under 60 seconds (flashing warning like SMB3).</summary>
        public bool IsWarning    => SecondsLeft <= 60 && SecondsLeft > 0;

        /// <summary>True when under 20 seconds (urgent danger zone).</summary>
        public bool IsDanger     => SecondsLeft <= 20 && SecondsLeft > 0;

        private float _fractional;  // sub-second accumulator
        private bool  _running;

        /// <param name="seconds">Starting time in seconds.</param>
        public LevelTimer(int seconds) { SecondsLeft = seconds; }

        /// <summary>Starts or resumes the countdown.</summary>
        public void Start() => _running = true;

        /// <summary>Pauses the countdown (e.g. during boss telegraph).</summary>
        public void Pause() => _running = false;

        /// <summary>Advances the timer by dt seconds.</summary>
        public void Update(float dt)
        {
            if (!_running || SecondsLeft <= 0) return;
            _fractional += dt;
            while (_fractional >= 1f)
            {
                _fractional -= 1f;
                SecondsLeft = Math.Max(0, SecondsLeft - 1);
            }
        }

        /// <summary>Draws the SMB3-style TIME display on the HUD.</summary>
        public void Draw(Graphics g, int x, int y)
        {
            Color textColor = IsDanger  ? (int)(Environment.TickCount / 200) % 2 == 0 ? Color.Red : Color.White
                            : IsWarning ? Color.OrangeRed
                            :             Color.White;

            using (var f = new Font("Courier New", 14, FontStyle.Bold))
            {
                g.DrawString("TIME", f, Brushes.Gray,  x, y);
                using (var br = new SolidBrush(textColor))
                    g.DrawString($"{SecondsLeft,4}", f, br, x, y + 18);
            }
        }
    }

    // ── Star Coin ─────────────────────────────────────────────────────────────

    /// <summary>
    /// A collectible star coin hidden in each level (3 per level like SMB3).
    ///
    /// Team 5 (Level Designer) — Idea 7: 3 star coins per level collectible.
    /// Team 17 (VFX Artist)    — Idea 1: coin collection sparkle burst.
    /// </summary>
    public sealed class StarCoin
    {
        public float X    { get; }
        public float Y    { get; private set; }
        public bool  Collected { get; private set; }
        public int   Id   { get; }  // 1, 2, or 3

        private float _bobTimer;
        private float _glowTimer;

        public StarCoin(float x, float y, int id) { X = x; Y = y; Id = id; }

        public void Update(float dt, Player player)
        {
            if (Collected) return;

            _bobTimer += dt;
            // Gentle bob animation.
            Y = (float)(Y + Math.Sin(_bobTimer * 3.0) * 0.3);

            // Collect if player overlaps.
            var pb = new Rectangle((int)player.X, (int)player.Y, player.Width, player.Height);
            if (pb.IntersectsWith(new Rectangle((int)X - 10, (int)Y - 10, 20, 20)))
            {
                Collect(player);
            }

            if (_glowTimer > 0f) _glowTimer -= dt;
        }

        private void Collect(Player player)
        {
            Collected = true;
            _glowTimer = 0.5f;
            Game.Instance.PlayerBounty += 200;
            Game.Instance.FloatingText.Spawn($"★ STAR COIN {Id}", (int)X, (int)Y - 20, Color.Gold);
            SessionStats.Instance.RecordBerry(1);
            AchievementSystem.Grant("ach_star_coin");
        }

        public void Draw(Graphics g, float cameraX)
        {
            if (Collected) return;
            float sx = X - cameraX;
            float sy = Y;

            // Glow aura when freshly collected.
            if (_glowTimer > 0f)
            {
                float alpha = _glowTimer / 0.5f;
                using (var br = new SolidBrush(Color.FromArgb((int)(150 * alpha), Color.Gold)))
                    g.FillEllipse(br, sx - 14, sy - 14, 28, 28);
            }

            // Outer ring.
            using (var pen = new Pen(Color.FromArgb(220, 200, 160, 0), 3))
                g.DrawEllipse(pen, sx - 9, sy - 9, 18, 18);

            // Star shape (simple 5-point approximation using filled ellipse + star lines).
            using (var br = new SolidBrush(Color.FromArgb(240, 255, 215, 0)))
                g.FillEllipse(br, sx - 6, sy - 6, 12, 12);

            using (var f = new Font("Courier New", 7, FontStyle.Bold))
                g.DrawString($"★{Id}", f, Brushes.DarkOrange, sx - 8, sy - 5);
        }
    }
}
