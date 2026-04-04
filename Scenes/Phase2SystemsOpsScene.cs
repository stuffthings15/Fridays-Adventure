// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 8: Systems Programmer
// Feature: Systems Ops Scene
// Purpose: In-game validation panel for Team 8 Phase 2 systems services.
// ────────────────────────────────────────────────────────────────────────────

using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Runtime scene for Team 8 Phase 2 systems feature verification.
    /// </summary>
    public sealed class Phase2SystemsOpsScene : Scene
    {
        private readonly string[] _tabs = { "Localization", "Analytics", "Config", "DLC", "Patch", "CloudSave", "Mods", "Replay", "LangPack", "Stats" };
        private int _tab;
        private string _status = "Use tab actions.";

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 1 && input.IsPressed(System.Windows.Forms.Keys.A))
            {
                AnalyticsEventLogger.Log("sample", "from_ops_scene");
                _status = "Analytics events: " + AnalyticsEventLogger.Count();
            }
            if (_tab == 4 && input.IsPressed(System.Windows.Forms.Keys.P))
                _status = PatchManager.ApplyFirst();
            if (_tab == 5)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.U)) _status = "Upload snapshot: " + (CloudSaveIntegration.Upload() ?? "failed");
                if (input.IsPressed(System.Windows.Forms.Keys.D)) _status = "Download snapshot: " + (CloudSaveIntegration.Download() ? "ok" : "failed");
            }
            if (_tab == 7 && input.IsPressed(System.Windows.Forms.Keys.R))
                EventReplayRecorder.Record("sample_event");
            if (_tab == 8 && input.IsPressed(System.Windows.Forms.Keys.L))
                _status = "Language: " + LanguagePackSystem.CycleNext();

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(16, 18, 26))) g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 20, FontStyle.Bold)) g.DrawString("PHASE 2 SYSTEMS OPS", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);
            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36))) g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawLines(g, body, "Localization System", new[] { Phase2LocalizationSystem.T("ui.shop", "Cosmetics Shop"), _status }); break;
                case 1: DrawLines(g, body, "Analytics Event Logger", new[] { "A: log sample event", "Count=" + AnalyticsEventLogger.Count(), _status }); break;
                case 2: DrawLines(g, body, "Configuration Validator", ConfigurationValidator.Validate()); break;
                case 3: DrawLines(g, body, "DLC Content Loader", DlcContentLoader.GetPackages()); break;
                case 4: DrawLines(g, body, "Patch Manager", Merge(new[] { "P: apply first patch", _status }, PatchManager.Discover())); break;
                case 5: DrawLines(g, body, "Cloud Save Integration", new[] { "U: upload snapshot   D: download snapshot", _status }); break;
                case 6: DrawLines(g, body, "Mod Loader System", ModLoaderSystem.EnabledIds()); break;
                case 7: DrawLines(g, body, "Event Replay Recorder", Merge(new[] { "R: record sample event" }, EventReplayRecorder.Tail())); break;
                case 8: DrawLines(g, body, "Language Pack System", Merge(new[] { "L: cycle language", "Current=" + LanguagePackManager.CurrentLanguage, _status }, LanguagePackSystem.Available())); break;
                default: DrawLines(g, body, "Statistics Aggregator", StatisticsAggregator.Summary()); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right tab   Esc/Enter back   A/P/U/D/R/L actions", f, Brushes.DimGray, 14, H - 26);
        }

        private void DrawTabs(Graphics g, int W)
        {
            int x = 14;
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool sel = i == _tab;
                int w = 100;
                if (x + w > W - 20) break;
                var r = new Rectangle(x, 52, w, 28);
                using (var br = new SolidBrush(sel ? Color.FromArgb(70, 130, 220) : Color.FromArgb(40, 40, 55))) g.FillRectangle(br, r);
                g.DrawRectangle(sel ? Pens.Cyan : Pens.Gray, r);
                using (var f = new Font("Courier New", 8, FontStyle.Bold)) g.DrawString(_tabs[i], f, sel ? Brushes.Cyan : Brushes.LightGray, x + 6, 60);
                x += w + 6;
            }
        }

        private static void DrawLines(Graphics g, Rectangle body, string title, System.Collections.Generic.IReadOnlyList<string> lines)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString(title, f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var line in lines)
                {
                    g.DrawString("• " + line, f, Brushes.LightGray, body.X + 12, y);
                    y += 20;
                    if (y > body.Bottom - 12) break;
                }
        }

        private static string[] Merge(System.Collections.Generic.IReadOnlyList<string> a, System.Collections.Generic.IReadOnlyList<string> b)
        {
            var list = new System.Collections.Generic.List<string>();
            if (a != null) list.AddRange(a);
            if (b != null) list.AddRange(b);
            return list.ToArray();
        }
    }
}
