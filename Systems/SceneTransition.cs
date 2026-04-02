using System;
using System.Drawing;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// SMB3-style curtain wipe scene transition.
    ///
    /// Two black curtain panels (top + bottom) slide inward from the screen edges
    /// to meet at the centre (closing phase), then slide back out to reveal the
    /// new scene (opening phase).  Any scene can request a transition by calling
    /// <see cref="Begin"/> with an optional callback that fires mid-wipe.
    ///
    /// Team 1  (Game Director)  — Idea 5: curtain wipe between every scene change.
    /// Team 16 (2D Animator)    — Idea 6: curtain animation uses EaseInOutCubic.
    /// </summary>
    public static class SceneTransition
    {
        // ── State machine ──────────────────────────────────────────────────────
        private enum Phase { Idle, Closing, Holding, Opening }
        private static Phase _phase = Phase.Idle;

        // ── Timing ─────────────────────────────────────────────────────────────
        private const float CloseDuration = 0.30f;   // seconds for curtains to close
        private const float HoldDuration  = 0.10f;   // seconds held fully closed
        private const float OpenDuration  = 0.30f;   // seconds for curtains to open

        private static float _timer;
        private static Action _midCallback;  // fired once curtains are fully closed

        /// <summary>True while a transition is in progress (blocks input during wipe).</summary>
        public static bool IsActive => _phase != Phase.Idle;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Starts a curtain-wipe transition.
        /// <paramref name="midCallback"/> is called when the screen is fully black
        /// — use it to push or replace scenes so the swap is invisible to the player.
        /// </summary>
        public static void Begin(Action midCallback)
        {
            if (_phase != Phase.Idle) return;  // never interrupt an active transition
            _midCallback = midCallback;
            _timer       = 0f;
            _phase       = Phase.Closing;
        }

        /// <summary>
        /// Advances the transition timer.  Call once per game tick from Game.OnTick.
        /// </summary>
        public static void Update(float dt)
        {
            if (_phase == Phase.Idle) return;

            _timer += dt;
            switch (_phase)
            {
                case Phase.Closing:
                    if (_timer >= CloseDuration)
                    {
                        _timer = 0f;
                        _phase = Phase.Holding;
                        // Fire the scene-swap callback while the screen is black.
                        _midCallback?.Invoke();
                        _midCallback = null;
                    }
                    break;

                case Phase.Holding:
                    if (_timer >= HoldDuration)
                    { _timer = 0f; _phase = Phase.Opening; }
                    break;

                case Phase.Opening:
                    if (_timer >= OpenDuration)
                    { _timer = 0f; _phase = Phase.Idle; }
                    break;
            }
        }

        /// <summary>
        /// Draws the curtain panels over the already-drawn scene.
        /// Call from Game.OnRender AFTER the current scene draws.
        /// </summary>
        public static void Draw(Graphics g, int W, int H)
        {
            if (_phase == Phase.Idle) return;

            // Compute how far the curtains have extended from the edges.
            float progress;
            switch (_phase)
            {
                case Phase.Closing:
                    progress = EasingFunctions.EaseInOutCubic(_timer / CloseDuration);
                    break;
                case Phase.Holding:
                    progress = 1f;
                    break;
                default: // Opening
                    progress = 1f - EasingFunctions.EaseInOutCubic(_timer / OpenDuration);
                    break;
            }

            // Each panel covers half the screen height at full extent.
            int panelH = (int)(H / 2f * progress) + 2; // +2 to remove hairline gap

            using (var br = new SolidBrush(Color.Black))
            {
                // Top curtain slides down.
                g.FillRectangle(br, 0, 0, W, panelH);
                // Bottom curtain slides up.
                g.FillRectangle(br, 0, H - panelH, W, panelH);
            }
        }
    }
}
