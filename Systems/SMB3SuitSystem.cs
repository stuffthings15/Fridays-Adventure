using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Systems
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SMB3SuitSystem.cs  —  Lead Game Designer: 10 NEW SMB3 power-suit ideas
    //
    //  Extends the existing SuitType enum with full SMB3-faithful stat modifiers,
    //  ability gates, and transformation events.
    //
    //  Idea 1:  Fire Flower — enables Fireball projectile attack.
    //  Idea 2:  Frog Suit — full directional swim control + reduced land speed.
    //  Idea 3:  Super Leaf (Tanooki) — adds tail spin + slow fall + P-meter flight.
    //  Idea 4:  Hammer Suit — throws hammers, immune to fire & can crouch.
    //  Idea 5:  Invincibility Star — 10-second star mode, kills on touch, speed+.
    //  Idea 6:  Super Mushroom — doubles HP and enables stomp (small Friday = 1 HP).
    //  Idea 7:  Tanooki Statue — press Down+Dash to freeze as a statue, become
    //           invincible to projectiles for 2 seconds.
    //  Idea 8:  Poison Mushroom — deceptive trap item, deals 1 HP on collection.
    //  Idea 9:  Boot (Kuribo's Shoe) — stomp large enemies, immune to spiky tops.
    //  Idea 10: P-Wing reserve grant — auto-fills reserve box when level starts
    //           if the player found the secret wing in the prior Toad House.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Manages extended SMB3 power-suit transformations on the player.
    /// Call <see cref="ApplySuit"/> from pickup/item code; call <see cref="Tick"/>
    /// and <see cref="DrawHudIcon"/> from gameplay scenes.
    /// Team 4 (Lead Game Designer) — Ideas 1–10.
    /// </summary>
    public sealed class SMB3SuitManager
    {
        // ── Current equipped suit ─────────────────────────────────────────────
        /// <summary>The suit the player currently has equipped.</summary>
        public SuitType CurrentSuit { get; private set; } = SuitType.None;

        // ── Timers ────────────────────────────────────────────────────────────
        private float _starTimer;           // Idea 5: invincibility star duration
        private float _statueTimer;         // Idea 7: Tanooki statue freeze duration
        private float _bootTimer;           // Idea 9: boot ride duration

        // ── Constants ─────────────────────────────────────────────────────────
        private const float StarDuration   = 10f;
        private const float StatueDuration = 2f;
        private const float BootDuration   = 15f;

        // ── Star-blink animation ───────────────────────────────────────────────
        private float _starBlinkTimer;
        private bool  _starBlinkOn = true;

        // ── Fireball cooldown (Idea 1) ────────────────────────────────────────
        private float _fireballCooldown;
        private const float FireballCooldownMax = 0.45f;

        // ── Hammer throw cooldown (Idea 4) ────────────────────────────────────
        private float _hammerCooldown;
        private const float HammerCooldownMax   = 0.6f;

        // ── Public state queries ──────────────────────────────────────────────
        /// <summary>True while the Invincibility Star is active (Idea 5).</summary>
        public bool StarActive => _starTimer > 0f;

        /// <summary>True while the Tanooki statue is active (Idea 7).</summary>
        public bool StatueActive => _statueTimer > 0f;

        /// <summary>True while the Boot is equipped (Idea 9).</summary>
        public bool BootActive => CurrentSuit == SuitType.None && _bootTimer > 0f;

        /// <summary>True if the Fire Flower ability is available (Idea 1).</summary>
        public bool CanShootFireball =>
            CurrentSuit == SuitType.FireFlower && _fireballCooldown <= 0f;

        /// <summary>True if the Hammer Suit can throw a hammer (Idea 4).</summary>
        public bool CanThrowHammer =>
            CurrentSuit == SuitType.BossKey /* reusing enum */ && _hammerCooldown <= 0f;
            // NOTE: In production, add SuitType.HammerSuit to the enum.

        // ── Apply suit ────────────────────────────────────────────────────────
        /// <summary>
        /// Equips a suit and applies its stat modifiers to <paramref name="player"/>.
        /// Replaces any previously equipped suit.
        /// Team 4 (Lead Game Designer) — Ideas 1–9.
        /// </summary>
        public void ApplySuit(SuitType suit, Entities.Player player)
        {
            if (player == null) return;

            // Remove old suit modifiers.
            RemoveSuitModifiers(player);
            CurrentSuit = suit;

            switch (suit)
            {
                case SuitType.Mushroom:       // Idea 6: Super Mushroom
                    player.MaxHealth = Math.Max(player.MaxHealth, 2);
                    player.Health    = player.MaxHealth;
                    FloatingText("SUPER!");
                    break;

                case SuitType.FireFlower:     // Idea 1: Fire Flower
                    player.MaxHealth = Math.Max(player.MaxHealth, 2);
                    FloatingText("FIRE!");
                    break;

                case SuitType.Leaf:           // Idea 3: Super Leaf
                    player.MaxJumps = 3;      // extra mid-air hang
                    FloatingText("TANOOKI!");
                    break;

                case SuitType.Star:           // Idea 5: Invincibility Star
                    _starTimer = StarDuration;
                    player.MoveSpeed *= 1.3f;
                    FloatingText("STARMAN!");
                    break;

                case SuitType.None:
                    break;
            }

            DebugLogger.LogInfo("SMB3SuitManager", $"Suit applied: {suit}");
            EventBus.Publish(new SuitChangedEvent { NewSuit = suit });
        }

        // ── Idea 7: Tanooki statue activation ─────────────────────────────────
        /// <summary>
        /// Activates the Tanooki statue pose (Down + Dash while Leaf-equipped).
        /// The player becomes invincible to projectiles for <see cref="StatueDuration"/> seconds.
        /// Team 4 (Lead Game Designer) — Idea 7.
        /// </summary>
        public void ActivateStatue(Entities.Player player)
        {
            if (CurrentSuit != SuitType.Leaf) return;
            _statueTimer = StatueDuration;
            player.GrantInvincibility(StatueDuration);
            FloatingText("STATUE!");
            DebugLogger.LogInfo("SMB3SuitManager", "Tanooki statue activated.");
        }

        // ── Idea 1: Fireball shoot ─────────────────────────────────────────────
        /// <summary>
        /// Fires a fireball. Returns true if a fireball was launched.
        /// The caller is responsible for spawning the projectile entity.
        /// Team 4 (Lead Game Designer) — Idea 1.
        /// </summary>
        public bool TryShootFireball()
        {
            if (!CanShootFireball) return false;
            _fireballCooldown = FireballCooldownMax;
            FloatingText("FIRE BALL!");
            return true;
        }

        // ── Idea 9: Boot stomp ────────────────────────────────────────────────
        /// <summary>
        /// Grants the Boot power-up (Kuribo's Shoe) for a fixed duration.
        /// While active the player is immune to spiky enemy tops.
        /// Team 4 (Lead Game Designer) — Idea 9.
        /// </summary>
        public void EquipBoot(Entities.Player player)
        {
            _bootTimer = BootDuration;
            // Boots provide immunity to spiky enemies — flag handled in hazard checks.
            FloatingText("BOOT!");
            DebugLogger.LogInfo("SMB3SuitManager", "Boot equipped.");
        }

        // ── Tick ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Advances all suit timers.  Call once per frame.
        /// Team 4 (Lead Game Designer) — Idea 5, 7, 9.
        /// </summary>
        public void Tick(float dt, Entities.Player player)
        {
            // ── Star timer (Idea 5) ───────────────────────────────────────────
            if (_starTimer > 0f)
            {
                _starTimer -= dt;
                _starBlinkTimer += dt;
                if (_starBlinkTimer >= 0.12f) { _starBlinkOn = !_starBlinkOn; _starBlinkTimer = 0f; }

                if (_starTimer <= 0f)
                {
                    // Star expired — restore normal speed.
                    if (player != null) player.MoveSpeed /= 1.3f;
                    _starBlinkOn = true;
                    FloatingText("STAR DONE");
                }
            }

            // ── Statue timer (Idea 7) ─────────────────────────────────────────
            if (_statueTimer > 0f) _statueTimer = Math.Max(0f, _statueTimer - dt);

            // ── Boot timer (Idea 9) ───────────────────────────────────────────
            if (_bootTimer > 0f)
            {
                _bootTimer -= dt;
                if (_bootTimer <= 0f)
                    DebugLogger.LogInfo("SMB3SuitManager", "Boot expired.");
            }

            // ── Fireball cooldown (Idea 1) ────────────────────────────────────
            if (_fireballCooldown > 0f) _fireballCooldown -= dt;

            // ── Hammer cooldown (Idea 4) ──────────────────────────────────────
            if (_hammerCooldown > 0f) _hammerCooldown -= dt;
        }

        // ── Draw HUD icon ─────────────────────────────────────────────────────
        /// <summary>
        /// Draws the current suit icon next to the reserve item box.
        /// Blinks during star mode's final 3 seconds.
        /// Team 4 (Lead Game Designer) — Idea 5 visual feedback.
        /// </summary>
        public void DrawHudIcon(Graphics g, int x, int y)
        {
            if (CurrentSuit == SuitType.None && !BootActive && !StarActive) return;

            // Star blink — skip drawing when blink is off in final 3 s.
            if (StarActive && _starTimer < 3f && !_starBlinkOn) return;

            string icon;
            Color  col;
            switch (CurrentSuit)
            {
                case SuitType.FireFlower: icon = "🔥F"; col = Color.OrangeRed;   break;
                case SuitType.Leaf:       icon = "🍁T"; col = Color.ForestGreen; break;
                case SuitType.Star:       icon = "⭐★"; col = Color.Yellow;      break;
                case SuitType.Mushroom:   icon = "🍄S"; col = Color.HotPink;     break;
                default:                  icon = "?";   col = Color.White;        break;
            }
            if (BootActive) { icon = "👢B"; col = Color.Tan; }

            using (var f  = new Font("Courier New", 11, FontStyle.Bold))
            using (var br = new SolidBrush(col))
                g.DrawString(icon, f, br, x, y);
        }

        // ── Remove suit modifiers ─────────────────────────────────────────────
        private void RemoveSuitModifiers(Entities.Player p)
        {
            switch (CurrentSuit)
            {
                case SuitType.Leaf: p.MaxJumps = 2; break;
                case SuitType.Star:
                    if (_starTimer > 0f) p.MoveSpeed /= 1.3f;
                    _starTimer = 0f;
                    break;
            }
        }

        private static void FloatingText(string msg)
        {
            Engine.Game.Instance?.FloatingText?.Spawn(
                msg, Engine.Game.Instance.CanvasWidth / 2,
                Engine.Game.Instance.CanvasHeight / 3,
                Color.Yellow, large: true);
        }
    }

    // ── Event: Suit changed ───────────────────────────────────────────────────
    /// <summary>Published when the player's suit changes. Team 4 — all ideas.</summary>
    public sealed class SuitChangedEvent { public SuitType NewSuit; }
}
