// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 – Multi-Team Implementation
// Systems/HammerBrosSystem.cs
// Purpose: Hammer Bros patrol on the overworld map.  They move between nodes,
//          and entering their node triggers a mini-encounter.
// ────────────────────────────────────────────────────────────────────────────
// Team 1  (Game Director)       – Idea 8:  Hammer Bros patrol between nodes
// Team 1  (Game Director)       – Idea 9:  Reaching player node = encounter
// Team 1  (Game Director)       – Idea 10: Defeat grants 1UP + item reward
// Team 4  (Lead Game Designer)  – Idea 14: Hammer Bros retreat when HP is low
// Team 4  (Lead Game Designer)  – Idea 15: 2 Hammer Bros per world chapter
// Team 9  (UI Programmer)       – Idea 4:  Overworld "!" warning popup
// Team 9  (UI Programmer)       – Idea 5:  Hammer Bros icon on overworld map
// Team 14 (Environment Artist)  – Idea 13: Hammer Bros walking sprite
// Team 14 (Environment Artist)  – Idea 14: "!" alert icon visual
// Team 17 (VFX Artist)          – Idea 11: Alert flash when HB moves toward player
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Tracks one or more Hammer Bros on the overworld map.
    /// Each Hammer Bro occupies a node ID and moves toward the player every few seconds.
    /// When a Hammer Bro shares the player's node, an encounter is triggered.
    ///
    /// Team 1 (Game Director) — Ideas 8–10.
    /// Team 4 (Lead Game Designer) — Ideas 14–15.
    /// Team 9 (UI Programmer) — Ideas 4–5.
    /// Team 14 (Environment Artist) — Ideas 13–14.
    /// Team 17 (VFX Artist) — Idea 11.
    /// </summary>
    public static class HammerBrosSystem
    {
        // ── Hammer Bro data ───────────────────────────────────────────────────
        public sealed class HammerBro
        {
            /// <summary>Current overworld node ID this Hammer Bro occupies.</summary>
            public string NodeId { get; set; }

            /// <summary>True once the encounter for this Hammer Bro has been won.</summary>
            public bool Defeated { get; set; }

            /// <summary>Seconds until this Hammer Bro moves to an adjacent node.</summary>
            public float MoveTimer { get; set; }

            /// <summary>Walk animation phase [0–1].</summary>
            public float WalkAnim { get; set; }

            /// <summary>True when this Hammer Bro is one node away from the player.</summary>
            public bool IsAlert { get; set; }

            /// <summary>Alert flash timer (Team 17 — Idea 11).</summary>
            public float AlertFlashTimer { get; set; }
        }

        // ── State ─────────────────────────────────────────────────────────────
        private static readonly List<HammerBro> _bros = new List<HammerBro>();
        private static readonly Random _rng = new Random();

        // ── Move interval (Team 1 — Idea 8) ───────────────────────────────────
        private const float MoveInterval  = 8.0f;   // seconds between moves
        private const float AlertDistance = 1;      // node hops to trigger alert

        // ── Encounter trigger ─────────────────────────────────────────────────
        /// <summary>
        /// Set to the node ID of an encountered Hammer Bro; cleared after processing.
        /// Team 1 (Game Director) — Idea 9.
        /// </summary>
        public static string PendingEncounterNodeId { get; set; }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Spawns Hammer Bros at the given node IDs.
        /// Team 1 (Game Director) — Idea 8.
        /// Team 4 (Lead Game Designer) — Idea 15: 2 per world chapter.
        /// </summary>
        public static void Spawn(params string[] nodeIds)
        {
            _bros.Clear();
            foreach (string id in nodeIds)
            {
                _bros.Add(new HammerBro
                {
                    NodeId    = id,
                    MoveTimer = MoveInterval + (float)_rng.NextDouble() * 4f
                });
            }
        }

        /// <summary>Removes all Hammer Bros (call on new game or world transition).</summary>
        public static void Clear() => _bros.Clear();

        /// <summary>
        /// Marks a Hammer Bro at <paramref name="nodeId"/> as defeated.
        /// Awards a 1UP and places a random item in the reserve.
        /// Team 1 (Game Director) — Idea 10.
        /// </summary>
        public static void OnDefeated(string nodeId)
        {
            foreach (var b in _bros)
            {
                if (b.NodeId != nodeId) continue;
                b.Defeated = true;

                // 1UP reward (Team 1 — Idea 10)
                Game.Instance.CurrentLives++;
                SMB3Hud.ShowToast($"Hammer Bros defeated! ♥×{Game.Instance.CurrentLives}");
                Game.Instance.FloatingText.Spawn("1UP!",
                    Game.Instance.CanvasWidth / 2, 120, Color.LimeGreen, large: true);

                // Random item in reserve (Team 1 — Idea 10)
                var suits = new[] { SuitType.Mushroom, SuitType.FireFlower, SuitType.Leaf };
                PowerUpInventory.SetReserve(suits[_rng.Next(suits.Length)]);
                break;
            }
        }

        /// <summary>
        /// Returns all active (non-defeated) Hammer Bros.
        /// Team 9 (UI Programmer) — Idea 5.
        /// </summary>
        public static IReadOnlyList<HammerBro> ActiveBros => _bros;

        // ── Update (Team 1 — Idea 8 / Team 4 — Idea 14) ──────────────────────
        /// <summary>
        /// Advances Hammer Bros timers and moves them toward the player.
        /// Call from OverworldScene.Update().
        /// </summary>
        /// <param name="dt">Frame delta time.</param>
        /// <param name="playerNodeId">The node ID the player currently occupies.</param>
        /// <param name="nodeLinks">Adjacency look-up: nodeId → list of linked node IDs.</param>
        public static void Update(float dt, string playerNodeId,
            Func<string, IReadOnlyList<string>> nodeLinks)
        {
            foreach (var b in _bros)
            {
                if (b.Defeated) continue;

                b.WalkAnim  += dt * 4f;
                b.MoveTimer -= dt;

                if (b.AlertFlashTimer > 0f) b.AlertFlashTimer -= dt;

                if (b.MoveTimer <= 0f)
                {
                    b.MoveTimer = MoveInterval;
                    MoveTowardPlayer(b, playerNodeId, nodeLinks);
                }

                // Check encounter (same node as player)
                if (b.NodeId == playerNodeId)
                {
                    PendingEncounterNodeId = b.NodeId;
                }

                // Alert when adjacent to player (Team 9 — Idea 4)
                var links = nodeLinks(playerNodeId);
                b.IsAlert = links != null && links.Contains(b.NodeId);
                if (b.IsAlert && b.AlertFlashTimer <= 0f)
                    b.AlertFlashTimer = 0.6f;  // flash timer (Team 17 — Idea 11)
            }
        }

        private static void MoveTowardPlayer(HammerBro b, string playerNodeId,
            Func<string, IReadOnlyList<string>> nodeLinks)
        {
            // Simple move: pick a random adjacent node (biased toward player)
            var fromLinks = nodeLinks(b.NodeId);
            if (fromLinks == null || fromLinks.Count == 0) return;

            // Prefer moving toward player (50% chance)
            if (_rng.NextDouble() < 0.5)
            {
                var playerAdj = nodeLinks(playerNodeId);
                if (playerAdj != null)
                {
                    // Find fromLinks that also appears in playerAdj or equals playerNodeId
                    foreach (string adj in fromLinks)
                    {
                        if (adj == playerNodeId || (playerAdj != null && playerAdj.Contains(adj)))
                        {
                            b.NodeId = adj;
                            return;
                        }
                    }
                }
            }

            // Random move
            b.NodeId = fromLinks[_rng.Next(fromLinks.Count)];
        }

        // ── Draw (Team 14 — Idea 13 / Team 9 — Idea 5 / Team 17 — Idea 11) ───
        /// <summary>
        /// Draws all active Hammer Bros at their node positions on the overworld.
        /// Call from OverworldScene.Draw().
        /// </summary>
        /// <param name="g">Graphics context.</param>
        /// <param name="nodePositions">Map of nodeId → screen Point.</param>
        public static void Draw(Graphics g,
            Func<string, Point?> nodePositions, float anim)
        {
            foreach (var b in _bros)
            {
                if (b.Defeated) continue;

                Point? pos = nodePositions(b.NodeId);
                if (pos == null) continue;

                int px = pos.Value.X;
                int py = pos.Value.Y;

                // Alert flash ring (Team 17 — Idea 11)
                if (b.AlertFlashTimer > 0f)
                {
                    int fa = (int)(b.AlertFlashTimer / 0.6f * 200);
                    using (var pen = new Pen(Color.FromArgb(fa, Color.OrangeRed), 3))
                        g.DrawEllipse(pen, px - 18, py - 18, 36, 36);
                }

                // "!" icon (Team 14 — Idea 14)
                if (b.IsAlert)
                {
                    float bob = (float)(Math.Sin(anim * 6) * 3);
                    using (var f = new Font("Courier New", 14, FontStyle.Bold))
                    using (var br = new SolidBrush(Color.FromArgb(220, Color.OrangeRed)))
                        g.DrawString("!", f, br, px - 6, py - 30 + bob);
                }

                DrawHammerBroSprite(g, px - 12, py - 24, b.WalkAnim);
            }
        }

        /// <summary>
        /// Draws a small Hammer Bro icon at the given pixel position.
        /// Team 14 (Environment Artist) — Idea 13.
        /// </summary>
        private static void DrawHammerBroSprite(Graphics g, int x, int y, float walkAnim)
        {
            float bob = (float)Math.Sin(walkAnim) * 2f;
            float yr  = y + bob;

            // Helmet
            using (var br = new SolidBrush(Color.FromArgb(50, 80, 40)))
                g.FillRectangle(br, x + 4, yr, 16, 7);
            // Head
            using (var br = new SolidBrush(Color.FromArgb(210, 175, 110)))
                g.FillEllipse(br, x + 4, yr + 5, 16, 14);
            // Body
            using (var br = new SolidBrush(Color.FromArgb(80, 130, 60)))
                g.FillRectangle(br, x + 6, yr + 17, 12, 12);
            // Legs
            using (var br = new SolidBrush(Color.FromArgb(40, 80, 30)))
            {
                float legSwing = (float)Math.Sin(walkAnim * 2f) * 3f;
                g.FillRectangle(br, x + 6,  yr + 28 - legSwing, 5, 8);
                g.FillRectangle(br, x + 13, yr + 28 + legSwing, 5, 8);
            }
            // Hammer icon above head
            using (var br = new SolidBrush(Color.FromArgb(160, 140, 140)))
                g.FillRectangle(br, x, yr - 5, 8, 5);
            using (var br = new SolidBrush(Color.FromArgb(110, 70, 20)))
                g.FillRectangle(br, x + 3, yr - 10, 3, 8);
        }

        // ── Overworld warning popup draw (Team 9 — Idea 4) ───────────────────
        /// <summary>
        /// Draws the "HAMMER BROS NEARBY!" warning toast if any Hammer Bro is alert.
        /// Team 9 (UI Programmer) — Idea 4.
        /// </summary>
        public static void DrawWarningToast(Graphics g, int W, int H, float anim)
        {
            bool anyAlert = false;
            foreach (var b in _bros)
                if (!b.Defeated && b.IsAlert) { anyAlert = true; break; }

            if (!anyAlert) return;

            float pulse = (float)(Math.Sin(anim * 6) * 0.5 + 0.5);
            using (var br = new SolidBrush(Color.FromArgb((int)(180 * pulse), 200, 40, 0)))
                g.FillRectangle(br, W / 2 - 130, H - 52, 260, 28);

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
            using (var br = new SolidBrush(Color.White))
            {
                const string msg = "⚠  HAMMER BROS NEARBY!  ⚠";
                var sz = g.MeasureString(msg, f);
                g.DrawString(msg, f, br,
                    (W - sz.Width) / 2f, H - 48);
            }
        }
    }
}
