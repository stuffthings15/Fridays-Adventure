using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Scenes
{
    public sealed class TitleScene : Scene
    {
        private Bitmap    _bg;
        private float     _timer;
        private float     _promptBlink;
        private bool      _showPrompt = true;

        // Button rectangles — computed in Draw, used in HandleClick
        private Rectangle _optionsBtn;
        private Rectangle _exitBtn;

        // Secret password box
        private bool   _pwActive;
        private string _pwInput  = "";
        private float  _pwCursor;
        private const string Password = "Luffy";

        public override void OnEnter()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                       "Assets", "Sprites", "bg_title.png");
            if (File.Exists(path))
                _bg = new Bitmap(path);
            Game.Instance.Audio.PlayOverworld();
        }

        public override void OnExit()
        {
            _bg?.Dispose();
            _bg = null;
        }

        public override void Update(float dt)
        {
            _timer       += dt;
            _promptBlink += dt;
            if (_promptBlink >= 0.55f) { _showPrompt = !_showPrompt; _promptBlink = 0; }

            var input = Game.Instance.Input;

            // ── Password box ──────────────────────────────────────────────────
            string typed = input.ConsumeTyped();
            if (typed.Length > 0)
            {
                _pwActive  = true;
                _pwInput  += typed;
                if (_pwInput.Length > 20) _pwInput = _pwInput.Substring(_pwInput.Length - 20);
            }

            if (_pwActive)
            {
                _pwCursor += dt;

                if (input.IsPressed(System.Windows.Forms.Keys.Back) && _pwInput.Length > 0)
                    _pwInput = _pwInput.Substring(0, _pwInput.Length - 1);

                if (input.IsPressed(System.Windows.Forms.Keys.Escape))
                {
                    _pwInput  = "";
                    _pwActive = false;
                    return;
                }

                if (input.IsPressed(System.Windows.Forms.Keys.Return))
                {
                    if (string.Equals(_pwInput, Password, StringComparison.OrdinalIgnoreCase))
                    {
                        Game.Instance.GodMode = true;
                        Game.Instance.Scenes.Push(new DevMenuScene());
                    }
                    _pwInput  = "";
                    _pwActive = false;
                    return;
                }

                return; // block regular navigation while typing
            }

            // ── Regular navigation ────────────────────────────────────────────
            if (input.InteractPressed || input.AttackPressed || input.JumpPressed)
                Game.Instance.Scenes.Replace(new OverworldScene());

            if (input.IsPressed(System.Windows.Forms.Keys.L))
                Engine.Game.RequestOpenLogbook();

            if (input.PausePressed)
                Game.Instance.Scenes.Push(new OptionsScene());
        }

        public override void HandleClick(Point p)
        {
            if (_optionsBtn.Contains(p)) Game.Instance.Scenes.Push(new OptionsScene());
            if (_exitBtn.Contains(p))    Game.RequestClose();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            if (_bg != null)
                g.DrawImage(_bg, 0, 0, W, H);
            else
            {
                using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                    Color.FromArgb(8, 16, 50), Color.FromArgb(20, 60, 120), 90f))
                    g.FillRectangle(br, 0, 0, W, H);
            }

            // ── Title banner ──────────────────────────────────────────────────
            using (var br = new SolidBrush(Color.FromArgb(190, 255, 255, 255)))
                g.FillRectangle(br, 0, (int)(H * 0.10f), W, 190);

            using (var f = new Font("Courier New", 58, FontStyle.Bold))
            {
                const string title = "Miss Friday's Adventure";
                SizeF  sz = g.MeasureString(title, f);
                g.DrawString(title, f, Brushes.Black, (W - sz.Width) / 2f, H * 0.12f);
            }

            using (var f = new Font("Courier New", 15, FontStyle.Bold))
            {
                const string tag = "Ice-Ice Fruit  \u2022  The Sea Serpent  \u2022  The Grand Line";
                SizeF sz = g.MeasureString(tag, f);
                g.DrawString(tag, f, Brushes.DarkSlateGray, (W - sz.Width) / 2f, H * 0.12f + 80f);
            }

            // ── Press-to-start prompt ─────────────────────────────────────────
            if (_showPrompt)
            {
                using (var br = new SolidBrush(Color.FromArgb(190, 0, 0, 0)))
                    g.FillRectangle(br, 0, (int)(H * 0.64f), W, 54);
                using (var f = new Font("Courier New", 22, FontStyle.Bold))
                {
                    const string s = "Press  ENTER  or  Z  to  set  sail";
                    SizeF sz = g.MeasureString(s, f);
                    g.DrawString(s, f, Brushes.Yellow, (W - sz.Width) / 2f, H * 0.65f);
                }
            }

            // ── Options and Exit buttons ──────────────────────────────────────
            const int btnW = 180, btnH = 50;
            int btnY = (int)(H * 0.75f);
            _optionsBtn = new Rectangle(W / 2 - btnW - 14, btnY, btnW, btnH);
            _exitBtn    = new Rectangle(W / 2 + 14,        btnY, btnW, btnH);

            DrawButton(g, _optionsBtn, "OPTIONS",  Color.FromArgb(40, 80, 140));
            DrawButton(g, _exitBtn,    "EXIT",     Color.FromArgb(120, 30, 30));

            // ── Controls panel ────────────────────────────────────────────────
            const int panelH = 80;
            using (var br = new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
                g.FillRectangle(br, 0, H - panelH, W, panelH);
            g.DrawLine(Pens.White, 0, H - panelH, W, H - panelH);

            using (var f = new Font("Courier New", 13, FontStyle.Bold))
            {
                g.DrawString("Move: WASD / Arrows   Jump: Space / W   Attack: Z   Dodge: X",
                             f, Brushes.White, 14, H - panelH + 10);
                g.DrawString("Ice Wall: Q   Freeze: E   Interact: F / Enter   Pause: Esc   Logbook: L",
                             f, Brushes.LightCyan, 14, H - panelH + 38);
            }

            if (_pwActive) DrawPasswordBox(g, W, H);
        }

        private void DrawPasswordBox(Graphics g, int W, int H)
        {
            const int bw = 380, bh = 90;
            int bx = (W - bw) / 2, by = H / 2 - bh / 2;

            using (var br = new SolidBrush(Color.FromArgb(230, 0, 0, 0)))
                g.FillRectangle(br, bx, by, bw, bh);
            using (var pen = new Pen(Color.Cyan, 2))
                g.DrawRectangle(pen, bx, by, bw, bh);

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("ENTER SECRET CODE:", f, Brushes.Cyan, bx + 14, by + 10);

            string cursor  = (int)(_pwCursor / 0.45f) % 2 == 0 ? "|" : " ";
            string display = _pwInput + cursor;
            using (var f = new Font("Courier New", 18, FontStyle.Bold))
                g.DrawString(display, f, Brushes.White, bx + 14, by + 34);

            using (var f = new Font("Courier New", 9))
                g.DrawString("[Enter] Confirm   [Backspace] Delete   [Esc] Cancel",
                             f, Brushes.DimGray, bx + 14, by + 68);
        }

        private static void DrawButton(Graphics g, Rectangle r, string label, Color bg)
        {
            using (var br = new SolidBrush(bg))
                g.FillRectangle(br, r);
            g.DrawRectangle(Pens.White, r);
            using (var f = new Font("Courier New", 16, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(label, f);
                g.DrawString(label, f, Brushes.White,
                    r.X + (r.Width  - sz.Width)  / 2f,
                    r.Y + (r.Height - sz.Height) / 2f);
            }
        }
    }
}
