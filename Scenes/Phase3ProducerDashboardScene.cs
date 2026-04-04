// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 2: Producer / Project Manager
// Feature: Content Pipeline Dashboard Scene
// Purpose: Displays roadmap, leaderboard, calendar, and survey summaries in one
//          producer-facing dashboard panel.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using System.Linq;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Producer dashboard for Phase 3 foundational operations.
    /// </summary>
    public sealed class Phase3ProducerDashboardScene : Scene
    {
        private int _tab;
        private readonly string[] _tabs =
        {
            "Pipeline",
            "Roadmap",
            "Leaderboard",
            "Calendar",
            "Survey",
            "Revenue",
            "Quality",
            "Creator",
            "Beta"
        };

        private string _qualityGateCached;

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            // Quick survey submit test path
            if (_tab == 4 && input.IsPressed(System.Windows.Forms.Keys.S))
            {
                Phase3ProducerSystems.SubmitSurveyResponse("General", 5,
                    $"Quick-submit from dashboard at {DateTime.Now:HH:mm:ss}");
                SMB3Hud.ShowToast("Survey response recorded.");
            }

            // Quality gate quick run.
            if (_tab == 6 && input.IsPressed(System.Windows.Forms.Keys.G))
            {
                _qualityGateCached = string.Join("\n", Phase3ProducerSystems.RunQualityGateAutomation());
                SMB3Hud.ShowToast("Quality gate executed.");
            }

            // Beta tester quick register.
            if (_tab == 8 && input.IsPressed(System.Windows.Forms.Keys.B))
            {
                string alias = string.IsNullOrWhiteSpace(Game.Instance.PlayerName)
                    ? "tester"
                    : Game.Instance.PlayerName;
                Phase3ProducerSystems.RegisterBetaTester(alias, "balance");
                SMB3Hud.ShowToast("Beta tester registered.");
            }

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            using (var br = new SolidBrush(Color.FromArgb(18, 16, 30)))
                g.FillRectangle(br, 0, 0, W, H);

            using (var f = new Font("Courier New", 20, FontStyle.Bold))
                g.DrawString("PHASE 3 PRODUCER DASHBOARD", f, Brushes.Gold, 16, 10);

            DrawTabs(g, W);
            DrawBody(g, W, H);

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right: Switch Tab   Esc/Enter: Back   S:survey  G:quality  B:beta",
                    f, Brushes.DimGray, 14, H - 26);
        }

        private void DrawTabs(Graphics g, int W)
        {
            int x = 14;
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool sel = i == _tab;
                int w = 130;
                var r = new Rectangle(x, 52, w, 28);
                using (var br = new SolidBrush(sel ? Color.FromArgb(70, 130, 220) : Color.FromArgb(40, 40, 55)))
                    g.FillRectangle(br, r);
                g.DrawRectangle(sel ? Pens.Cyan : Pens.Gray, r);
                using (var f = new Font("Courier New", 9, FontStyle.Bold))
                    g.DrawString(_tabs[i], f, sel ? Brushes.Cyan : Brushes.LightGray, x + 10, 60);
                x += w + 8;
                if (x + w > W - 20) break;
            }
        }

        private void DrawBody(Graphics g, int W, int H)
        {
            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36)))
                g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawPipeline(g, body); break;
                case 1: DrawRoadmap(g, body); break;
                case 2: DrawLeaderboard(g, body); break;
                case 3: DrawCalendar(g, body); break;
                case 4: DrawSurvey(g, body); break;
                case 5: DrawRevenue(g, body); break;
                case 6: DrawQuality(g, body); break;
                case 7: DrawCreatorDashboard(g, body); break;
                default: DrawBetaProgram(g, body); break;
            }
        }

        private void DrawPipeline(Graphics g, Rectangle body)
        {
            var rows = Phase3ProducerSystems.GetPipelineTasks();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Content Pipeline Dashboard", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            int y = body.Y + 36;
            using (var f = new Font("Courier New", 10))
            {
                foreach (var r in rows)
                {
                    Brush sb = r.Status == "Done" ? Brushes.LimeGreen :
                               r.Status == "In Progress" ? Brushes.Gold :
                               r.Status == "Blocked" ? Brushes.OrangeRed : Brushes.LightGray;
                    g.DrawString($"[{r.Status}] {r.Name} ({r.OwnerTeam})", f, sb, body.X + 12, y);
                    y += 22;
                }
            }
        }

        private void DrawRoadmap(Graphics g, Rectangle body)
        {
            var lines = Phase3ProducerSystems.GetSeasonalRoadmap();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Seasonal Roadmap Display", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            int y = body.Y + 36;
            using (var f = new Font("Courier New", 10))
                foreach (var line in lines)
                {
                    g.DrawString("• " + line, f, Brushes.LightGray, body.X + 12, y);
                    y += 24;
                }
        }

        private void DrawLeaderboard(Graphics g, Rectangle body)
        {
            var rows = Phase3ProducerSystems.GetLeaderboard();
            if (!rows.Any())
                Phase3ProducerSystems.AddLeaderboardScore(Game.Instance.PlayerName ?? "Player", Game.Instance.PlayerBounty);
            rows = Phase3ProducerSystems.GetLeaderboard();

            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Player Stats Leaderboard", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            int y = body.Y + 36;
            int rank = 1;
            using (var f = new Font("Courier New", 10))
                foreach (var r in rows.Take(12))
                {
                    g.DrawString($"#{rank,2}  {r.Player,-14}  {r.Score,8:N0}  {r.TimestampUtc.ToLocalTime():MM-dd HH:mm}",
                        f, rank <= 3 ? Brushes.Gold : Brushes.LightGray, body.X + 12, y);
                    y += 20;
                    rank++;
                }
        }

        private void DrawCalendar(Graphics g, Rectangle body)
        {
            var rows = Phase3ProducerSystems.GetCommunityEvents();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Community Event Calendar", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            int y = body.Y + 36;
            using (var f = new Font("Courier New", 10))
                foreach (var r in rows.Take(14))
                {
                    g.DrawString($"{r.Date:yyyy-MM-dd}  {r.Name}", f, Brushes.Gold, body.X + 12, y);
                    y += 18;
                    g.DrawString($"   {r.Details}", f, Brushes.LightGray, body.X + 12, y);
                    y += 18;
                }
        }

        private void DrawSurvey(Graphics g, Rectangle body)
        {
            var rows = Phase3ProducerSystems.GetSurveySummary();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Player Survey System", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            using (var f = new Font("Courier New", 10))
                g.DrawString("Press S to submit quick sample response.", f, Brushes.Gold, body.X + 12, body.Y + 34);

            int y = body.Y + 62;
            using (var f = new Font("Courier New", 10))
                foreach (var line in rows.Take(16))
                {
                    g.DrawString("• " + line, f, Brushes.LightGray, body.X + 12, y);
                    y += 20;
                }
        }

        private void DrawRevenue(Graphics g, Rectangle body)
        {
            var rows = Phase3ProducerSystems.GetRevenueModelSnapshot();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Revenue Model System", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var line in rows)
                {
                    g.DrawString("• " + line, f, Brushes.LightGray, body.X + 12, y);
                    y += 22;
                }
        }

        private void DrawQuality(Graphics g, Rectangle body)
        {
            if (string.IsNullOrWhiteSpace(_qualityGateCached))
                _qualityGateCached = string.Join("\n", Phase3ProducerSystems.RunQualityGateAutomation());

            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Quality Gate Automation", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString("Press G to rerun checks.", f, Brushes.Gold, body.X + 12, body.Y + 34);

            using (var f = new Font("Courier New", 9))
                g.DrawString(_qualityGateCached, f, Brushes.LightGray, body.X + 12, body.Y + 58);
        }

        private void DrawCreatorDashboard(Graphics g, Rectangle body)
        {
            var rows = Phase3ProducerSystems.GetContentCreatorDashboardSummary();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Content Creator Dashboard", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var line in rows)
                {
                    g.DrawString("• " + line, f, Brushes.LightGray, body.X + 12, y);
                    y += 22;
                }
        }

        private void DrawBetaProgram(Graphics g, Rectangle body)
        {
            var rows = Phase3ProducerSystems.GetBetaProgramSummary();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Beta Testing Program", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString("Press B to register a quick tester using current player alias.",
                    f, Brushes.Gold, body.X + 12, body.Y + 34);

            int y = body.Y + 62;
            using (var f = new Font("Courier New", 10))
                foreach (var line in rows.Take(16))
                {
                    g.DrawString("• " + line, f, Brushes.LightGray, body.X + 12, y);
                    y += 20;
                }
        }
    }
}
