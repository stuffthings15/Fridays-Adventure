using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    public sealed class CreditsScene : Scene
    {
        private float _timer;
        private bool  _saved;
        private readonly int _finalScore;

        private static readonly Font _titleFont   = new Font("Courier New", 28, FontStyle.Bold);
        private static readonly Font _creditFont  = new Font("Courier New", 18, FontStyle.Bold);
        private static readonly Font _scoreFont   = new Font("Courier New", 16, FontStyle.Bold);
        private static readonly Font _promptFont  = new Font("Courier New", 11, FontStyle.Bold);
        private static readonly Font _smallFont   = new Font("Courier New", 12);

        public CreditsScene()
        {
            _finalScore = Game.Instance.PlayerBounty;
        }

        public override void OnEnter()
        {
            Game.Instance.Audio.StopMusic();
            Game.Instance.Audio.PlayVictoryFanfare();

            if (_finalScore > 0)
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
            if (_timer > 2f &&
                (input.InteractPressed || input.AttackPressed || input.JumpPressed))
                Game.Instance.Scenes.Replace(new TitleScene());
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
            if (_timer > 2f)
                Game.Instance.Scenes.Replace(new TitleScene());
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Background
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                Color.FromArgb(5, 8, 30), Color.FromArgb(20, 40, 100), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // Stars
            var rng = new Random(42);
            for (int i = 0; i < 60; i++)
            {
                int sx = rng.Next(W), sy = rng.Next(H);
                int sz = rng.Next(1, 3);
                g.FillRectangle(Brushes.White, sx, sy, sz, sz);
            }

            float y = H * 0.08f;

            // Title
            const string title = "CONGRATULATIONS!";
            SizeF tsz = g.MeasureString(title, _titleFont);
            g.DrawString(title, _titleFont, Brushes.Gold, (W - tsz.Width) / 2f, y);
            y += tsz.Height + 16;

            // Subtitle
            const string sub = "You have conquered the Grand Line!";
            SizeF ssz = g.MeasureString(sub, _smallFont);
            g.DrawString(sub, _smallFont, Brushes.LightCyan, (W - ssz.Width) / 2f, y);
            y += ssz.Height + 30;

            // Final score box
            using (var br = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                g.FillRectangle(br, W / 2 - 200, (int)y, 400, 80);
            g.DrawRectangle(Pens.Gold, W / 2 - 200, (int)y, 400, 80);

            string scoreLine = $"FINAL SCORE: {_finalScore:N0}";
            SizeF scsz = g.MeasureString(scoreLine, _scoreFont);
            g.DrawString(scoreLine, _scoreFont, Brushes.Yellow, (W - scsz.Width) / 2f, y + 12);

            string rankLine = BountySystem.Title();
            SizeF rsz = g.MeasureString(rankLine, _smallFont);
            g.DrawString(rankLine, _smallFont, Brushes.Gold, (W - rsz.Width) / 2f, y + 46);
            y += 110;

            if (_saved)
            {
                string savedMsg = $"Score saved for {Game.Instance.PlayerName}!";
                SizeF smsz = g.MeasureString(savedMsg, _smallFont);
                g.DrawString(savedMsg, _smallFont, Brushes.Cyan, (W - smsz.Width) / 2f, y);
                y += smsz.Height + 20;
            }

            // Credits
            y += 10;
            using (var br = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
                g.FillRectangle(br, W / 2 - 180, (int)y, 360, 110);

            const string creditsTitle = "CREDITS";
            SizeF ctsz = g.MeasureString(creditsTitle, _creditFont);
            g.DrawString(creditsTitle, _creditFont, Brushes.White, (W - ctsz.Width) / 2f, y + 8);

            const string author = "By Curtis Loop";
            SizeF asz = g.MeasureString(author, _creditFont);
            g.DrawString(author, _creditFont, Brushes.Gold, (W - asz.Width) / 2f, y + 50);
            y += 140;

            // Thank you
            const string thanks = "Thank you for playing Miss Friday's Adventure Part II!";
            SizeF thsz = g.MeasureString(thanks, _smallFont);
            g.DrawString(thanks, _smallFont, Brushes.LightGray, (W - thsz.Width) / 2f, y);

            // Continue prompt
            if (_timer > 2f && (int)(_timer / 0.55f) % 2 == 0)
            {
                const string prompt = "Press  ENTER  or  Z  to continue";
                SizeF psz = g.MeasureString(prompt, _promptFont);
                g.DrawString(prompt, _promptFont, Brushes.Yellow, (W - psz.Width) / 2f, H - 50);
            }
            DrawDevMenuButton(g);
        }
    }
}
