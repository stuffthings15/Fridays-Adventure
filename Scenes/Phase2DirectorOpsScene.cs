// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 1: Game Director
// Feature: Director Ops Scene
// Purpose: In-game validation panel for remaining Team 1 Phase 2 features.
// ────────────────────────────────────────────────────────────────────────────

using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Runtime scene for Team 1 Phase 2 feature verification.
    /// </summary>
    public sealed class Phase2DirectorOpsScene : Scene
    {
        private readonly string[] _tabs = { "Seasonal", "Speedrun", "Mixer", "Cheats", "Demo", "Replay", "Captions", "Theme" };
        private int _tab;
        private int _demoFrame;
        private string _status = "Use tab actions.";

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            SpeedRunTimerSystem.Tick(dt);
            _demoFrame++;

            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 1)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.S)) SpeedRunTimerSystem.Start();
                if (input.IsPressed(System.Windows.Forms.Keys.X)) SpeedRunTimerSystem.Stop();
                if (input.IsPressed(System.Windows.Forms.Keys.R)) SpeedRunTimerSystem.Reset();
            }
            if (_tab == 2 && input.IsPressed(System.Windows.Forms.Keys.M))
            {
                var p = SoundtrackMixerSystem.Profiles();
                int i = -1;
                for (int k = 0; k < p.Count; k++) if (p[k] == SoundtrackMixerSystem.Profile) { i = k; break; }
                int n = (i + 1) % p.Count;
                SoundtrackMixerSystem.SetProfile(p[n]);
            }
            if (_tab == 3 && input.IsPressed(System.Windows.Forms.Keys.C))
                _status = "infinite_lives=" + CheatsMenuSystem.Toggle("infinite_lives");
            if (_tab == 5 && input.IsPressed(System.Windows.Forms.Keys.P))
            {
                ReplaySystemPhase2.RecordMeta("ops_marker");
                _status = "Replay metadata recorded.";
            }
            if (_tab == 6 && input.IsPressed(System.Windows.Forms.Keys.T)) CaptionSystem.Toggle();
            if (_tab == 7 && input.IsPressed(System.Windows.Forms.Keys.H))
            {
                var th = ThemeCustomizationSystem.Themes();
                int i = -1;
                for (int k = 0; k < th.Count; k++) if (th[k] == ThemeCustomizationSystem.Current) { i = k; break; }
                int n = (i + 1) % th.Count;
                ThemeCustomizationSystem.Set(th[n]);
            }

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(16, 18, 26))) g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 20, FontStyle.Bold)) g.DrawString("PHASE 2 DIRECTOR OPS", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);
            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36))) g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawLines(g, body, "Seasonal World Themes", new[] { "Current theme: " + SeasonalWorldThemesSystem.CurrentTheme() }); break;
                case 1: DrawLines(g, body, "Speed Run Timer", new[] { "S=start X=stop R=reset", "Running=" + SpeedRunTimerSystem.Running, "Time=" + SpeedRunTimerSystem.Formatted() }); break;
                case 2: DrawLines(g, body, "Soundtrack Mixer", new[] { "M: cycle profile", "Profile=" + SoundtrackMixerSystem.Profile }); break;
                case 3: DrawLines(g, body, "Cheats Menu", Merge(new[] { "C: toggle infinite_lives", _status }, CheatsMenuSystem.Status())); break;
                case 4: DrawLines(g, body, "Demo Mode", new[] { "Script action: " + DemoModeSystem.ScriptAt(_demoFrame) }); break;
                case 5: DrawLines(g, body, "Replay System", Merge(new[] { "P: record replay metadata", _status }, ReplaySystemPhase2.Tail())); break;
                case 6: DrawLines(g, body, "Caption System", new[] { "T: toggle captions", "Enabled=" + CaptionSystem.Enabled, CaptionSystem.Line("Captain: Full speed ahead!") }); break;
                default: DrawLines(g, body, "Theme Customization", new[] { "H: cycle theme", "Current=" + ThemeCustomizationSystem.Current }); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right tab   Esc/Enter back   S/X/R/M/C/P/T/H actions", f, Brushes.DimGray, 14, H - 26);
        }

        private void DrawTabs(Graphics g, int W)
        {
            int x = 14;
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool sel = i == _tab;
                int w = 110;
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
