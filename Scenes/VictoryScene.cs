using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Standalone victory screen shown after a boss is defeated.
    /// Displays score summary, rewards, and provides Menu / Continue buttons
    /// as required by the game specification.
    /// </summary>
    public sealed class VictoryScene : Scene
    {
        private readonly string _title;           // e.g. "CAPTAIN DEFEATED!"
        private readonly string _subtitle;        // e.g. "+2000 Bounty  +2 Crew Bonds"
        private readonly int    _finalScore;
        private readonly int    _totalBerries;
        private readonly Action _onContinue;      // what "Continue" does (pop, replace, etc.)

        private float     _timer;
        private Rectangle _menuBtn;
        private Rectangle _continueBtn;

        private static readonly Font _titleFont = new Font("Courier New", 26, FontStyle.Bold);
        private static readonly Font _subFont   = new Font("Courier New", 12, FontStyle.Bold);
        private static readonly Font _bodyFont  = new Font("Courier New", 11);
        private static readonly Font _btnFont   = new Font("Courier New", 14, FontStyle.Bold);

        /// <summary>
        /// Creates a new VictoryScene.
        /// </summary>
        /// <param name="title">Large heading text (e.g. "CAPTAIN DEFEATED!").</param>
        /// <param name="subtitle">Reward summary line.</param>
        /// <param name="onContinue">Action invoked when Continue is pressed. If null, pops to previous scene.</param>
        public VictoryScene(string title, string subtitle, Action onContinue = null)
        {
            _title      = title;
            _subtitle   = subtitle;
            _finalScore   = Game.Instance.PlayerBounty;
            _totalBerries = Game.Instance.TotalBerriesCollected;
            _onContinue   = onContinue;
        }

        public override void OnEnter()
        {
            // Play a triumphant hub track during the victory screen
            Game.Instance.Audio.ContinueOrPlay("hub");
        }

        public override void OnExit() { }

        public override void Update(float dt)
        {
            _timer += dt;
            if (_timer < 0.6f) return; // brief input delay

            var input = Game.Instance.Input;

            // Keyboard: Enter/Z continues, Esc goes to menu
            if (input.InteractPressed || input.AttackPressed)
                DoContinue();
            if (input.PausePressed)
                GoToMenu();
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
            if (_timer < 0.6f) return;

            if (_continueBtn.Contains(p)) DoContinue();
            if (_menuBtn.Contains(p))     GoToMenu();
        }

        /// <summary>Proceeds to the next scene (overworld, credits, etc.).</summary>
        private void DoContinue()
        {
            if (_onContinue != null)
                _onContinue();
            else
                Game.Instance.Scenes.Pop();
        }

        /// <summary>Returns to the main menu (title screen).</summary>
        private void GoToMenu()
        {
            Game.Instance.Scenes.Replace(new TitleScene());
        }

        // ── Draw ─────────────────────────────────────────────────────────────

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // ── Dark gradient background ─────────────────────────────────────
            using (var br = new LinearGradientBrush(
                new Rectangle(0, 0, W, H),
                Color.FromArgb(8, 20, 50), Color.FromArgb(20, 60, 40), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // ── Gold starburst decoration ────────────────────────────────────
            float pulse = (float)(0.5 + 0.5 * Math.Sin(_timer * 2.0));
            int glowAlpha = (int)(30 + 25 * pulse);
            using (var br = new SolidBrush(Color.FromArgb(glowAlpha, Color.Gold)))
                g.FillEllipse(br, W / 2 - 200, 30, 400, 200);

            // ── Victory title ────────────────────────────────────────────────
            SizeF tsz = g.MeasureString(_title, _titleFont);
            g.DrawString(_title, _titleFont, Brushes.Gold,
                (W - tsz.Width) / 2f, H * 0.12f);

            // ── Subtitle / reward summary ────────────────────────────────────
            SizeF ssz = g.MeasureString(_subtitle, _subFont);
            g.DrawString(_subtitle, _subFont, Brushes.White,
                (W - ssz.Width) / 2f, H * 0.22f);

            // ── Score summary panel ──────────────────────────────────────────
            int panelW = 380, panelH = 140;
            int px = (W - panelW) / 2, py = (int)(H * 0.32f);
            using (var br = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                g.FillRectangle(br, px, py, panelW, panelH);
            using (var pen = new Pen(Color.Gold, 2))
                g.DrawRectangle(pen, px, py, panelW, panelH);

            int ty = py + 14;
            g.DrawString($"Final Score:     {_finalScore:N0}", _bodyFont, Brushes.Gold, px + 20, ty);
            ty += 28;
            g.DrawString($"Berries:         {_totalBerries}", _bodyFont, Brushes.Gold, px + 20, ty);
            ty += 28;
            g.DrawString($"Crew Bonds:      {Game.Instance.CrewBonds}", _bodyFont, Brushes.Cyan, px + 20, ty);
            ty += 28;
            g.DrawString($"Character:       {Game.Instance.SelectedCharacter}", _bodyFont, Brushes.LightGray, px + 20, ty);

            // ── Buttons ──────────────────────────────────────────────────────
            const int btnW = 160, btnH = 48;
            int btnY = (int)(H * 0.72f);
            _continueBtn = new Rectangle(W / 2 - btnW - 16, btnY, btnW, btnH);
            _menuBtn     = new Rectangle(W / 2 + 16,        btnY, btnW, btnH);

            // Continue button (green)
            DrawButton(g, _continueBtn, "CONTINUE", Color.FromArgb(30, 120, 30));
            // Menu button (red-ish)
            DrawButton(g, _menuBtn, "MAIN MENU", Color.FromArgb(120, 30, 30));

            // ── Control hints ────────────────────────────────────────────────
            if (_timer > 0.6f)
            {
                g.DrawString("[Enter/Z] Continue   [Esc] Main Menu",
                    _bodyFont, Brushes.DimGray, W / 2 - 160, H - 30);
            }

            DrawDevMenuButton(g);
        }

        /// <summary>Draws a styled button rectangle with centered label text.</summary>
        private void DrawButton(Graphics g, Rectangle rect, string label, Color bgColor)
        {
            using (var br = new SolidBrush(Color.FromArgb(200, bgColor)))
                g.FillRectangle(br, rect);
            using (var pen = new Pen(Color.White, 2))
                g.DrawRectangle(pen, rect);
            SizeF sz = g.MeasureString(label, _btnFont);
            g.DrawString(label, _btnFont, Brushes.White,
                rect.X + (rect.Width - sz.Width) / 2f,
                rect.Y + (rect.Height - sz.Height) / 2f);
        }
    }
}
