using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;
using System.IO;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Shown immediately at startup while the background MCI prewarm task
    /// finishes opening the first MP3 track. Once both the minimum display
    /// time has elapsed AND the audio system is ready, it replaces itself
    /// with TitleScene so the real music starts without any stutter.
    /// </summary>
    public sealed class LoadingScene : Scene
    {
        private const float MinDisplay = 1.5f;   // always show for at least this long
        private const float Timeout    = 6.0f;   // hard fallback — proceed regardless

        private float       _elapsed;
        private bool        _transitioned;
        private float       _dotTimer;
        private int         _dotCount;
        private Bitmap      _bg;
        private string      _tip;

        public override void OnEnter()
        {
            // Load a generic background — character art should not be used as scene backgrounds.
            string spritesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sprites");
            string[] candidates = new[]
            {
                Path.Combine(spritesDir, "bg_title.png"),
                Path.Combine(spritesDir, "deck.jpg"),
            };
            foreach (string c in candidates)
                if (File.Exists(c)) { _bg = new Bitmap(c); break; }

            // Team 2 (Producer) — Idea 10: rotating tip cycling on loading screen.
            _tip = TipCycler.NextTip();

            // Start lyrical theme music immediately so it plays throughout loading and title.
            // PlayIntroAmbient() was removed here — it produced an unwanted low buzzing SFX.
            Game.Instance.Audio.PlayTheme();
        }

        public override void OnExit()
        {
            _bg?.Dispose();
            _bg = null;
        }

        public override void Update(float dt)
        {
            if (_transitioned) return;

            _elapsed  += dt;
            _dotTimer += dt;
            if (_dotTimer >= 0.4f) { _dotCount = (_dotCount + 1) % 4; _dotTimer = 0f; }

            bool audioReady = Game.Instance.Audio.AudioSystemReady;
            if (_elapsed >= MinDisplay && (audioReady || _elapsed >= Timeout))
            {
                _transitioned = true;
                Game.Instance.Scenes.ReplaceAll(new TitleScene());
            }
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Miss Friday background if available, otherwise fallback gradient.
            if (_bg != null)
            {
                g.DrawImage(_bg, 0, 0, W, H);
                using (var dim = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                    g.FillRectangle(dim, 0, 0, W, H);
            }
            else
            {
                using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                    Color.FromArgb(5, 12, 40), Color.FromArgb(15, 35, 90), 90f))
                    g.FillRectangle(br, 0, 0, W, H);
            }

            // Title
            float tagY;
            using (var f = new Font("Courier New", 40, FontStyle.Bold))
            {
                const string title = "Miss Friday's Adventure Part II";
                SizeF sz = g.MeasureString(title, f);
                float titleY = H * 0.28f;
                g.DrawString(title, f, Brushes.White, (W - sz.Width) / 2f, titleY);
                tagY = titleY + sz.Height + 4f;
            }

            // Tagline
            using (var f = new Font("Courier New", 13, FontStyle.Bold))
            {
                const string tag = "Ice-Ice Fruit  \u2022  The Sea Serpent  \u2022  The Grand Line";
                SizeF sz = g.MeasureString(tag, f);
                g.DrawString(tag, f, Brushes.DarkSlateGray, (W - sz.Width) / 2f, tagY);
            }

            // Progress bar
            bool  audioReady = Game.Instance.Audio.AudioSystemReady;
            float progress   = Math.Min(1f, _elapsed / MinDisplay);
            if (!audioReady) progress = Math.Min(progress, 0.88f);

            const int BarW = 440;
            const int BarH = 10;
            int barX = (W - BarW) / 2;
            int barY = (int)(H * 0.65f);

            // Track
            using (var br = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                g.FillRectangle(br, barX, barY, BarW, BarH);

            // Fill
            if (progress > 0f)
            {
                int fillW = Math.Max(1, (int)(BarW * progress));
                using (var br = new LinearGradientBrush(
                    new Rectangle(barX, barY, fillW, BarH),
                    Color.FromArgb(80, 140, 255), Color.FromArgb(140, 200, 255), 0f))
                    g.FillRectangle(br, barX, barY, fillW, BarH);
            }

            // Border
            using (var pen = new Pen(Color.FromArgb(120, 255, 255, 255)))
                g.DrawRectangle(pen, barX, barY, BarW, BarH);

            // Status text with animated dots
            string dots   = new string('.', _dotCount);
            string status = audioReady ? "Ready!" : "Initializing audio" + dots;
            using (var f  = new Font("Courier New", 11))
            using (var br = new SolidBrush(Color.FromArgb(130, 200, 200, 200)))
            {
                SizeF sz = g.MeasureString(status, f);
                g.DrawString(status, f, br, (W - sz.Width) / 2f, barY + 18);
            }

            // Producer tip line.
            if (!string.IsNullOrEmpty(_tip))
            {
                using (var f = new Font("Courier New", 9, FontStyle.Bold))
                using (var br = new SolidBrush(Color.FromArgb(200, 220, 220, 180)))
                {
                    SizeF sz = g.MeasureString(_tip, f);
                    g.DrawString(_tip, f, br, (W - sz.Width) / 2f, H - 34);
                }
            }
        }
    }
}
