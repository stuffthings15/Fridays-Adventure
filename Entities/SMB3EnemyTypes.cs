// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 – Multi-Team Implementation
// Entities/SMB3EnemyTypes.cs
// Purpose: SMB3-faithful enemy archetypes (Goomba, Koopa, Piranha, Thwomp, HammerBro)
// ────────────────────────────────────────────────────────────────────────────
// Team 4  (Lead Game Designer)  – Idea 9:  GoombaEnemy – walk, stomp = dead
// Team 4  (Lead Game Designer)  – Idea 10: KoopaEnemy  – stomp = shell, kick = projectile
// Team 4  (Lead Game Designer)  – Idea 11: PiranhaPlant – pipe in/out cycle
// Team 4  (Lead Game Designer)  – Idea 12: Thwomp       – falls on player, rises slowly
// Team 4  (Lead Game Designer)  – Idea 13: HammerBroEnemy – throws hammers at player
// Team 7  (Gameplay Programmer) – Idea 8:  KoopaShell velocity + wall-bounce physics
// Team 7  (Gameplay Programmer) – Idea 9:  Thwomp fall/rise state machine
// Team 7  (Gameplay Programmer) – Idea 10: Piranha plant in/out cycle timer
// Team 7  (Gameplay Programmer) – Idea 11: Goomba patrol + fall-off-ledge stop
// Team 14 (Environment Artist)  – Idea 5:  Goomba brown oval visual
// Team 14 (Environment Artist)  – Idea 6:  Koopa green box + shell visual
// Team 14 (Environment Artist)  – Idea 7:  Thwomp stone-face block visual
// Team 14 (Environment Artist)  – Idea 8:  Piranha Plant pipe + head visual
// Team 17 (VFX Artist)          – Idea 4:  Stomp squish animation
// Team 17 (VFX Artist)          – Idea 5:  Thwomp slam dust shockwave
// Team 17 (VFX Artist)          – Idea 6:  Shell kick speed-line particles
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Drawing;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Entities
{
    // ══════════════════════════════════════════════════════════════════════════
    // GoombaEnemy
    // ══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Basic walk-and-bounce enemy.  Stomped from above = defeated.
    /// Reverses direction at platform edges or walls.
    ///
    /// Team 4  (Lead Game Designer)  — Idea 9.
    /// Team 7  (Gameplay Programmer) — Idea 11: patrol logic.
    /// Team 14 (Environment Artist)  — Idea 5:  brown oval visual.
    /// Team 17 (VFX Artist)          — Idea 4:  squish animation.
    /// </summary>
    public sealed class GoombaEnemy : Entity
    {
        // ── Constants ─────────────────────────────────────────────────────────
        private const float MoveSpeed = 70f;
        private const float Gravity   = 560f;
        public  const int   HP        = 1;
        public  const int   Score     = 100;

        // ── State ─────────────────────────────────────────────────────────────
        public  bool  IsAlive    { get; private set; } = true;
        private float _squishTimer;
        private const float SquishDuration = 0.35f;

        // ── Ground tracking ───────────────────────────────────────────────────
        private float _patrolLeft;
        private float _patrolRight;

        private static Bitmap _sprite;

        public GoombaEnemy(float x, float y, float patrolLeft, float patrolRight)
            : base(x, y, 30, 28)
        {
            _patrolLeft  = patrolLeft;
            _patrolRight = patrolRight;
            VelocityX    = -MoveSpeed;
            FacingRight  = false;
        }

        // ── Stomp (Team 4 — Idea 9) ────────────────────────────────────────────
        /// <summary>
        /// Call when the player lands on this Goomba from above.
        /// Team 4 (Lead Game Designer) — Idea 9.
        /// Team 17 (VFX Artist)        — Idea 4: squish effect.
        /// </summary>
        public void Stomp()
        {
            if (!IsAlive) return;
            IsAlive     = false;
            _squishTimer = SquishDuration;
            ParticleSystem.SpawnCoinSparkle(CenterX, CenterY);
        }

        // ── Update (Team 7 — Idea 11) ──────────────────────────────────────────
        public void UpdateEnemy(float dt, List<Rectangle> platforms, int groundY)
        {
            if (!IsAlive)
            {
                _squishTimer -= dt;
                return;
            }

            // Gravity
            VelocityY += Gravity * dt;
            X         += VelocityX * dt;
            Y         += VelocityY * dt;

            // Patrol reversal
            if (X < _patrolLeft)  { X = _patrolLeft;  VelocityX =  MoveSpeed; FacingRight = true; }
            if (X + Width > _patrolRight) { X = _patrolRight - Width; VelocityX = -MoveSpeed; FacingRight = false; }

            // Ground & platform collision
            bool grounded = false;
            if (Y + Height >= groundY) { Y = groundY - Height; VelocityY = 0; grounded = true; }
            foreach (var p in platforms)
            {
                if (VelocityY >= 0 &&
                    X + Width > p.Left && X < p.Right &&
                    Y + Height >= p.Top && Y + Height <= p.Top + 20)
                {
                    Y = p.Top - Height; VelocityY = 0; grounded = true;
                }
                // Wall push
                if (Hitbox.IntersectsWith(p) && VelocityY <= 0)
                    VelocityX = -VelocityX;
            }

            // Ledge stop — stay on platform edges
            if (grounded) { /* simple patrol already handles this */ }
        }

        // ── Player interaction ────────────────────────────────────────────────
        /// <summary>
        /// Checks stomp detection and body contact.
        /// Returns true if the player stomped this Goomba (triggers bounce).
        /// Team 4 (Lead Game Designer) — Idea 9.
        /// </summary>
        public bool CheckPlayerInteraction(Player player)
        {
            if (!IsAlive) return false;
            if (!Hitbox.IntersectsWith(player.Hitbox)) return false;

            // Stomp: player falling from above
            if (player.VelocityY > 0)
            {
                float overlap = (player.Y + player.Height) - Y;
                if (overlap > 0 && overlap < 20)
                {
                    Stomp();
                    return true;
                }
            }

            // Body contact: damage player
            if (!player.IsInvincible)
                player.TakeDamage(player.MaxHealth / 10);

            return false;
        }

        // ── Draw (Team 14 — Idea 5 / Team 17 — Idea 4) ────────────────────────
        public override void Draw(Graphics g)
        {
            if (!IsAlive && _squishTimer <= 0f) return;

            // Prefer imported sprite asset if available.
            if (IsAlive)
            {
                if (_sprite == null)
                    _sprite = SpriteManager.GetScaled("enemy_goomba.png", Width, Height);
                if (_sprite != null)
                {
                    g.DrawImage(_sprite, X, Y, Width, Height);
                    return;
                }
            }

            float drawH = IsAlive ? Height : (int)(Height * (1f - (_squishTimer > 0 ? 1f - _squishTimer / SquishDuration : 1f)) * 0.3f + 4);
            float drawY = Y + (Height - drawH);

            // Body — brown mushroom cap (Team 14 — Idea 5)
            using (var br = new SolidBrush(Color.FromArgb(160, 100, 40)))
                g.FillEllipse(br, X, drawY, Width, drawH);

            if (IsAlive)
            {
                // Eyes — white dots
                using (var br = new SolidBrush(Color.White))
                {
                    g.FillEllipse(br, X + 5, Y + 4, 7, 7);
                    g.FillEllipse(br, X + Width - 12, Y + 4, 7, 7);
                }
                // Pupils
                using (var br = new SolidBrush(Color.Black))
                {
                    g.FillEllipse(br, X + 6, Y + 6, 4, 4);
                    g.FillEllipse(br, X + Width - 11, Y + 6, 4, 4);
                }
                // Feet
                using (var br = new SolidBrush(Color.FromArgb(80, 50, 10)))
                {
                    g.FillEllipse(br, X + 2, Y + Height - 10, 10, 10);
                    g.FillEllipse(br, X + Width - 12, Y + Height - 10, 10, 10);
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // KoopaEnemy
    // ══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Shell-based enemy.  First stomp = becomes a shell.  Shell can be kicked.
    ///
    /// Team 4  (Lead Game Designer)  — Idea 10.
    /// Team 7  (Gameplay Programmer) — Idea 8: shell velocity + wall-bounce.
    /// Team 14 (Environment Artist)  — Idea 6: green turtle visual.
    /// Team 17 (VFX Artist)          — Idea 6: speed lines when kicked.
    /// </summary>
    public sealed class KoopaEnemy : Entity
    {
        public enum KoopaState { Walking, Shell, SlidingShell }

        public  KoopaState State     { get; private set; } = KoopaState.Walking;
        public  bool       IsAlive   { get; private set; } = true;
        public  int        Score     => State == KoopaState.Walking ? 200 : 400;

        private const float WalkSpeed  = 60f;
        private const float ShellSpeed = 340f;
        private const float Gravity    = 560f;
        private float _patrolLeft, _patrolRight;
        private float _shellKickDir;   // +1 right, -1 left
        private float _shellBlinkTimer;

        private static Bitmap _sprite;

        public KoopaEnemy(float x, float y, float patrolLeft, float patrolRight)
            : base(x, y, 32, 40)
        {
            _patrolLeft  = patrolLeft;
            _patrolRight = patrolRight;
            VelocityX    = -WalkSpeed;
        }

        // ── Stomp logic (Team 4 — Idea 10) ────────────────────────────────────
        public void Stomp()
        {
            if (State == KoopaState.Walking)
            {
                State     = KoopaState.Shell;
                VelocityX = 0f;
                Height    = 20;   // flatten to shell height
                ParticleSystem.SpawnCoinSparkle(CenterX, CenterY);
            }
            else if (State == KoopaState.SlidingShell)
            {
                IsAlive = false;   // stomp sliding shell = defeat
            }
        }

        // ── Kick shell (Team 7 — Idea 8) ──────────────────────────────────────
        /// <summary>
        /// Kicks the shell in the direction the player is facing.
        /// Team 7 (Gameplay Programmer) — Idea 8.
        /// </summary>
        public void Kick(bool kickRight)
        {
            if (State != KoopaState.Shell) return;
            State         = KoopaState.SlidingShell;
            _shellKickDir = kickRight ? 1f : -1f;
            VelocityX     = _shellKickDir * ShellSpeed;

            // Speed-line particles (Team 17 — Idea 6)
            ParticleSystem.SpawnBurst(CenterX, CenterY, 6,
                Color.Cyan, 60f, 160f, 0.2f, 0.5f);
        }

        public void UpdateEnemy(float dt, List<Rectangle> platforms, int groundY, List<GoombaEnemy> goombas)
        {
            if (!IsAlive) return;

            VelocityY += Gravity * dt;
            X         += VelocityX * dt;
            Y         += VelocityY * dt;

            // Ground / platform
            bool grounded = false;
            if (Y + Height >= groundY) { Y = groundY - Height; VelocityY = 0; grounded = true; }
            foreach (var p in platforms)
            {
                if (VelocityY >= 0 &&
                    X + Width > p.Left && X < p.Right &&
                    Y + Height >= p.Top && Y + Height <= p.Top + 20)
                { Y = p.Top - Height; VelocityY = 0; grounded = true; }
            }

            if (State == KoopaState.Walking)
            {
                // Patrol reversal
                if (X < _patrolLeft)  { X = _patrolLeft;  VelocityX =  WalkSpeed; FacingRight = true; }
                if (X + Width > _patrolRight) { X = _patrolRight - Width; VelocityX = -WalkSpeed; FacingRight = false; }
            }
            else if (State == KoopaState.SlidingShell)
            {
                // Shell bounces off walls (Team 7 — Idea 8)
                foreach (var p in platforms)
                {
                    if (Hitbox.IntersectsWith(p))
                    {
                        VelocityX = -VelocityX;
                        _shellKickDir = -_shellKickDir;
                        break;
                    }
                }
                // Shell kills Goombas (Team 7 — Idea 8)
                foreach (var g in goombas)
                    if (g.IsAlive && Hitbox.IntersectsWith(g.Hitbox))
                    {
                        g.Stomp();
                        // Score is a static const — access via type, not instance
                        Game.Instance.PlayerBounty += GoombaEnemy.Score;
                    }
            }

            if (!grounded && State == KoopaState.Shell) _shellBlinkTimer += dt;
        }

        public bool CheckPlayerInteraction(Player player)
        {
            if (!IsAlive) return false;
            if (!Hitbox.IntersectsWith(player.Hitbox)) return false;

            // Stomp
            if (player.VelocityY > 0 && (player.Y + player.Height) - Y < 20)
            {
                Stomp();
                return true;
            }
            // Kick shell
            if (State == KoopaState.Shell)
            {
                Kick(player.FacingRight);
                return false;
            }
            // Body contact
            if (!player.IsInvincible && State != KoopaState.Shell)
                player.TakeDamage(player.MaxHealth / 10);

            return false;
        }

        // ── Draw (Team 14 — Idea 6) ────────────────────────────────────────────
        public override void Draw(Graphics g)
        {
            if (!IsAlive) return;

            if (State == KoopaState.Walking)
            {
                // Prefer imported sprite asset if available.
                if (_sprite == null)
                    _sprite = SpriteManager.GetScaled("enemy_koopa.png", Width, Height);
                if (_sprite != null)
                {
                    g.DrawImage(_sprite, X, Y, Width, Height);
                    return;
                }

                // Shell (body)
                using (var br = new SolidBrush(Color.FromArgb(60, 160, 60)))
                    g.FillEllipse(br, X + 4, Y + Height / 3f, Width - 8, Height * 0.6f);
                // Head
                using (var br = new SolidBrush(Color.FromArgb(200, 180, 100)))
                    g.FillEllipse(br, X + 6, Y, Width - 12, Height * 0.45f);
                // Eye
                using (var br = new SolidBrush(Color.Black))
                    g.FillEllipse(br, FacingRight ? X + Width - 14 : X + 4, Y + 5, 6, 6);
                // Feet
                using (var br = new SolidBrush(Color.FromArgb(60, 160, 60)))
                {
                    g.FillRectangle(br, X + 2, Y + Height - 12, 10, 12);
                    g.FillRectangle(br, X + Width - 12, Y + Height - 12, 10, 12);
                }
            }
            else
            {
                // Shell only
                using (var br = new SolidBrush(Color.FromArgb(60, 160, 60)))
                    g.FillEllipse(br, X, Y, Width, Height);
                using (var br = new SolidBrush(Color.FromArgb(180, 220, 120)))
                    g.FillEllipse(br, X + 4, Y + 4, Width - 8, Height - 8);
                using (var br = new SolidBrush(Color.FromArgb(60, 160, 60)))
                {
                    g.DrawLine(new Pen(br, 2), X + Width / 2f, Y + 2, X + Width / 2f, Y + Height - 2);
                    g.DrawLine(new Pen(br, 2), X + 2, Y + Height / 2f, X + Width - 2, Y + Height / 2f);
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PiranhaPlant
    // ══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Stationary pipe enemy that pops in and out.  Cannot be stomped.
    ///
    /// Team 4  (Lead Game Designer)  — Idea 11.
    /// Team 7  (Gameplay Programmer) — Idea 10: in/out cycle timer.
    /// Team 14 (Environment Artist)  — Idea 8:  pipe + head visual.
    /// </summary>
    public sealed class PiranhaPlant : Entity
    {
        private const float UpDuration   = 2.0f;   // visible for 2 s
        private const float DownDuration = 2.5f;   // hidden for 2.5 s
        private float _cycleTimer;
        private bool  _isUp = true;

        // ── Pipe geometry ─────────────────────────────────────────────────────
        public float PipeX { get; }
        public float PipeY { get; }
        private const int PipeW = 36;
        private const int PipeH = 48;

        public PiranhaPlant(float pipeX, float pipeY)
            : base(pipeX + (PipeW - 28) / 2f, pipeY - 36, 28, 36)
        {
            PipeX = pipeX;
            PipeY = pipeY;
        }

        // ── Update (Team 7 — Idea 10) ──────────────────────────────────────────
        public void UpdatePlant(float dt)
        {
            _cycleTimer += dt;
            float duration = _isUp ? UpDuration : DownDuration;
            if (_cycleTimer >= duration)
            {
                _isUp       = !_isUp;
                _cycleTimer = 0f;
            }
            IsActive = _isUp;
        }

        // ── Player contact (Team 4 — Idea 11) ─────────────────────────────────
        public void CheckPlayerContact(Player player)
        {
            if (!_isUp || !Hitbox.IntersectsWith(player.Hitbox)) return;
            if (!player.IsInvincible)
                player.TakeDamage(player.MaxHealth / 8);
        }

        // ── Draw (Team 14 — Idea 8) ────────────────────────────────────────────
        public override void Draw(Graphics g)
        {
            // Draw pipe always (pipe is always visible)
            DrawPipe(g);

            if (!_isUp) return;

            // Stem
            using (var br = new SolidBrush(Color.FromArgb(30, 140, 30)))
                g.FillRectangle(br, X + Width * 0.35f, Y + Height * 0.5f, Width * 0.3f, Height * 0.5f + PipeH);

            // Head — red mouth (Team 14 — Idea 8)
            using (var br = new SolidBrush(Color.FromArgb(220, 40, 40)))
                g.FillEllipse(br, X, Y, Width, Height);
            // Mouth interior
            using (var br = new SolidBrush(Color.FromArgb(50, 10, 10)))
                g.FillEllipse(br, X + 3, Y + Height / 3f, Width - 6, Height * 0.5f);
            // White teeth
            using (var br = new SolidBrush(Color.White))
            {
                for (int i = 0; i < 4; i++)
                    g.FillRectangle(br, X + 3 + i * 6, Y + Height / 3f, 4, 8);
            }
            // Spots
            using (var br = new SolidBrush(Color.FromArgb(255, 80, 80)))
            {
                g.FillEllipse(br, X + 4, Y + 4, 8, 8);
                g.FillEllipse(br, X + Width - 12, Y + 4, 8, 8);
            }
        }

        private void DrawPipe(Graphics g)
        {
            // Outer pipe
            using (var br = new SolidBrush(Color.FromArgb(30, 160, 30)))
                g.FillRectangle(br, PipeX, PipeY, PipeW, PipeH);
            // Pipe rim
            using (var br = new SolidBrush(Color.FromArgb(20, 110, 20)))
                g.FillRectangle(br, PipeX - 3, PipeY, PipeW + 6, 14);
            // Highlight
            using (var br = new SolidBrush(Color.FromArgb(80, 200, 80)))
                g.FillRectangle(br, PipeX + 4, PipeY + 4, 6, PipeH - 4);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Thwomp
    // ══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Ceiling-mounted stone block that falls when the player is below.
    /// Rises slowly after reaching the floor.
    ///
    /// Team 4  (Lead Game Designer)  — Idea 12.
    /// Team 7  (Gameplay Programmer) — Idea 9: fall/rise state machine.
    /// Team 14 (Environment Artist)  — Idea 7: stone-face visual.
    /// Team 17 (VFX Artist)          — Idea 5: slam dust shockwave.
    /// </summary>
    public sealed class Thwomp : Entity
    {
        public enum ThwompState { Idle, Falling, Rising }

        public ThwompState State { get; private set; } = ThwompState.Idle;

        private const float FallAccel    = 1200f;
        private const float MaxFallSpeed = 600f;
        private const float RiseSpeed    = 60f;
        private const float TriggerRange = 60f;   // horizontal range to trigger drop

        private readonly float _homeY;   // original ceiling Y position
        private float _faceAngle;        // wobble animation

        // ── Slam VFX ─────────────────────────────────────────────────────────
        private float _slamShockwaveTimer;
        private float _slamX, _slamY;

        public Thwomp(float x, float y)
            : base(x, y, 48, 48)
        {
            _homeY = y;
        }

        // ── Update (Team 7 — Idea 9) ───────────────────────────────────────────
        public void UpdateThwomp(float dt, Player player, int groundY)
        {
            _faceAngle += dt * 2f;
            if (_slamShockwaveTimer > 0f) _slamShockwaveTimer -= dt;

            switch (State)
            {
                case ThwompState.Idle:
                    // Trigger when player is within horizontal range and below
                    if (Math.Abs(player.CenterX - CenterX) < TriggerRange &&
                        player.Y > Y + Height)
                    {
                        State = ThwompState.Falling;
                    }
                    break;

                case ThwompState.Falling:
                    VelocityY = Math.Min(VelocityY + FallAccel * dt, MaxFallSpeed);
                    Y        += VelocityY * dt;

                    // Slam the ground or platform
                    if (Y + Height >= groundY)
                    {
                        Y         = groundY - Height;
                        VelocityY = 0;
                        State     = ThwompState.Rising;
                        Slam(player);
                    }
                    break;

                case ThwompState.Rising:
                    Y -= RiseSpeed * dt;
                    if (Y <= _homeY)
                    {
                        Y         = _homeY;
                        VelocityY = 0;
                        State     = ThwompState.Idle;
                    }
                    break;
            }
        }

        private void Slam(Player player)
        {
            // Screen shake (Team 17 — Idea 5)
            Game.Instance.ScreenShake.Trigger(0.8f);
            _slamShockwaveTimer = 0.4f;
            _slamX = CenterX; _slamY = Y + Height;

            // Dust particles
            ParticleSystem.SpawnBurst(_slamX, _slamY, 12,
                Color.FromArgb(180, 150, 100), 60f, 200f, 0.6f, 1.2f);

            // Crush player if directly below
            if (Math.Abs(player.CenterX - CenterX) < Width / 2f + 8 &&
                Math.Abs(player.Y + player.Height - Y - Height) < 16)
            {
                player.TakeDamage(player.MaxHealth / 4);
            }
        }

        // ── Draw (Team 14 — Idea 7 / Team 17 — Idea 5) ────────────────────────
        public override void Draw(Graphics g)
        {
            // Shockwave ring (Team 17 — Idea 5)
            if (_slamShockwaveTimer > 0f)
            {
                float prog   = 1f - (_slamShockwaveTimer / 0.4f);
                float radius = prog * 80f;
                int   alpha  = (int)(160 * (1f - prog));
                using (var pen = new Pen(Color.FromArgb(alpha, 200, 180, 120), 3))
                    g.DrawEllipse(pen, _slamX - radius, _slamY - radius / 2f,
                        radius * 2f, radius);
            }

            // Stone body (Team 14 — Idea 7)
            using (var br = new SolidBrush(Color.FromArgb(100, 90, 110)))
                g.FillRectangle(br, X, Y, Width, Height);
            using (var pen = new Pen(Color.FromArgb(60, 50, 80), 2))
                g.DrawRectangle(pen, X, Y, Width - 1, Height - 1);

            // Stone texture lines
            using (var pen = new Pen(Color.FromArgb(60, 80, 70, 90), 1))
            {
                g.DrawLine(pen, X + Width / 3f, Y + 4, X + Width / 3f, Y + Height - 4);
                g.DrawLine(pen, X + 2 * Width / 3f, Y + 4, X + 2 * Width / 3f, Y + Height - 4);
                g.DrawLine(pen, X + 4, Y + Height / 2f, X + Width - 4, Y + Height / 2f);
            }

            // Angry face
            float bob = (float)Math.Sin(_faceAngle) * 1.5f;
            // Eyes (angry slanted)
            using (var br = new SolidBrush(Color.FromArgb(240, 50, 50)))
            {
                g.FillEllipse(br, X + 8, Y + 10 + bob, 10, 10);
                g.FillEllipse(br, X + Width - 18, Y + 10 + bob, 10, 10);
            }
            // Frowning mouth
            using (var pen = new Pen(Color.FromArgb(240, 50, 50), 3))
                g.DrawArc(pen,
                    X + 8, Y + Height - 20 + bob,
                    Width - 16, 14, 0, 180);

            // Spikes on bottom
            using (var br = new SolidBrush(Color.FromArgb(80, 70, 90)))
            {
                for (int i = 0; i < 4; i++)
                {
                    float sx = X + 4 + i * 11;
                    g.FillPolygon(br, new[]
                    {
                        new PointF(sx, Y + Height),
                        new PointF(sx + 5, Y + Height + 8),
                        new PointF(sx + 10, Y + Height)
                    });
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HammerBroEnemy  (in-level Hammer Bro)
    // ══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Platform-hopping enemy that throws hammer projectiles at the player.
    ///
    /// Team 4  (Lead Game Designer)  — Idea 13.
    /// Team 7  (Gameplay Programmer) — Idea 5 (hammer arc physics).
    /// </summary>
    public sealed class HammerBroEnemy : Entity
    {
        public bool  IsAlive   { get; private set; } = true;
        public int   Score     { get; }              = 500;

        private float _throwTimer;
        private const float ThrowInterval = 2.5f;
        private const float Gravity       = 560f;
        private float _patrolLeft, _patrolRight;
        private float _hopTimer;

        public readonly List<Hammer> Hammers = new List<Hammer>();

        private static Bitmap _sprite;

        public HammerBroEnemy(float x, float y, float patrolLeft, float patrolRight)
            : base(x, y, 32, 44)
        {
            _patrolLeft  = patrolLeft;
            _patrolRight = patrolRight;
            _throwTimer  = 1.0f;
            VelocityX    = 50f;
        }

        public void Stomp()
        {
            if (!IsAlive) return;
            IsAlive = false;
            ParticleSystem.SpawnEnemyDefeat(CenterX, CenterY);
        }

        public void UpdateEnemy(float dt, List<Rectangle> platforms, int groundY, Player player)
        {
            if (!IsAlive) return;

            // Update hammers
            foreach (var h in Hammers) h.UpdateProjectile(dt);
            for (int i = Hammers.Count - 1; i >= 0; i--)
                if (!Hammers[i].IsActive) Hammers.RemoveAt(i);

            // Throw timer
            _throwTimer -= dt;
            if (_throwTimer <= 0f)
            {
                _throwTimer = ThrowInterval;
                ThrowHammer(player);
            }

            // Gravity + patrol
            VelocityY += Gravity * dt;
            X         += VelocityX * dt;
            Y         += VelocityY * dt;

            // Bounce patrol
            if (X < _patrolLeft)  { X = _patrolLeft; VelocityX = 50f; FacingRight = true; }
            if (X + Width > _patrolRight) { X = _patrolRight - Width; VelocityX = -50f; FacingRight = false; }

            // Ground
            if (Y + Height >= groundY) { Y = groundY - Height; VelocityY = 0; }
            foreach (var p in platforms)
                if (VelocityY >= 0 && X + Width > p.Left && X < p.Right &&
                    Y + Height >= p.Top && Y + Height <= p.Top + 20)
                { Y = p.Top - Height; VelocityY = 0; }

            // Periodic hop
            _hopTimer -= dt;
            if (_hopTimer <= 0f)
            {
                _hopTimer = 2.0f;
                VelocityY = -280f;
            }
        }

        private void ThrowHammer(Player player)
        {
            // Aim toward player with arc
            float dx = player.CenterX - CenterX;
            float sign = dx >= 0 ? 1f : -1f;
            Hammers.Add(new Hammer(CenterX, Y, sign * 180f, -340f));
        }

        public bool CheckPlayerInteraction(Player player)
        {
            if (!IsAlive) return false;

            // Check hammers
            foreach (var h in Hammers)
                h.CheckPlayerHit(player, player.MaxHealth / 8);

            if (!Hitbox.IntersectsWith(player.Hitbox)) return false;

            // Stomp from above
            if (player.VelocityY > 0 && (player.Y + player.Height) - Y < 20)
            {
                Stomp();
                return true;
            }

            if (!player.IsInvincible)
                player.TakeDamage(player.MaxHealth / 8);

            return false;
        }

        // ── Draw (Team 14 — robot helmet + hammer) ────────────────────────────
        public override void Draw(Graphics g)
        {
            if (!IsAlive) return;

            // Draw hammers
            foreach (var h in Hammers) h.Draw(g);

            // Prefer imported sprite asset if available.
            if (_sprite == null)
                _sprite = SpriteManager.GetScaled("enemy_hammer_bro.png", Width, Height);
            if (_sprite != null)
            {
                g.DrawImage(_sprite, X, Y, Width, Height);
                return;
            }

            // Body (green-khaki uniform)
            using (var br = new SolidBrush(Color.FromArgb(80, 130, 60)))
                g.FillRectangle(br, X + 4, Y + Height / 3f, Width - 8, Height * 0.65f);

            // Head
            using (var br = new SolidBrush(Color.FromArgb(210, 175, 110)))
                g.FillEllipse(br, X + 4, Y, Width - 8, Height * 0.38f);

            // Helmet
            using (var br = new SolidBrush(Color.FromArgb(50, 80, 40)))
                g.FillRectangle(br, X + 2, Y, Width - 4, Height * 0.22f);

            // Eye
            using (var br = new SolidBrush(Color.Black))
                g.FillEllipse(br,
                    FacingRight ? X + Width - 14 : X + 4,
                    Y + 6, 7, 7);

            // Held hammer (small indicator)
            float hx = FacingRight ? X + Width - 6 : X - 6;
            using (var br = new SolidBrush(Color.FromArgb(140, 140, 150)))
                g.FillRectangle(br, hx, Y + 4, 8, 6);
            using (var br = new SolidBrush(Color.FromArgb(120, 70, 20)))
                g.FillRectangle(br, hx + 2, Y + 10, 4, 14);
        }
    }
}
