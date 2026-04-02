using System.Drawing;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Global render-mode flags toggled by F-keys during GodMode.
    ///
    /// Team 3 (Technical Lead)  — Idea 1: F9 collision box visualization.
    /// Team 3 (Technical Lead)  — Idea 2: F11 hitbox visualization.
    /// Team 3 (Technical Lead)  — Idea 5: input state inspector draw.
    /// Team 3 (Technical Lead)  — Idea 6: entity count monitor.
    ///
    /// ── Wiring ───────────────────────────────────────────────────────────────
    ///   In Form1.KeyDown:
    ///     if (e.KeyCode == Keys.F9)  RenderDebugModes.ToggleCollision();
    ///     if (e.KeyCode == Keys.F10) VisualDebugger.ToggleOverlay();
    ///     if (e.KeyCode == Keys.F11) RenderDebugModes.ToggleHitboxes();
    ///     if (e.KeyCode == Keys.F12) RenderDebugModes.TriggerQuickExport();
    ///
    /// ── Scene usage ──────────────────────────────────────────────────────────
    ///   In any scene's Draw():
    ///     if (RenderDebugModes.ShowCollision)  DrawCollisionBoxes(g);
    ///     if (RenderDebugModes.ShowHitboxes)   DrawHitboxes(g);
    /// </summary>
    public static class RenderDebugModes
    {
        // ── Feature toggles ────────────────────────────────────────────────────

        /// <summary>F9 — draw platform/hazard collision boxes as semi-transparent overlays.</summary>
        public static bool ShowCollision { get; private set; }

        /// <summary>F11 — draw entity attack hitboxes and hurt boxes.</summary>
        public static bool ShowHitboxes { get; private set; }

        /// <summary>Whether to draw the entity count badge.</summary>
        public static bool ShowEntityCount { get; set; } = true;

        /// <summary>Whether to draw input-state inspector row.</summary>
        public static bool ShowInputInspector { get; set; }

        // ── Entity count ───────────────────────────────────────────────────────
        /// <summary>
        /// Total live entities reported by the active scene.
        /// Scenes should call RenderDebugModes.ReportEntityCount(n) each update.
        /// </summary>
        public static int EntityCount { get; private set; }

        // ── Toggle methods ─────────────────────────────────────────────────────

        /// <summary>Toggles collision box visualisation (F9).</summary>
        public static void ToggleCollision()
        {
            if (!Game.Instance.GodMode) return;
            ShowCollision = !ShowCollision;
            DebugLogger.LogInfo("RenderDebugModes", $"ShowCollision={ShowCollision}");
        }

        /// <summary>Toggles hitbox visualisation (F11).</summary>
        public static void ToggleHitboxes()
        {
            if (!Game.Instance.GodMode) return;
            ShowHitboxes = !ShowHitboxes;
            DebugLogger.LogInfo("RenderDebugModes", $"ShowHitboxes={ShowHitboxes}");
        }

        /// <summary>Triggers a quick log export + screenshot (F12).</summary>
        public static void TriggerQuickExport()
        {
            DebugLogger.LogInfo("QuickExport", "F12 manual snapshot triggered.");
        }

        /// <summary>Scenes call this each Update to report live entity count.</summary>
        public static void ReportEntityCount(int count) => EntityCount = count;

        // ── Overlay draw ───────────────────────────────────────────────────────

        /// <summary>
        /// Draws active mode badges and entity count in a small bar below the
        /// performance profiler overlay.
        /// </summary>
        public static void DrawModeBadges(Graphics g, int W)
        {
            if (!Game.Instance.GodMode) return;

            int x = 2, y = 56;
            using (var f = new Font("Courier New", 8, FontStyle.Bold))
            {
                if (ShowCollision)
                    g.DrawString("[F9:COL]", f, Brushes.LimeGreen, x, y);
                if (ShowHitboxes)
                    g.DrawString("[F11:HIT]", f, Brushes.OrangeRed, x + 70, y);
                if (ShowEntityCount)
                    g.DrawString($"ENT:{EntityCount}", f, Brushes.LightGray, x + 150, y);
            }
        }

        /// <summary>
        /// Helper to draw a named collision rectangle in the debug color.
        /// Call from scene Draw() when ShowCollision is true.
        /// </summary>
        public static void DrawCollisionRect(Graphics g, Rectangle rect, string label = null)
        {
            using (var br = new SolidBrush(Color.FromArgb(60, 0, 200, 255)))
                g.FillRectangle(br, rect);
            using (var pen = new Pen(Color.FromArgb(180, 0, 200, 255), 1))
                g.DrawRectangle(pen, rect);
            if (label != null)
                using (var f = new Font("Courier New", 7))
                    g.DrawString(label, f, Brushes.Cyan, rect.X + 2, rect.Y + 2);
        }

        /// <summary>
        /// Helper to draw a named hitbox rectangle in the debug color.
        /// </summary>
        public static void DrawHitboxRect(Graphics g, Rectangle rect, string label = null)
        {
            using (var br = new SolidBrush(Color.FromArgb(70, 255, 40, 0)))
                g.FillRectangle(br, rect);
            using (var pen = new Pen(Color.FromArgb(200, 255, 80, 20), 1))
                g.DrawRectangle(pen, rect);
            if (label != null)
                using (var f = new Font("Courier New", 7))
                    g.DrawString(label, f, Brushes.OrangeRed, rect.X + 2, rect.Y + 2);
        }
    }
}
