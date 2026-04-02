using System.Drawing;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Scenes
{
    public sealed class HowToPlayScene : Scene
    {
        private int _scroll;

        private static readonly string[] _lines = new[]
        {
            "HOW TO PLAY",
            "",
            "MOVEMENT",
            "  Move .............. WASD / Arrow Keys",
            "  Jump .............. Space / W / Up",
            "  Short Hop ......... Tap jump quickly",
            "  Full Jump ......... Hold jump button",
            "  Dodge ............. X / K",
            "",
            "COMBAT",
            "  Attack ............ Z / J",
            "  Head Stomp ........ Jump on enemies!",
            "                      (2x damage + bounce)",
            "  Ice Wall .......... Q  (costs 20 ice)",
            "  Flash Freeze ...... E  (costs 30 ice)",
            "  Break Wall ........ R  (no ice cost,",
            "                          works while suppressed)",
            "",
            "NAVIGATION",
            "  Interact / Confirm  F / Enter",
            "  Pause ............. Esc",
            "  Logbook ........... L",
            "  Select node ....... Click on island",
            "",
            "GAME TIPS",
            "  - Stomp enemies from above for 2x",
            "    damage and a free bounce! (SMB3)",
            "  - Tap jump for short hops, hold for",
            "    full height. Master both!",
            "  - Collect gold coins on platforms",
            "    for bonus bounty.",
            "  - Water pits drain your health! Mash",
            "    SPACE/Z/X to escape sinking.",
            "  - Sea-Stone zones suppress ice powers.",
            "  - Fire sources can melt your ice walls.",
            "  - Freeze enemies with E then attack!",
            "  - Explore all islands to build Crew",
            "    Bonds and increase your Bounty.",
            "  - Higher bounty means more Marines.",
            "",
            "Press  ESC  or  ENTER  to go back."
        };

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Up))
                _scroll = System.Math.Max(0, _scroll - 1);
            if (input.IsPressed(System.Windows.Forms.Keys.Down))
                _scroll = System.Math.Min(System.Math.Max(0, _lines.Length - 18), _scroll + 1);
            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void HandleMouseWheel(int delta)
        {
            if (delta > 0)
                _scroll = System.Math.Max(0, _scroll - 1);
            else if (delta < 0)
                _scroll = System.Math.Min(System.Math.Max(0, _lines.Length - 18), _scroll + 1);
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
            Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            using (var br = new SolidBrush(Color.FromArgb(220, 0, 0, 20)))
                g.FillRectangle(br, 0, 0, W, H);

            float y = 30f;
            for (int i = _scroll; i < _lines.Length && y < H - 40; i++)
            {
                string line = _lines[i];
                bool isHeader = line.Length > 0 && line == line.ToUpper() && !line.StartsWith(" ");
                Font f = isHeader
                    ? new Font("Courier New", 16, FontStyle.Bold)
                    : new Font("Courier New", 13);
                Brush br = isHeader ? Brushes.Cyan : Brushes.White;
                float tx = isHeader ? (W - g.MeasureString(line, f).Width) / 2f : 80f;
                g.DrawString(line, f, br, tx, y);
                y += isHeader ? 36f : 26f;
                f.Dispose();
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Up/Down to scroll", f, Brushes.DimGray, 12, H - 22);
            DrawDevMenuButton(g);
        }
    }
}
