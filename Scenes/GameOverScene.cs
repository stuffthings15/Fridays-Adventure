using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Defeat screen — shows final score, offers "Try Again" to restart the level
    /// and "Main Menu" to return to the title screen.
    /// </summary>
    public sealed class GameOverScene : Scene
    {
        /// <summary>
        /// Optional factory that creates a fresh scene to retry the failed level.
        /// When null, the Try Again button is hidden.
        /// </summary>
        private readonly Func<Scene> _retryFactory;

        private float     _timer;
        private int       _selected;        // 0 = Try Again, 1 = Main Menu
        private Rectangle _retryBtn;
        private Rectangle _menuBtn;

        private static readonly Font _titleFont = new Font("Courier New", 30, FontStyle.Bold);
        private static readonly Font _bodyFont  = new Font("Courier New", 11);
        private static readonly Font _scoreFont = new Font("Courier New", 13, FontStyle.Bold);
        private static readonly Font _btnFont   = new Font("Courier New", 14, FontStyle.Bold);
        private static readonly Font _hintFont  = new Font("Courier New", 10);

        /// <summary>
        /// Creates the Game Over scene.
        /// </summary>
        /// <param name="retryFactory">
        /// Factory that produces a fresh scene of the level just failed.
        /// Pass null to disable the "Try Again" option.
        /// </param>
        public GameOverScene(Func<Scene> retryFactory = null)
        {
            _retryFactory = retryFactory;
        }

        public override void OnEnter() { Game.Instance.Audio.StopMusic(); }
        public override void OnExit()  { }

        public override void Update(float dt)
        {
            _timer += dt;
            if (_timer < 0.8f) return;

            var input = Game.Instance.Input;

            // Allow keyboard selection between buttons
            bool hasRetry = _retryFactory != null;
            if (hasRetry)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.Left)  && _selected > 0) _selected--;
                if (input.IsPressed(System.Windows.Forms.Keys.Right) && _selected < 1) _selected++;
            }
            else
            {
                _selected = 1; // only Main Menu available
            }

            // Confirm selection
            if (input.InteractPressed || input.AttackPressed)
                ActivateSelected();
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
            if (_timer < 0.8f) return;

            if (_retryFactory != null && _retryBtn.Contains(p))
            { _selected = 0; ActivateSelected(); return; }
            if (_menuBtn.Contains(p))
            { _selected = 1; ActivateSelected(); return; }
        }

        /// <summary>Executes the currently selected button action.</summary>
        private void ActivateSelected()
        {
            if (_selected == 0 && _retryFactory != null)
            {
                // Restart the failed level
                Game.Instance.Scenes.Replace(_retryFactory());
            }
            else
            {
                // Exit / Menu — go to high scores then title
                Game.Instance.Scenes.Replace(new HighScoreScene(
                    Game.Instance.PlayerBounty,
                    Game.Instance.TotalBerriesCollected));
            }
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Dark background
            using (var br = new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);

            // Title
            SizeF tsz = g.MeasureString("GAME  OVER", _titleFont);
            g.DrawString("GAME  OVER", _titleFont, Brushes.Crimson,
                (W - tsz.Width) / 2f, H * 0.22f);

            // Flavour text
            SizeF fsz = g.MeasureString("The sea remembers those who dare it.", _bodyFont);
            g.DrawString("The sea remembers those who dare it.", _bodyFont,
                Brushes.LightGray, (W - fsz.Width) / 2f, H * 0.36f);

            // Score summary
            string scoreText = $"Final Score: {Game.Instance.PlayerBounty:N0}     Berries: {Game.Instance.TotalBerriesCollected}";
            SizeF ssz = g.MeasureString(scoreText, _scoreFont);
            g.DrawString(scoreText, _scoreFont, Brushes.Gold,
                (W - ssz.Width) / 2f, H * 0.46f);

            // ── Buttons (shown after input delay) ────────────────────────────
            if (_timer > 0.8f)
            {
                const int btnW = 170, btnH = 46;
                int btnY = (int)(H * 0.60f);
                bool hasRetry = _retryFactory != null;

                if (hasRetry)
                {
                    // Two buttons side by side: Try Again | Main Menu
                    _retryBtn = new Rectangle(W / 2 - btnW - 12, btnY, btnW, btnH);
                    _menuBtn  = new Rectangle(W / 2 + 12,        btnY, btnW, btnH);
                    DrawButton(g, _retryBtn, "TRY AGAIN",
                        Color.FromArgb(30, 120, 30), _selected == 0);
                    DrawButton(g, _menuBtn, "MAIN MENU",
                        Color.FromArgb(120, 30, 30), _selected == 1);

                    // Control hints
                    g.DrawString("←/→ Select    Z / Enter — Confirm",
                        _hintFont, Brushes.DimGray, W / 2 - 140, btnY + btnH + 16);
                }
                else
                {
                    // Single centered Main Menu button
                    _menuBtn = new Rectangle(W / 2 - btnW / 2, btnY, btnW, btnH);
                    DrawButton(g, _menuBtn, "MAIN MENU",
                        Color.FromArgb(120, 30, 30), true);

                    g.DrawString("Z / Enter — Continue",
                        _hintFont, Brushes.DimGray, W / 2 - 80, btnY + btnH + 16);
                }
            }

            DrawDevMenuButton(g);
        }

        /// <summary>Draws a styled button rectangle with centered label text.</summary>
        private void DrawButton(Graphics g, Rectangle rect, string label,
            Color bgColor, bool selected)
        {
            // Highlight selected button
            Color bg = selected
                ? Color.FromArgb(220, bgColor)
                : Color.FromArgb(120, bgColor);
            using (var br = new SolidBrush(bg))
                g.FillRectangle(br, rect);
            using (var pen = new Pen(selected ? Color.Yellow : Color.Gray, 2))
                g.DrawRectangle(pen, rect);
            SizeF sz = g.MeasureString(label, _btnFont);
            Brush txtBr = selected ? Brushes.Yellow : Brushes.White;
            g.DrawString(label, _btnFont, txtBr,
                rect.X + (rect.Width - sz.Width) / 2f,
                rect.Y + (rect.Height - sz.Height) / 2f);
        }
    }
}
