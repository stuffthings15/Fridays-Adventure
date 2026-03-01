using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Scenes
{
    public sealed class DevMenuScene : Scene
    {
        private struct LevelEntry
        {
            public string     Label;
            public Func<Scene> Create;
        }

        private List<LevelEntry> _levels;
        private int _sel;

        public override void OnEnter()
        {
            _levels = new List<LevelEntry>
            {
                new LevelEntry { Label = "Overworld Map",              Create = () => new OverworldScene() },
                new LevelEntry { Label = "Dinosaur Island",            Create = () => new IslandScene("dino",  "Dinosaur Island") },
                new LevelEntry { Label = "Storm Belt",                 Create = () => new StormScene() },
                new LevelEntry { Label = "Sky Island",                 Create = () => new SkyIslandScene() },
                new LevelEntry { Label = "Blade Nation",               Create = () => new IslandScene("wano",  "Blade Nation") },
                new LevelEntry { Label = "Marine Blockade  (Boss)",    Create = () => new BossScene() },
                new LevelEntry { Label = "Warlord: Lord Sudo  (Boss)", Create = () => new WarlordBossScene(WarlordConfig.FireLordSudo()) },
            };
        }

        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Up)   && _sel > 0)               _sel--;
            if (input.IsPressed(System.Windows.Forms.Keys.Down) && _sel < _levels.Count - 1) _sel++;

            if (input.InteractPressed)
            {
                Game.Instance.Scenes.Replace(_levels[_sel].Create());
                return;
            }

            if (input.PausePressed)
                Game.Instance.Scenes.Pop();
        }

        public override void HandleClick(Point p)
        {
            int H = Game.Instance.CanvasHeight;
            for (int i = 0; i < _levels.Count; i++)
            {
                float top = H * 0.30f + i * 52 - 6;
                if (p.Y >= top && p.Y < top + 46)
                {
                    _sel = i;
                    Game.Instance.Scenes.Replace(_levels[_sel].Create());
                    return;
                }
            }
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Dark green developer background
            using (var br = new LinearGradientBrush(new System.Drawing.Rectangle(0, 0, W, H),
                Color.FromArgb(0, 18, 0), Color.FromArgb(0, 40, 10), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // Header
            using (var f = new Font("Courier New", 24, FontStyle.Bold))
            {
                const string title = "[ DEV  LEVEL  SELECT ]";
                SizeF sz = g.MeasureString(title, f);
                g.DrawString(title, f, Brushes.LimeGreen, (W - sz.Width) / 2f, H * 0.07f);
            }

            // God-mode indicator badge
            const string badge = "\u26a1  GOD MODE ACTIVE  \u26a1";
            using (var br = new SolidBrush(Color.FromArgb(180, 80, 60, 0)))
                g.FillRectangle(br, W / 2 - 160, (int)(H * 0.17f) - 4, 320, 32);
            using (var f = new Font("Courier New", 13, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(badge, f);
                g.DrawString(badge, f, Brushes.Yellow, (W - sz.Width) / 2f, H * 0.17f);
            }

            // Level entries
            for (int i = 0; i < _levels.Count; i++)
            {
                bool  sel = i == _sel;
                float ty  = H * 0.30f + i * 52;

                if (sel)
                {
                    using (var br = new SolidBrush(Color.FromArgb(70, Color.LimeGreen)))
                        g.FillRectangle(br, 0, ty - 6, W, 46);
                    using (var pen = new Pen(Color.FromArgb(120, Color.LimeGreen), 1))
                        g.DrawLine(pen, 0, ty - 6, W, ty - 6);
                }

                using (var f = new Font("Courier New", 15, sel ? FontStyle.Bold : FontStyle.Regular))
                {
                    SizeF sz = g.MeasureString(_levels[i].Label, f);
                    Brush br = sel ? Brushes.LimeGreen : Brushes.DarkSeaGreen;
                    if (sel) g.DrawString("\u25b6", f, Brushes.LimeGreen,
                                          (W - sz.Width) / 2f - 28, ty);
                    g.DrawString(_levels[i].Label, f, br, (W - sz.Width) / 2f, ty);
                }
            }

            // Footer hint
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Up/Down  Navigate     Enter / Click  Launch     Esc  Back to Title",
                             f, Brushes.DimGray, 12, H - 26);
        }
    }
}
