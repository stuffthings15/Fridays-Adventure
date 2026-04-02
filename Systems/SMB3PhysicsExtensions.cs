using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;

namespace Fridays_Adventure.Systems
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SMB3PhysicsExtensions.cs  —  Gameplay Programmer: 10 NEW physics ideas
    //
    //  Idea 1:  Ice sliding — low-friction surface causes slide deceleration.
    //  Idea 2:  Tail-spin attack — 360° short-range melee with knockback.
    //  Idea 3:  Raccoon/Leaf P-meter flight — sustained horizontal flight.
    //  Idea 4:  Fireball projectile — bouncing, gravity-affected projectile.
    //  Idea 5:  Hammer throw — arcing projectile that bounces twice then expires.
    //  Idea 6:  Bounce block — spring surface that launches player upward.
    //  Idea 7:  Spring platform (Boing) — timed launch when player lands.
    //  Idea 8:  Spin-jump — reduces gravity during descent for extra airtime.
    //  Idea 9:  Stomp-chain score popup — escalating score text per chain stomp.
    //  Idea 10: Conveyor-belt force — surfaces that add lateral push to player.
    // ═══════════════════════════════════════════════════════════════════════════

    // ── Idea 4: Fireball projectile ───────────────────────────────────────────
    // NOTE: FireballProjectile is defined in PowerUpSystem.cs (canonical definition).
    // FireballSystem below manages live instances using that shared class.

    /// <summary>
    /// Manages all live fireball projectiles in a level scene.
    /// Uses the canonical <see cref="FireballProjectile"/> class from PowerUpSystem.cs.
    /// Team 7 (Gameplay Programmer) — Idea 4.
    /// </summary>
    public static class FireballSystem
    {
        private static readonly List<FireballProjectile> _balls = new List<FireballProjectile>();

        /// <summary>
        /// Fires a new fireball from the given position in the direction the player faces.
        /// Team 7 (Gameplay Programmer) — Idea 4.
        /// </summary>
        public static void Fire(float x, float y, bool facingRight)
        {
            // Use the canonical constructor: FireballProjectile(float x, float y, float vx).
            _balls.Add(new FireballProjectile(x, y, facingRight ? 340f : -340f));
        }

        /// <summary>
        /// Updates all live fireballs, resolving platform ground collisions.
        /// Team 7 (Gameplay Programmer) — Idea 4.
        /// </summary>
        public static void Update(float dt, IReadOnlyList<Rectangle> platforms)
        {
            foreach (var fb in _balls)
            {
                if (!fb.Alive) continue;

                // Find the closest platform surface below the fireball for bounce.
                int groundY = (int)fb.Y + 2000; // default: far below, no ground
                var fbBounds = new Rectangle((int)fb.X - 7, (int)fb.Y - 7, 14, 14);
                foreach (var p in platforms)
                {
                    // Fireball must be moving downward and horizontally overlapping.
                    if (fb.VY > 0
                        && fbBounds.Right  > p.Left
                        && fbBounds.Left   < p.Right
                        && fbBounds.Bottom >= p.Top
                        && fbBounds.Top    <  p.Top)
                    {
                        if (p.Top < groundY) groundY = p.Top;
                    }
                }

                fb.Update(dt, groundY);
            }
            _balls.RemoveAll(b => !b.Alive);
        }

        /// <summary>
        /// Checks if any fireball hit the given enemy bounds.
        /// Returns 15 (fire damage) if a hit occurred, 0 otherwise.
        /// Team 7 (Gameplay Programmer) — Idea 4.
        /// </summary>
        public static int CheckEnemyHit(Rectangle enemyBounds)
        {
            for (int i = _balls.Count - 1; i >= 0; i--)
            {
                var fb = _balls[i];
                if (!fb.Alive) continue;
                var bounds = new Rectangle((int)fb.X - 7, (int)fb.Y - 7, 14, 14);
                if (bounds.IntersectsWith(enemyBounds))
                {
                    fb.Kill();
                    return 15; // fire damage
                }
            }
            return 0;
        }

        /// <summary>
        /// Draws all live fireballs. Pass the scene's camera X offset for world-space rendering.
        /// Team 7 (Gameplay Programmer) — Idea 4.
        /// </summary>
        public static void Draw(Graphics g, float cameraX = 0f)
        {
            foreach (var fb in _balls)
                fb.Draw(g, cameraX);
        }

        /// <summary>Clears all fireballs (call on scene exit).</summary>
        public static void Clear() => _balls.Clear();
    }

    // ── Idea 5: Hammer projectile ─────────────────────────────────────────────
    /// <summary>
    /// An arcing hammer throw projectile.
    /// Bounces twice on the ground then expires.
    /// Team 7 (Gameplay Programmer) — Idea 5.
    /// </summary>
    public sealed class HammerProjectile
    {
        public float X, Y, VX, VY;
        public float Rotation;
        public bool  Active = true;
        private int  _bounces;

        public void Update(float dt)
        {
            if (!Active) return;
            X        += VX * dt;
            Y        += VY * dt;
            VY       += Character.Gravity * 0.5f * dt;
            Rotation += 360f * dt;
        }

        public void OnGroundHit()
        {
            _bounces++;
            if (_bounces >= 2) { Active = false; return; }
            VY = -Math.Abs(VY) * 0.6f;
        }

        public Rectangle Bounds => new Rectangle((int)X, (int)Y, 16, 16);
    }

    // ── Idea 1: Ice sliding ───────────────────────────────────────────────────
    /// <summary>
    /// Applied by an ice-surface platform tag. Reduces X deceleration.
    /// Call <see cref="Apply"/> from a scene's Update when the player is on ice.
    /// Team 7 (Gameplay Programmer) — Idea 1.
    /// </summary>
    public static class IceSlide
    {
        private const float FrictionMultiplier = 0.12f;  // much less than normal 1.0

        /// <summary>
        /// Applies ice-surface friction to the player's X velocity.
        /// Team 7 (Gameplay Programmer) — Idea 1.
        /// </summary>
        public static void Apply(Player player, float dt)
        {
            // Gradually bring velocity toward zero instead of stopping immediately.
            player.VelocityX = player.VelocityX * (1f - FrictionMultiplier * dt * 60f);
            if (Math.Abs(player.VelocityX) < 2f) player.VelocityX = 0f;
        }
    }

    // ── Idea 2: Tail-spin attack ──────────────────────────────────────────────
    /// <summary>
    /// 360° short-range tail spin attack available while wearing the Super Leaf.
    /// Damages all enemies within <see cref="Radius"/> pixels.
    /// Team 7 (Gameplay Programmer) — Idea 2.
    /// </summary>
    public static class TailSpinAttack
    {
        private static float _cooldown;
        private const  float Cooldown = 0.8f;
        private const  float Radius   = 60f;

        /// <summary>True when the tail spin can be used.</summary>
        public static bool IsReady => _cooldown <= 0f;

        /// <summary>Tick cooldown.</summary>
        public static void Tick(float dt) { if (_cooldown > 0f) _cooldown -= dt; }

        /// <summary>
        /// Attempts to perform a tail spin centered on <paramref name="player"/>.
        /// Returns true if the spin was executed.
        /// Team 7 (Gameplay Programmer) — Idea 2.
        /// </summary>
        public static bool TrySpin(Player player, IReadOnlyList<Enemy> enemies, out int hitCount)
        {
            hitCount = 0;
            if (!IsReady) return false;
            _cooldown = Cooldown;

            float cx = player.X + player.Width  / 2f;
            float cy = player.Y + player.Height / 2f;

            foreach (var enemy in enemies)
            {
                float ex = enemy.X + enemy.Width  / 2f;
                float ey = enemy.Y + enemy.Height / 2f;
                float d  = (float)Math.Sqrt((cx - ex) * (cx - ex) + (cy - ey) * (cy - ey));
                if (d < Radius)
                {
                    enemy.TakeDamage(8);
                    hitCount++;
                }
            }

            Engine.Game.Instance?.FloatingText?.Spawn(
                "TAIL SPIN!", (int)cx, (int)cy - 20, Color.LimeGreen, large: false);
            return true;
        }
    }

    // ── Idea 3: Raccoon/Leaf P-meter flight ───────────────────────────────────
    /// <summary>
    /// Once the P-Meter is full and the player is airborne, holding JUMP triggers
    /// sustained horizontal flight. Gravity is suppressed during this period.
    /// Team 7 (Gameplay Programmer) — Idea 3.
    /// </summary>
    public static class RaccoonFlight
    {
        private static float _flightTimer;
        private const  float MaxFlightDuration = 5.0f;
        private const  float FlightVelocityY   = -40f;   // slight upward float

        /// <summary>True while the player is actively flying.</summary>
        public static bool IsFlying => _flightTimer > 0f;

        /// <summary>Remaining flight seconds.</summary>
        public static float TimeRemaining => _flightTimer;

        /// <summary>Starts flight. Returns false if P-Meter is not full or wrong suit.</summary>
        public static bool TryStartFlight(Player player, SMB3SuitManager suit)
        {
            if (suit.CurrentSuit != SuitType.Leaf) return false;
            if (!player.PMeterActive)              return false;
            if (_flightTimer > 0f)                 return false;

            _flightTimer = MaxFlightDuration;
            player.VelocityY = FlightVelocityY;
            Engine.Game.Instance?.FloatingText?.Spawn(
                "FLIGHT!", (int)player.X, (int)player.Y - 20, Color.Gold, large: true);
            DebugLogger.LogInfo("RaccoonFlight", "P-meter flight started.");
            return true;
        }

        /// <summary>Sustains flight while jump is held. Call every frame when flying.</summary>
        public static void Sustain(Player player, bool jumpHeld, float dt)
        {
            if (_flightTimer <= 0f) return;
            if (!jumpHeld) { _flightTimer = 0f; return; }

            _flightTimer -= dt;
            // Override gravity while flying — maintain near-zero Y velocity.
            player.VelocityY = EasingFunctions.Lerp(player.VelocityY, -20f, 0.1f);
            if (_flightTimer <= 0f)
                DebugLogger.LogInfo("RaccoonFlight", "P-meter flight exhausted.");
        }

        /// <summary>Cancels flight immediately (landing, taking damage, etc.).</summary>
        public static void Cancel() => _flightTimer = 0f;
    }

    // ── Idea 6: Bounce block ──────────────────────────────────────────────────
    /// <summary>
    /// A static bounce block that launches the player upward when stomped.
    /// Team 7 (Gameplay Programmer) — Idea 6.
    /// </summary>
    public sealed class BounceBlock
    {
        public Rectangle Rect;
        private float    _squishTimer;
        private const    float BounceForce   = -650f;
        private const    float SquishDuration = 0.2f;

        /// <summary>Checks if the player landed on this block and applies the bounce.</summary>
        public bool TryBounce(Player player)
        {
            var pr = new Rectangle((int)player.X, (int)player.Y, player.Width, player.Height);
            if (!pr.IntersectsWith(Rect)) return false;
            if (player.VelocityY < 0) return false;

            // Player is falling onto the block — launch upward.
            player.Y         = Rect.Top - player.Height;
            player.VelocityY = BounceForce;
            player.IsGrounded = false;
            _squishTimer = SquishDuration;
            Engine.Game.Instance?.FloatingText?.Spawn(
                "BOING!", Rect.X + Rect.Width / 2, Rect.Y - 20, Color.Yellow);
            return true;
        }

        /// <summary>Tick animation.</summary>
        public void Tick(float dt) { if (_squishTimer > 0f) _squishTimer -= dt; }

        /// <summary>Draws the bounce block with squish animation.</summary>
        public void Draw(Graphics g)
        {
            float squish = _squishTimer > 0f
                ? 1f + 0.3f * EasingFunctions.EaseOutElastic(_squishTimer / 0.2f)
                : 1f;
            int w = (int)(Rect.Width  * squish);
            int h = (int)(Rect.Height / squish);
            int x = Rect.X - (w - Rect.Width) / 2;
            int y = Rect.Y + (Rect.Height - h);

            using (var br = new LinearGradientBrush(
                new Rectangle(x, y, w, h),
                Color.FromArgb(255, 180, 0), Color.FromArgb(200, 100, 0), 90f))
                g.FillRectangle(br, x, y, w, h);
            using (var pen = new Pen(Color.Black, 2))
                g.DrawRectangle(pen, x, y, w, h);
            using (var f = new Font("Courier New", 8, FontStyle.Bold))
                g.DrawString("↑", f, Brushes.White, x + w / 2 - 5, y + h / 2 - 7);
        }
    }

    // ── Idea 8: Spin-jump ─────────────────────────────────────────────────────
    /// <summary>
    /// A spin-jump that reduces gravity during descent, giving extended airtime.
    /// Team 7 (Gameplay Programmer) — Idea 8.
    /// </summary>
    public static class SpinJump
    {
        private static bool  _isSpinning;
        private static float _spinTimer;
        private const  float SpinDuration = 0.6f;
        private const  float GravityScale = 0.4f;

        /// <summary>Initiates a spin jump from the ground.</summary>
        public static bool TrySpin(Player player)
        {
            if (!player.IsGrounded) return false;
            _isSpinning = true;
            _spinTimer  = SpinDuration;
            player.VelocityY  = player.JumpForce * 0.9f;
            player.IsGrounded = false;
            return true;
        }

        /// <summary>Applies reduced gravity while spinning. Call every frame.</summary>
        public static void Update(Player player, float dt)
        {
            if (!_isSpinning) return;
            _spinTimer -= dt;
            if (_spinTimer <= 0f) { _isSpinning = false; return; }

            // Scale gravity down during the spin.
            if (!player.IsGrounded && player.VelocityY > 0)
                player.VelocityY += Character.Gravity * GravityScale * dt - Character.Gravity * dt;
        }

        /// <summary>True while a spin jump is in progress.</summary>
        public static bool IsSpinning => _isSpinning;
    }

    // ── Idea 9: Stomp-chain score popup ──────────────────────────────────────
    /// <summary>
    /// Generates escalating score text for stomp chains.
    /// Team 7 (Gameplay Programmer) — Idea 9.
    /// </summary>
    public static class StompChainScore
    {
        // SMB3-style score progression per stomp in a chain.
        private static readonly int[] _scoreTable = { 100, 200, 400, 800, 1000, 2000, 5000, 8000 };

        /// <summary>
        /// Records a stomp and awards the chain-progressive score.
        /// Call when the player successfully stomps an enemy.
        /// Team 7 (Gameplay Programmer) — Idea 9.
        /// </summary>
        public static void RecordStomp(Player player, float popX, float popY)
        {
            player.StompChain++;
            int idx   = Math.Min(player.StompChain - 1, _scoreTable.Length - 1);
            int score = _scoreTable[idx];
            // Route through ScoreManager — the canonical score tracker.
            // Team 7 (Gameplay Programmer) — Idea 9: stomp-chain scoring.
            ScoreManager.Add(score);

            string text = player.StompChain >= _scoreTable.Length ? "PERFECT!" : $"+{score}";
            Engine.Game.Instance?.FloatingText?.Spawn(text, (int)popX, (int)popY - 10,
                Color.Gold, large: player.StompChain >= 5);

            DebugLogger.LogInfo("StompChain", $"Chain {player.StompChain}: +{score} pts");

            if (player.StompChain >= 5)
                AchievementSystem.Grant("ach_combo_5");
            if (player.StompChain >= 10)
                AchievementSystem.Grant("ach_combo_10");
        }
    }

    // ── Idea 10: Conveyor belt force ──────────────────────────────────────────
    /// <summary>
    /// Applies a lateral push force to the player when standing on a conveyor belt.
    /// Team 7 (Gameplay Programmer) — Idea 10.
    /// </summary>
    public static class ConveyorBelt
    {
        /// <summary>
        /// Applies the conveyor force to the player if they are grounded and overlapping
        /// the belt rect.
        /// <paramref name="direction"/> +1 = right, -1 = left.
        /// </summary>
        public static void Apply(Player player, Rectangle beltRect, int direction, float speed, float dt)
        {
            if (!player.IsGrounded) return;
            var pr = new Rectangle((int)player.X, (int)player.Y + player.Height - 2, player.Width, 4);
            if (!pr.IntersectsWith(beltRect)) return;
            player.VelocityX += direction * speed * dt * 60f * 0.1f;
        }
    }
}
