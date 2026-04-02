using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Entities
{
    /// <summary>
    /// SMB3-style checkpoint flag — saves the player's position mid-level.
    ///
    /// Team 5  (Level Designer)      — place at ~1/2 and ~3/4 of level length.
    /// Team 7  (Gameplay Programmer) — TryCollect() activates the checkpoint.
    /// Team 2  (Producer)            — feeds CheckpointReached counter to SessionStats.
    /// Team 19 (QA Tester)           — checkpoint state verified in test runs.
    ///
    /// ── Scene integration ────────────────────────────────────────────────────
    /// 1. List&lt;Checkpoint&gt; _checkpoints initialized in OnEnter.
    /// 2. In Update: foreach(var cp in _checkpoints) cp.TryCollect(_player);
    /// 3. In Draw:   foreach(var cp in _checkpoints) cp.Draw(g, cameraY);
    /// 4. On respawn: restore player to LastCheckpointX / LastCheckpointY.
    /// </summary>
    public sealed class Checkpoint
    {
        // ── Dimensions ────────────────────────────────────────────────────────
        private const int PoleW   = 4;
        private const int PoleH   = 48;
        private const int FlagW   = 22;
        private const int FlagH   = 14;

        // ── State ─────────────────────────────────────────────────────────────
        /// <summary>World X centre of the pole base.</summary>
        public float X { get; }
        /// <summary>World Y of the pole base (ground level).</summary>
        public float Y { get; }
        /// <summary>Unique index within the level (0-based).</summary>
        public int   Index { get; }
        /// <summary>True once the player has activated this checkpoint.</summary>
        public bool  IsActivated { get; private set; }

        // ── Respawn position stored for the scene ─────────────────────────────
        /// <summary>X coordinate where the player will respawn.</summary>
        public float RespawnX => X - 20;
        /// <summary>Y coordinate where the player will respawn (above ground).</summary>
        public float RespawnY => Y - 60;

        // ── Animation ────────────────────────────────────────────────────────
        private float _waveTimer;   // drives flag wave animation
        private float _flashTimer;  // brief flash on activation

        // ── Constructor ───────────────────────────────────────────────────────
        /// <param name="x">World X of the pole base centre.</param>
        /// <param name="y">World Y of the pole base (stands on ground).</param>
        /// <param name="index">Sequential index in the level.</param>
        public Checkpoint(float x, float y, int index = 0)
        {
            X     = x;
            Y     = y;
            Index = index;
        }

        // ── Update ────────────────────────────────────────────────────────────
        /// <summary>
        /// Advances animation timers and checks whether the player collects this checkpoint.
        /// </summary>
        public void TryCollect(Player player, float dt)
        {
            _waveTimer += dt;
            if (_flashTimer > 0f) _flashTimer -= dt;

            if (IsActivated) return;

            // Generous trigger radius: 40px from pole centre.
            float dx = player.CenterX - X;
            float dy = player.CenterY - (Y - PoleH / 2f);
            if (Math.Sqrt(dx * dx + dy * dy) < 40f)
            {
                Activate();
                player.Health = Math.Min(player.MaxHealth, player.Health + player.MaxHealth / 4);
            }
        }

        private void Activate()
        {
            IsActivated = true;
            _flashTimer = 0.6f;
            SessionStats.Instance.RecordCheckpoint();
            AchievementSystem.Grant("ach_checkpoint");
            EventBus.Publish(new CheckpointEvent { CheckpointIndex = Index });
            DebugLogger.LogInfo("Checkpoint", $"Reached checkpoint #{Index} at ({X:F0},{Y:F0})");
        }

        // ── Draw ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Draws the checkpoint flag pole and waving flag.
        /// cameraY is subtracted from world Y for vertical-scrolling scenes.
        /// </summary>
        public void Draw(Graphics g, float cameraY = 0f)
        {
            int sx = (int)X - PoleW / 2;
            int sy = (int)(Y - cameraY) - PoleH;

            // ── Activation flash ──────────────────────────────────────────────
            if (_flashTimer > 0f)
            {
                float alpha = (_flashTimer / 0.6f) * 160;
                using (var br = new SolidBrush(Color.FromArgb((int)alpha, Color.Gold)))
                    g.FillEllipse(br, sx - 20, sy - 20, 48, 80);
            }

            // ── Pole ──────────────────────────────────────────────────────────
            Color poleColor = IsActivated ? Color.FromArgb(220, 180, 40) : Color.FromArgb(160, 160, 160);
            using (var br = new SolidBrush(poleColor))
                g.FillRectangle(br, sx, sy, PoleW, PoleH);

            // Pole shine
            using (var br = new SolidBrush(Color.FromArgb(80, Color.White)))
                g.FillRectangle(br, sx, sy, 2, PoleH);

            // ── Flag ──────────────────────────────────────────────────────────
            // Flag waves using a sine-based horizontal offset on the tip.
            float wave = IsActivated
                ? (float)Math.Sin(_waveTimer * 5f) * 4f
                : 0f;

            Color flagColor = IsActivated ? Color.LimeGreen : Color.FromArgb(180, 180, 180);

            // Flag body — simple quad approximating a waving flag.
            var pts = new PointF[]
            {
                new PointF(sx + PoleW,       sy),
                new PointF(sx + PoleW + FlagW + wave, sy + FlagH / 2f),
                new PointF(sx + PoleW,       sy + FlagH)
            };
            using (var br = new SolidBrush(flagColor))
                g.FillPolygon(br, pts);

            // ── Base sphere ───────────────────────────────────────────────────
            using (var br = new SolidBrush(IsActivated ? Color.Gold : Color.Silver))
                g.FillEllipse(br, sx - 3, (int)(Y - cameraY) - 4, PoleW + 6, PoleW + 6);

            // ── "CP" label above if not yet activated ─────────────────────────
            if (!IsActivated)
            {
                using (var f = new Font("Courier New", 7, FontStyle.Bold))
                    g.DrawString("CP", f, Brushes.Silver, sx - 3, sy - 12);
            }
        }
    }
}
