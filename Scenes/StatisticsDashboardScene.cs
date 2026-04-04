// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 9: UI Programmer / Team 2: Producer
// Feature: Statistics Dashboard + Performance Metrics Display
// Purpose: Presents session telemetry, resource monitor data, and build info.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Displays a compact runtime statistics dashboard for gameplay/QA review.
    /// </summary>
    public sealed class StatisticsDashboardScene : Scene
    {
        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            using (var br = new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);

            using (var f = new Font("Courier New", 22, FontStyle.Bold))
                g.DrawString("STATISTICS DASHBOARD", f, Brushes.Cyan, 18, 16);

            DrawPanel(g, 16, 58, W / 2 - 24, H - 100, "SESSION TELEMETRY", DrawSessionTelemetry);
            DrawPanel(g, W / 2 + 8, 58, W / 2 - 24, H - 100, "PERFORMANCE / RESOURCES", DrawPerfAndResource);

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Enter/Esc: Back", f, Brushes.DimGray, 16, H - 22);

            DrawDevMenuButton(g);
        }

        private static void DrawPanel(Graphics g, int x, int y, int w, int h, string title, Action<Graphics, int, int> body)
        {
            using (var br = new SolidBrush(Color.FromArgb(180, 16, 22, 34)))
                g.FillRectangle(br, x, y, w, h);
            using (var pen = new Pen(Color.DimGray, 1))
                g.DrawRectangle(pen, x, y, w, h);
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString(title, f, Brushes.Gold, x + 10, y + 8);
            body(g, x + 12, y + 34);
        }

        /// <summary>
        /// Draws session/player telemetry counters.
        /// </summary>
        /// <remarks>PHASE 2 - Team 9: Statistics Dashboard</remarks>
        private static void DrawSessionTelemetry(Graphics g, int x, int y)
        {
            var s = SessionStats.Instance;
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString($"Session ID: {s.SessionId}", f, Brushes.LightGray, x, y); y += 20;
                g.DrawString($"Play Time: {s.PlayTimeFormatted}", f, Brushes.LightGray, x, y); y += 20;
                g.DrawString($"Deaths: {s.DeathCount}", f, Brushes.LightGray, x, y); y += 20;
                g.DrawString($"Berries: {s.BerriesCollected}", f, Brushes.LightGray, x, y); y += 20;
                g.DrawString($"Enemies Defeated: {s.EnemiesDefeated}", f, Brushes.LightGray, x, y); y += 20;
                g.DrawString($"Bosses Defeated: {s.BossesDefeated}", f, Brushes.LightGray, x, y); y += 20;
                g.DrawString($"Longest Combo: {s.LongestCombo}", f, Brushes.LightGray, x, y); y += 20;
                g.DrawString($"Levels Completed: {s.LevelsCompleted}", f, Brushes.LightGray, x, y); y += 24;
                g.DrawString("Milestones:", f, Brushes.Cyan, x, y); y += 18;
                foreach (var m in s.UnlockedMilestones)
                {
                    g.DrawString("• " + m, f, Brushes.LightGray, x + 8, y);
                    y += 18;
                    if (y > Game.Instance.CanvasHeight - 70) break;
                }
            }
        }

        /// <summary>
        /// Draws performance metrics and resource monitor data.
        /// </summary>
        /// <remarks>PHASE 2 - Team 9: Performance Metrics Display / Team 2: Resource Monitor</remarks>
        private static void DrawPerfAndResource(Graphics g, int x, int y)
        {
            long mem = GC.GetTotalMemory(false);
            string frame = FrameTimeHistogram.GetSummary();
            int nl = frame.IndexOf('\n');
            if (nl > 0) frame = frame.Substring(0, nl);

            using (var f = new Font("Courier New", 10))
            {
                g.DrawString(BuildInfo.Summary, f, Brushes.LightGray, x, y); y += 32;
                g.DrawString($"Managed Heap: {mem / 1024.0 / 1024.0:F2} MB", f, Brushes.LightGray, x, y); y += 20;
                g.DrawString(TechLeadFeatures.GetGCInfo(), f, Brushes.LightGray, x, y); y += 20;
                g.DrawString(frame, f, Brushes.LightGray, x, y); y += 20;
                g.DrawString($"Draw Calls (last): {TechLeadFeatures.DrawCallCount}", f, Brushes.LightGray, x, y); y += 20;

                g.DrawString("Dependency Check:", f, Brushes.Cyan, x, y); y += 18;
                var missing = BuildInfo.CheckDependencies();
                if (missing.Count == 0)
                    g.DrawString("• All required dependencies present", f, Brushes.LightGray, x + 8, y);
                else
                    foreach (var d in missing)
                    {
                        g.DrawString("• Missing: " + d, f, Brushes.OrangeRed, x + 8, y);
                        y += 18;
                    }
            }
        }
    }
}
