using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Scenes
{
    public sealed class HighScoreScene : Scene
    {
        private readonly int _finalScore;
        private readonly int _berriesCollected;
        private readonly bool _isNewEntry;

        private bool  _saved;
        private float _timer;

        private static readonly Font _titleFont = new Font("Courier New", 22, FontStyle.Bold);
        private static readonly Font _smallFont = new Font("Courier New", 11, FontStyle.Bold);

        public HighScoreScene(int finalScore, int berriesCollected, bool isNewEntry = true)
        {
            _finalScore       = finalScore;
            _berriesCollected = berriesCollected;
            _isNewEntry       = isNewEntry;
        }

        public override void OnEnter()
        {
            if (_isNewEntry && _finalScore > 0)
            {
                string name = Game.Instance.PlayerName;
                if (string.IsNullOrEmpty(name)) name = "???";
                Game.Instance.Save.AddHighScore(name, _finalScore);
                Game.Instance.Save.Save();
                _saved = true;
            }
        }

        public override void OnExit() { }

        public override void Update(float dt)
        {
            _timer += dt;

            var input = Game.Instance.Input;
            if (_timer > 0.5f &&
                (input.InteractPressed || input.AttackPressed || input.JumpPressed))
                Game.Instance.Scenes.ReplaceAll(new TitleScene());
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
            if (_timer > 0.5f)
                Game.Instance.Scenes.ReplaceAll(new TitleScene());
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                Color.FromArgb(10, 10, 40), Color.FromArgb(30, 30, 80), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // Title
            const string title = "HIGH  SCORES";
            SizeF tsz = g.MeasureString(title, _titleFont);
            g.DrawString(title, _titleFont, Brushes.Gold, (W - tsz.Width) / 2f, 30);

            // Score summary (only when coming from a game)
            if (_isNewEntry)
            {
                string summary = $"Final Score: {_finalScore:N0}     Berries: {_berriesCollected}";
                SizeF ssz = g.MeasureString(summary, _smallFont);
                g.DrawString(summary, _smallFont, Brushes.White, (W - ssz.Width) / 2f, 70);

                if (_saved)
                {
                    string savedMsg = $"Score saved for {Game.Instance.PlayerName}!";
                    SizeF smsz = g.MeasureString(savedMsg, _smallFont);
                    g.DrawString(savedMsg, _smallFont, Brushes.Cyan, (W - smsz.Width) / 2f, 94);
                }
            }

            // High score table
            int tableY = _isNewEntry ? 130 : 80;
            var scores = Game.Instance.Save.GetTopScores(10);

            using (var br = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                g.FillRectangle(br, W / 2 - 220, tableY, 440, 30 + Math.Max(1, scores.Count) * 32);

            g.DrawString("RANK", _smallFont, Brushes.Yellow, W / 2 - 200, tableY + 4);
            g.DrawString("NAME", _smallFont, Brushes.Yellow, W / 2 - 100, tableY + 4);
            g.DrawString("SCORE", _smallFont, Brushes.Yellow, W / 2 + 100, tableY + 4);

            for (int i = 0; i < scores.Count; i++)
            {
                int rowY = tableY + 30 + i * 32;
                bool highlight = _saved && scores[i].Score == _finalScore &&
                                 string.Equals(scores[i].Name, Game.Instance.PlayerName, StringComparison.OrdinalIgnoreCase);
                Brush textBr = highlight ? Brushes.Cyan : Brushes.White;

                g.DrawString($"#{i + 1}", _smallFont, Brushes.Gold, W / 2 - 200, rowY);
                g.DrawString(scores[i].Name, _smallFont, textBr, W / 2 - 100, rowY);
                g.DrawString($"{scores[i].Score:N0}", _smallFont, textBr, W / 2 + 100, rowY);
            }

            if (scores.Count == 0)
                g.DrawString("No scores yet!", _smallFont, Brushes.DimGray, W / 2 - 50, tableY + 34);

            // Continue prompt
            if (_timer > 0.5f && (int)(_timer / 0.55f) % 2 == 0)
            {
                const string prompt = "Press  ENTER  or  Z  to continue";
                SizeF psz = g.MeasureString(prompt, _smallFont);
                g.DrawString(prompt, _smallFont, Brushes.Yellow, (W - psz.Width) / 2f, H - 50);
            }
            DrawDevMenuButton(g);
        }
    }
}
