using System.Drawing;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Scenes
{
    public sealed class PauseScene : Scene
    {
        private int _selected;
        private readonly string[] _options = { "Resume", "Options", "How to Play", "Save & Quit to Map", "Quit to Title" };

        public override void OnEnter() { Game.Instance.Audio.StopMusic(); }
        public override void OnExit()  { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Up)   && _selected > 0) _selected--;
            if (input.IsPressed(System.Windows.Forms.Keys.Down) && _selected < _options.Length-1) _selected++;
            if (input.PausePressed) Game.Instance.Scenes.Pop();
            if (input.InteractPressed) ActivateSelected();
        }

        public override void HandleClick(Point p)
        {
            int H = Game.Instance.CanvasHeight;
            for (int i = 0; i < _options.Length; i++)
            {
                float top = H * 0.38f + i * 48 - 6;
                if (p.Y >= top && p.Y < top + 38)
                {
                    _selected = i;
                    ActivateSelected();
                    return;
                }
            }
        }

        private void ActivateSelected()
        {
            switch (_selected)
            {
                case 0: Game.Instance.Scenes.Pop(); break;
                case 1: Game.Instance.Scenes.Push(new OptionsScene()); break;
                case 2: Game.Instance.Scenes.Push(new HowToPlayScene()); break;
                case 3:
                    Game.Instance.Save.Save();
                    while (!(Game.Instance.Scenes.Current is OverworldScene))
                        Game.Instance.Scenes.Pop();
                    break;
                case 4: Game.Instance.Scenes.Replace(new TitleScene()); break;
            }
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 28, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString("PAUSED", f);
                g.DrawString("PAUSED", f, Brushes.Cyan, (W-sz.Width)/2f, H*0.20f);
            }
            for (int i = 0; i < _options.Length; i++)
            {
                bool sel = i == _selected;
                using (var f = new Font("Courier New", 16, sel ? FontStyle.Bold : FontStyle.Regular))
                {
                    SizeF sz = g.MeasureString(_options[i], f);
                    Brush br = sel ? Brushes.Yellow : Brushes.White;
                    if (sel)
                        using (var high = new SolidBrush(Color.FromArgb(60, Color.Cyan)))
                            g.FillRectangle(high, (W-sz.Width)/2f - 14, H*0.38f + i*48 - 6, sz.Width+28, 38);
                    g.DrawString(_options[i], f, br, (W-sz.Width)/2f, H*0.38f + i*48);
                }
            }
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Up/Down Navigate   Enter Select   Esc Resume",
                             f, Brushes.DimGray, 12, H - 28);
        }
    }
}
