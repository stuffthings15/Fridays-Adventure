// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 2: Producer
// Feature: Producer Dashboard Scene
// Purpose: In-game dashboard for weekly challenge, roadmap, feedback, test mode,
//          and telemetry summary.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Producer dashboard for Phase 2 operations tooling.
    /// </summary>
    public sealed class Phase2ProducerDashboardScene : Scene
    {
        private readonly string[] _tabs =
        {
            "Challenge",
            "Roadmap",
            "Feedback",
            "Test Mode",
            "Telemetry",
            "Milestones",
            "Session Rec",
            "Broadcast"
        };
        private int _tab;

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 2 && input.IsPressed(System.Windows.Forms.Keys.F))
            {
                PlayerFeedbackPortal.Submit("General", "Quick feedback from producer dashboard.");
                SMB3Hud.ShowToast("Feedback submitted.");
            }

            if (_tab == 3 && input.IsPressed(System.Windows.Forms.Keys.T))
            {
                TestModeSelector.Next();
                SMB3Hud.ShowToast("Test mode: " + TestModeSelector.Current);
            }

            if (_tab == 6 && input.IsPressed(System.Windows.Forms.Keys.R))
            {
                SessionRecording.RecordSnapshot("dashboard");
                SMB3Hud.ShowToast("Session snapshot recorded.");
            }

            if (_tab == 7 && input.IsPressed(System.Windows.Forms.Keys.B))
            {
                CommunicationBroadcast.Push("Roadmap sync at " + DateTime.Now.ToString("HH:mm:ss"));
                SMB3Hud.ShowToast("Broadcast queued.");
            }

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(20, 16, 28))) g.FillRectangle(br, 0, 0, W, H);

            using (var f = new Font("Courier New", 20, FontStyle.Bold))
                g.DrawString("PHASE 2 PRODUCER DASHBOARD", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);

            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36))) g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawChallenge(g, body); break;
                case 1: DrawRoadmap(g, body); break;
                case 2: DrawFeedback(g, body); break;
                case 3: DrawTestMode(g, body); break;
                case 4: DrawTelemetry(g, body); break;
                case 5: DrawMilestones(g, body); break;
                case 6: DrawSessionRecording(g, body); break;
                default: DrawBroadcast(g, body); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right: tab   Esc/Enter: back   F/T/R/B actions per tab", f, Brushes.DimGray, 14, H - 26);
        }

        private void DrawTabs(Graphics g, int W)
        {
            int x = 14;
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool sel = i == _tab;
                int w = 140;
                if (x + w > W - 20) break;
                var r = new Rectangle(x, 52, w, 28);
                using (var br = new SolidBrush(sel ? Color.FromArgb(70, 130, 220) : Color.FromArgb(40, 40, 55)))
                    g.FillRectangle(br, r);
                g.DrawRectangle(sel ? Pens.Cyan : Pens.Gray, r);
                using (var f = new Font("Courier New", 9, FontStyle.Bold))
                    g.DrawString(_tabs[i], f, sel ? Brushes.Cyan : Brushes.LightGray, x + 8, 60);
                x += w + 8;
            }
        }

        private static void DrawChallenge(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Weekly Challenge Generator", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString(WeeklyChallengeGenerator.Current(), f, Brushes.LightGray, body.X + 12, body.Y + 40);
        }

        private static void DrawRoadmap(Graphics g, Rectangle body)
        {
            var rows = ContentRoadmapDisplay.GetItems();
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Content Roadmap Display", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var r in rows)
                {
                    g.DrawString("• " + r, f, Brushes.LightGray, body.X + 12, y);
                    y += 22;
                }
        }

        private static void DrawFeedback(Graphics g, Rectangle body)
        {
            var rows = PlayerFeedbackPortal.Summary();
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Player Feedback Portal", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString("Press F to submit a sample feedback row.", f, Brushes.Gold, body.X + 12, body.Y + 34);
            int y = body.Y + 60;
            using (var f = new Font("Courier New", 10))
                foreach (var r in rows)
                {
                    g.DrawString("• " + r, f, Brushes.LightGray, body.X + 12, y);
                    y += 20;
                }
        }

        private static void DrawTestMode(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Test Mode Selector", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("Press T to cycle mode.", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString("Current: " + TestModeSelector.Current, f, Brushes.LightGray, body.X + 12, body.Y + 60);
            }
        }

        private static void DrawTelemetry(Graphics g, Rectangle body)
        {
            var rows = TelemetryDashboard.Summary();
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Telemetry Dashboard", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var r in rows)
                {
                    g.DrawString("• " + r, f, Brushes.LightGray, body.X + 12, y);
                    y += 20;
                }
        }

        private static void DrawMilestones(Graphics g, Rectangle body)
        {
            var rows = MilestoneTracker.GetMilestones();
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Milestone Tracker", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var r in rows)
                {
                    g.DrawString("• " + r, f, Brushes.LightGray, body.X + 12, y);
                    y += 20;
                }
        }

        private static void DrawSessionRecording(Graphics g, Rectangle body)
        {
            var rows = SessionRecording.Tail();
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Session Recording", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString("Press R to record a snapshot.", f, Brushes.Gold, body.X + 12, body.Y + 34);
            int y = body.Y + 60;
            using (var f = new Font("Courier New", 9))
                foreach (var r in rows)
                {
                    g.DrawString("• " + r, f, Brushes.LightGray, body.X + 12, y);
                    y += 18;
                }
        }

        private static void DrawBroadcast(Graphics g, Rectangle body)
        {
            var rows = CommunicationBroadcast.GetAll();
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Communication Broadcast", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString("Press B to queue sample broadcast.", f, Brushes.Gold, body.X + 12, body.Y + 34);
            int y = body.Y + 60;
            using (var f = new Font("Courier New", 10))
            {
                if (rows.Count == 0) g.DrawString("• No broadcast messages yet.", f, Brushes.LightGray, body.X + 12, y);
                else
                    foreach (var r in rows)
                    {
                        g.DrawString("• " + r, f, Brushes.LightGray, body.X + 12, y);
                        y += 18;
                    }
            }
        }
    }
}
