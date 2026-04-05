using System.Windows.Forms;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Tests
{
    // ────────────────────────────────────────────
    // PHASE 2 - Team 10: Engine Programmer
    // Feature: Bot Player Controller
    // Purpose: Injects synthetic key presses into InputManager so the actual
    //          game scenes run exactly as if a human were playing.
    // ────────────────────────────────────────────

    /// <summary>
    /// Frame-by-frame AI that injects real keystrokes into <see cref="InputManager"/>
    /// so the actual game engine drives the player through each level.
    ///
    /// Design:
    ///   • Always hold Right + Shift (sprint forward)
    ///   • Jump every 0.55 s, hold Space for 0.38 s (full-height jump; double-jump fires mid-air)
    ///   • Attack (Z) injected EVERY frame — TryAttack() has its own cooldown so it fires
    ///     at the player's max attack rate; also keeps AnyMash = true so the bot
    ///     escapes water/SeaStone sinking within one frame
    ///   • Frost-ball (B) every 4 s for ranged coverage
    ///
    /// GodMode must be enabled by the caller (BotPlayLevelScene) so the player
    /// never dies — the bot relies on being unkillable rather than on dodging.
    /// </summary>
    public sealed class BotPlayerController
    {
        // ── State ─────────────────────────────────────────────────────────
        private float _time         = 0f;   // total elapsed time this level
        private float _jumpInterval = 0f;   // accumulates toward next jump
        private float _jumpHoldTimer = 0f;  // > 0 while Space should stay held
        private float _frostTimer   = 0f;   // accumulates toward next frost ball

        // ── Tuning ────────────────────────────────────────────────────────
        /// <summary>Seconds between jump triggers.</summary>
        private const float JumpInterval = 0.55f;

        /// <summary>
        /// How long to hold Space after each press.
        /// 0.38 s gives a near-maximum jump arc; anything shorter becomes a short-hop.
        /// </summary>
        private const float JumpHoldTime = 0.38f;

        /// <summary>Seconds between Frost Ball shots.</summary>
        private const float FrostInterval = 4.0f;

        /// <summary>
        /// Resets all timers for a new level run.
        /// </summary>
        public void Reset()
        {
            _time         = 0f;
            _jumpInterval = 0f;
            _jumpHoldTimer = 0f;
            _frostTimer   = 0f;
        }

        /// <summary>
        /// Call once per game frame BEFORE the inner scene's Update().
        /// Injects the appropriate keys so the real player entity behaves like
        /// a live human player.
        /// Call <see cref="InputManager.ClearInjected"/> AFTER the scene Update().
        /// </summary>
        /// <param name="input">The game's InputManager instance.</param>
        /// <param name="dt">Frame delta time in seconds.</param>
        public void InjectInput(InputManager input, float dt)
        {
            _time         += dt;
            _jumpInterval += dt;
            _frostTimer   += dt;
            if (_jumpHoldTimer > 0f)
                _jumpHoldTimer -= dt;

            // ── Always run right at sprint speed ──────────────────────────
            input.InjectHeld(Keys.Right);
            input.InjectHeld(Keys.ShiftKey);   // sprint — clears wide gaps faster

            // ── Attack pressed every frame ────────────────────────────────
            // Player.TryAttack() checks its own cooldown (~0.5 s), so this fires
            // at the max allowed rate, not every frame in terms of actual hits.
            // Injecting every frame also keeps InputManager.AnyMash = true, which
            // causes the sinking/SeaStone escape timer to drain by 0.4 per frame
            // — the bot escapes water in < 1 frame at 60 fps.
            input.InjectPressed(Keys.Z);

            // ── Periodic full-height jump ─────────────────────────────────
            // Fire a new jump every JumpInterval seconds.
            if (_jumpInterval >= JumpInterval)
            {
                input.InjectPressed(Keys.Space);   // begin jump
                _jumpHoldTimer = JumpHoldTime;     // schedule hold window
                _jumpInterval  = 0f;
            }

            // While inside the hold window, keep Space held so the engine
            // doesn't cut velocity to the short-hop cap (-120 px/s).
            if (_jumpHoldTimer > 0f)
                input.InjectHeld(Keys.Space);

            // ── Periodic frost ball (ranged coverage) ─────────────────────
            if (_frostTimer >= FrostInterval)
            {
                input.InjectPressed(Keys.B);
                _frostTimer = 0f;
            }
        }

        /// <summary>Elapsed seconds since last Reset.</summary>
        public float ElapsedTime => _time;
    }
}
