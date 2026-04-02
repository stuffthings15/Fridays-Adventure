using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// SMB3-style power-up state machine for the player.
    ///
    /// Tracks the active power-up, handles upgrade/downgrade chains,
    /// and provides the reserve item slot (hold one extra item).
    ///
    /// Power-up hierarchy (SMB3-style):
    ///   None → Mushroom (Super) → FireFlower → Star (temporary override)
    ///
    /// Team 4  (Lead Game Designer) — Idea 1: Super Star invincibility.
    /// Team 4  (Lead Game Designer) — Idea 2: Coin counter bonus life tie-in.
    /// Team 4  (Lead Game Designer) — Idea 3: HP states (small / normal / powered).
    /// Team 4  (Lead Game Designer) — Idea 8: Power-up reserve item slot (SMB3).
    /// Team 7  (Gameplay Programmer) — Idea 3: power-up hit-state HP remapping.
    /// Team 9  (UI Programmer)       — Idea 2: reserve item box in HUD.
    /// Team 13 (Character Artist)    — Idea 3: invincibility star animation signal.
    /// Team 17 (VFX Artist)          — Idea 4: fire trail VFX flag.
    /// Team 18 (Sound Designer)      — Idea 1: power-up SFX hook.
    /// </summary>
    public sealed class PowerUpState
    {
        // ── Power-up type enum ─────────────────────────────────────────────────
        public enum PowerUp
        {
            None        = 0,  // base/small form
            Mushroom    = 1,  // super form — double HP
            FireFlower  = 2,  // fire form — can throw fireballs
            Star        = 3,  // temporary invincibility (timed)
        }

        // ── Active state ───────────────────────────────────────────────────────
        /// <summary>Current equipped power-up (persists until hit or used).</summary>
        public PowerUp Active { get; private set; } = PowerUp.None;

        /// <summary>Reserve/inventory slot — one extra item held (SMB3-style).</summary>
        public PowerUp Reserve { get; private set; } = PowerUp.None;

        // ── Star (invincibility) timer ────────────────────────────────────────
        private float  _starTimer;
        private const float StarDuration = 10f;  // seconds of invincibility

        /// <summary>True while the star invincibility is active.</summary>
        public bool IsInvincibleStar => _starTimer > 0f;

        /// <summary>Normalised star timer progress [0→1] for animation/VFX.</summary>
        public float StarProgress => _starTimer / StarDuration;

        // ── Fire cooldown ─────────────────────────────────────────────────────
        private float _fireCooldown;
        private const float FireCooldownDuration = 0.4f;

        /// <summary>True when a fireball can be thrown this frame.</summary>
        public bool CanFireball => Active == PowerUp.FireFlower && _fireCooldown <= 0f;

        // ── Berry-to-life counter ─────────────────────────────────────────────
        private int _berryBankCount;
        private const int BerriesPerLife = 100;

        /// <summary>
        /// Progress toward the next extra life via berry collection [0–99].
        /// Team 4 (Lead Game Designer) — Idea 2.
        /// </summary>
        public int BerryBank => _berryBankCount % BerriesPerLife;

        // ── Constructor ────────────────────────────────────────────────────────
        public PowerUpState() { }

        // ── Tick ──────────────────────────────────────────────────────────────

        /// <summary>Advances all timers by one game tick.</summary>
        public void Update(float dt)
        {
            if (_starTimer > 0f)
            {
                _starTimer -= dt;
                if (_starTimer <= 0f)
                {
                    _starTimer = 0f;
                    // Star expired: revert to whatever state was active before star.
                    // (We leave Active as-is — star is a temporary layer on top.)
                }
            }

            if (_fireCooldown > 0f)
                _fireCooldown = Math.Max(0f, _fireCooldown - dt);
        }

        // ── Collection ────────────────────────────────────────────────────────

        /// <summary>
        /// Collects a power-up item. Follows the SMB3 upgrade hierarchy:
        /// None→Mushroom→FireFlower. Stars are always immediately applied.
        /// If already fully powered, new item goes to the reserve slot.
        /// </summary>
        public void Collect(PowerUp item, Entities.Player player)
        {
            if (item == PowerUp.Star)
            {
                // Star is always applied immediately regardless of current state.
                _starTimer = StarDuration;
                SessionStats.Instance.RecordPowerUp();
                DebugLogger.LogInfo("PowerUpState", "Star collected — invincibility active.");
                return;
            }

            if (Active == PowerUp.None)
            {
                Apply(item, player);
            }
            else if ((int)item > (int)Active)
            {
                // Upgrade.
                Apply(item, player);
            }
            else
            {
                // Can't upgrade further — store in reserve if empty.
                if (Reserve == PowerUp.None)
                {
                    Reserve = item;
                    DebugLogger.LogInfo("PowerUpState", $"Reserve item set: {item}");
                }
            }
            SessionStats.Instance.RecordPowerUp();
        }

        /// <summary>
        /// Uses the reserve item (player presses the item button).
        /// Swaps reserve into active if applicable.
        /// </summary>
        public void UseReserve(Entities.Player player)
        {
            if (Reserve == PowerUp.None) return;
            PowerUp held = Reserve;
            Reserve = PowerUp.None;
            Collect(held, player);
        }

        /// <summary>
        /// Called when the player takes a hit.  Downgrades the power-up state.
        /// Returns false if the player should die (no power-up to absorb the hit).
        /// Team 4 (Lead Game Designer) — Idea 3: hit-state HP remapping.
        /// </summary>
        public bool AbsorbHit(Entities.Player player)
        {
            if (IsInvincibleStar) return true;  // star absorbs all hits

            switch (Active)
            {
                case PowerUp.FireFlower:
                    Apply(PowerUp.Mushroom, player);   // fire → super
                    return true;
                case PowerUp.Mushroom:
                    Apply(PowerUp.None, player);       // super → small
                    return true;
                default:
                    return false;  // small form: player dies
            }
        }

        // ── Berry bank ────────────────────────────────────────────────────────

        /// <summary>
        /// Records a berry collection.  Every 100 berries grants an extra life.
        /// Team 4 (Lead Game Designer) — Idea 2.
        /// </summary>
        public void RecordBerry(int count = 1)
        {
            _berryBankCount += count;
            while (_berryBankCount >= BerriesPerLife)
            {
                _berryBankCount -= BerriesPerLife;
                Game.Instance.CurrentLives++;
                Game.Instance.FloatingText.Spawn("1-UP!", 450, 260, Color.Gold);
                AchievementSystem.Grant("ach_berry_life");
                DebugLogger.LogInfo("PowerUpState", "100 berries → extra life awarded.");
            }
        }

        // ── Fireball ──────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to spawn a fireball projectile.
        /// Returns a new <see cref="FireballProjectile"/> or null if on cooldown.
        /// </summary>
        public FireballProjectile TryFireball(Entities.Player player)
        {
            if (!CanFireball) return null;
            _fireCooldown = FireCooldownDuration;
            float bx = player.FacingRight ? player.X + player.Width + 4 : player.X - 16;
            float by = player.Y + player.Height * 0.5f;
            float vx = player.FacingRight ? 420f : -420f;
            return new FireballProjectile(bx, by, vx);
        }

        // ── Internal apply ─────────────────────────────────────────────────────

        /// <summary>Applies a power-up state, adjusting player HP to match.</summary>
        private void Apply(PowerUp p, Entities.Player player)
        {
            Active = p;
            switch (p)
            {
                case PowerUp.None:
                    // Small form — minimal HP.
                    player.MaxHealth = 40;
                    if (player.Health > 40) player.Health = 40;
                    break;
                case PowerUp.Mushroom:
                    // Super form — standard HP.
                    player.MaxHealth = 100;
                    player.Health    = Math.Max(player.Health, 60);
                    break;
                case PowerUp.FireFlower:
                    // Fire form — same HP as mushroom + fire ability.
                    player.MaxHealth = 100;
                    player.Health    = Math.Max(player.Health, 80);
                    break;
            }
            DebugLogger.LogInfo("PowerUpState", $"Applied power-up: {p}");
        }
    }

    // ── Fireball Projectile ────────────────────────────────────────────────────

    /// <summary>
    /// A simple bouncing fireball projectile spawned by the Fire Flower power-up.
    ///
    /// Team 7 (Gameplay Programmer) — Idea 7: projectile system.
    /// Team 17 (VFX Artist)         — Idea 4: fire trail VFX on projectile.
    /// </summary>
    public sealed class FireballProjectile
    {
        public float X     { get; private set; }
        public float Y     { get; private set; }
        public float VX    { get; private set; }
        public float VY    { get; private set; }
        public bool  Alive { get; private set; } = true;

        // Gravity for the bouncing arc.
        private const float Gravity     = 600f;
        private const float BounceForce = -280f;
        private const int   MaxBounces  = 3;
        private int         _bounces;

        // Visual glow timer for VFX.
        public float GlowTimer { get; private set; } = 0.15f;

        public FireballProjectile(float x, float y, float vx)
        {
            X = x; Y = y; VX = vx; VY = -100f;
        }

        /// <summary>Updates the fireball physics and lifetime each tick.</summary>
        public void Update(float dt, int groundY)
        {
            if (!Alive) return;

            VY += Gravity * dt;
            X  += VX * dt;
            Y  += VY * dt;

            // Bounce off the ground.
            if (Y >= groundY)
            {
                Y  = groundY;
                VY = BounceForce;
                _bounces++;
                if (_bounces >= MaxBounces) Alive = false;
            }

            // Expire after travelling off-screen or too many bounces.
            if (Math.Abs(X) > 3000) Alive = false;

            // VFX glow decay.
            GlowTimer = Math.Max(0f, GlowTimer - dt);
        }

        /// <summary>Draws the fireball as a glowing orange-white dot.</summary>
        public void Draw(Graphics g, float cameraX)
        {
            if (!Alive) return;
            float sx = X - cameraX;
            float sy = Y;
            using (var br = new SolidBrush(Color.FromArgb(200, 255, 140, 0)))
                g.FillEllipse(br, sx - 7, sy - 7, 14, 14);
            using (var br = new SolidBrush(Color.FromArgb(240, 255, 255, 180)))
                g.FillEllipse(br, sx - 3, sy - 3, 6, 6);
        }

        /// <summary>
        /// Immediately deactivates this fireball (e.g. on enemy or wall contact).
        /// Called by <see cref="FireballSystem.CheckEnemyHit"/> when a hit is confirmed.
        /// Team 7 (Gameplay Programmer) — Idea 4: projectile system.
        /// </summary>
        public void Kill() { Alive = false; }
    }
}
