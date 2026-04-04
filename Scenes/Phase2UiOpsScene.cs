// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 9: UI Programmer
// Feature: UI Ops Scene
// Purpose: In-game validation panel for remaining Team 9 Phase 2 UI features.
// ────────────────────────────────────────────────────────────────────────────

using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Runtime scene for Team 9 Phase 2 UI feature verification.
    /// </summary>
    public sealed class Phase2UiOpsScene : Scene
    {
        private readonly string[] _tabs = { "MiniMap", "Tutorial", "Notify", "Keybinds", "Chat", "Gallery" };
        private int _tab;

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 1 && input.IsPressed(System.Windows.Forms.Keys.T)) TutorialOverlaySystem.Toggle();
            if (_tab == 2 && input.IsPressed(System.Windows.Forms.Keys.N)) NotificationSystem.Push("Sample notification " + System.DateTime.Now.ToString("HH:mm:ss"));
            if (_tab == 3 && input.IsPressed(System.Windows.Forms.Keys.K)) KeybindCustomizationSystem.Set("Attack", "X");
            if (_tab == 4 && input.IsPressed(System.Windows.Forms.Keys.C)) ChatMessageSystem.Post("Dev", "Systems check OK");

            if (input.PausePressed || input.InteractPressed) Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(16, 18, 26))) g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 20, FontStyle.Bold)) g.DrawString("PHASE 2 UI OPS", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);
            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36))) g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawLines(g, body, "Mini-map Display", new[] { MiniMapDisplaySystem.PositionLabel(128f, 256f) }); break;
                case 1: DrawLines(g, body, "Tutorial Overlay", new[] { "T: toggle", "Enabled=" + TutorialOverlaySystem.Enabled }); break;
                case 2: DrawLines(g, body, "Notification System", Merge(new[] { "N: push sample" }, NotificationSystem.Snapshot())); break;
                case 3: DrawLines(g, body, "Keybind Customization", Merge(new[] { "K: set Attack -> X" }, KeybindCustomizationSystem.GetLines())); break;
                case 4: DrawLines(g, body, "Chat/Message System", Merge(new[] { "C: post sample" }, ChatMessageSystem.History())); break;
                default: DrawLines(g, body, "Screenshot Gallery", ScreenshotGallerySystem.ListShots()); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right tab   Esc/Enter back   T/N/K/C actions", f, Brushes.DimGray, 14, H - 26);
        }

        private void DrawTabs(Graphics g, int W)
        {
            int x = 14;
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool sel = i == _tab;
                int w = 120;
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
