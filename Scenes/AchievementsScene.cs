using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Achievements Scene — SMB3-style badge grid showing all achievements.
    ///
    /// Team 1  (Game Director)  — achievement roster display, unlocked/locked states.
    /// Team 2  (Producer)       — completion percentage header.
    /// Team 15 (UI/UX Artist)   — badge tile layout, gold/silver locked style.
    ///
    /// ── Access ───────────────────────────────────────────────────────────────
    /// From PauseScene or TitleScene:
    ///   Game.Instance.Scenes.Push(new AchievementsScene());
    /// </summary>
    public sealed class AchievementsScene : Scene
    {
        // ── Fonts ─────────────────────────────────────────────────────────────
        private Font _titleFont;
        private Font _nameFont;
        private Font _descFont;
        private Font _iconFont;

        // ── Layout ────────────────────────────────────────────────────────────
        private const int TileW   = 170;
        private const int TileH   = 72;
        private const int TileGap = 10;
        private const int Cols    = 4;
        private const int TopY    = 70;

        // ── Scroll ────────────────────────────────────────────────────────────
        private int _scrollY;
        private int _totalHeight;

        // ── Button ────────────────────────────────────────────────────────────
        private Rectangle _btnBack;

        // ── OnEnter ───────────────────────────────────────────────────────────
        public override void OnEnter()
        {
            _titleFont = new Font("Courier New", 18, FontStyle.Bold);
            _nameFont  = new Font("Courier New", 9,  FontStyle.Bold);
            _descFont  = new Font("Courier New", 8);
            _iconFont  = new Font("Courier New", 14, FontStyle.Bold);

            int rows  = (AchievementSystem.All.Count + Cols - 1) / Cols;
            _totalHeight = rows * (TileH + TileGap) + TopY + 60;
        }

        public override void OnExit()
        {
            _titleFont?.Dispose();
            _nameFont?.Dispose();
            _descFont?.Dispose();
            _iconFont?.Dispose();
        }

        // ── Update ────────────────────────────────────────────────────────────
        public override void Update(float dt) { }

        // ── Draw ──────────────────────────────────────────────────────────────
        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // ── Background (SMB3 dark navy) ───────────────────────────────────
            using (var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, W, H),
                Color.FromArgb(8, 8, 30), Color.FromArgb(18, 18, 55), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // Decorative star field.
            var rng = new Random(77);
            for (int i = 0; i < 60; i++)
            {
                int sx = (int)(rng.NextDouble() * W);
                int sy = (int)(rng.NextDouble() * H);
                using (var br = new SolidBrush(Color.FromArgb(80, Color.White)))
                    g.FillEllipse(br, sx, sy, 2, 2);
            }

            // ── Header ────────────────────────────────────────────────────────
            using (var br = new SolidBrush(Color.FromArgb(50, 20, 80)))
                g.FillRectangle(br, 0, 0, W, TopY);
            using (var pen = new Pen(Color.FromArgb(100, Color.Gold), 2))
                g.DrawLine(pen, 0, TopY - 1, W, TopY - 1);

            g.DrawString("ACHIEVEMENTS", _titleFont, Brushes.Gold, 12, 10);

            // Completion counter.
            int earned = AchievementSystem.EarnedCount();
            int total  = AchievementSystem.All.Count;
            float pct  = total > 0 ? (float)earned / total * 100f : 0f;

            g.DrawString($"{earned}/{total}  ({pct:F0}%)", _nameFont, Brushes.White, W - 180, 20);

            // Completion bar.
            g.FillRectangle(Brushes.DarkSlateBlue, W - 180, 40, 160, 8);
            using (var br = new SolidBrush(Color.Gold))
                g.FillRectangle(br, W - 180, 40, (int)(160 * pct / 100f), 8);

            // ── Badge grid ────────────────────────────────────────────────────
            int startX = (W - (Cols * (TileW + TileGap) - TileGap)) / 2;

            g.SetClip(new Rectangle(0, TopY, W, H - TopY - 50));

            for (int i = 0; i < AchievementSystem.All.Count; i++)
            {
                var ach = AchievementSystem.All[i];
                bool unlocked = AchievementSystem.IsEarned(ach.Id);

                int col = i % Cols;
                int row = i / Cols;
                int tx  = startX + col * (TileW + TileGap);
                int ty  = TopY + row * (TileH + TileGap) - _scrollY;

                if (ty + TileH < TopY || ty > H - 50) continue;

                DrawBadgeTile(g, tx, ty, ach, unlocked);
            }

            g.ResetClip();

            // ── Back button ───────────────────────────────────────────────────
            _btnBack = new Rectangle(8, H - 44, 100, 34);
            DrawButton(g, _btnBack, "◀ BACK", Color.FromArgb(40, 20, 70));

            DrawDevMenuButton(g);
        }

        // ── HandleClick ───────────────────────────────────────────────────────
        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
            if (_btnBack.Contains(p)) { Game.Instance.Scenes.Pop(); return; }
        }

        // ── HandleMouseWheel ─────────────────────────────────────────────────
        public override void HandleMouseWheel(int delta)
        {
            int maxScroll = Math.Max(0, _totalHeight - Game.Instance.CanvasHeight);
            _scrollY = Math.Max(0, Math.Min(maxScroll, _scrollY + (delta > 0 ? -30 : 30)));
        }

        // ── Badge tile ────────────────────────────────────────────────────────
        private void DrawBadgeTile(Graphics g, int tx, int ty, AchievementSystem.Achievement ach, bool unlocked)
        {
            // Tile background.
            Color bgColor = unlocked
                ? Color.FromArgb(40, 30, 10)
                : Color.FromArgb(20, 20, 30);
            using (var br = new SolidBrush(bgColor))
                g.FillRectangle(br, tx, ty, TileW, TileH);

            // Border — gold if unlocked, dark gray if locked.
            Color borderColor = unlocked ? ach.Color : Color.FromArgb(60, 60, 70);
            using (var pen = new Pen(borderColor, unlocked ? 2 : 1))
                g.DrawRectangle(pen, tx, ty, TileW, TileH);

            if (unlocked)
            {
                // Gold shimmer on top-left corner.
                using (var br = new SolidBrush(Color.FromArgb(30, ach.Color)))
                    g.FillRectangle(br, tx, ty, TileW, 4);
            }

            // ── Icon circle ───────────────────────────────────────────────────
            Color iconBg = unlocked ? Color.FromArgb(60, ach.Color) : Color.FromArgb(30, 30, 40);
            using (var br = new SolidBrush(iconBg))
                g.FillEllipse(br, tx + 6, ty + 14, 36, 36);

            using (var iconBrush = new SolidBrush(unlocked ? ach.Color : Color.Gray))
                g.DrawString(ach.Icon, _iconFont, iconBrush, tx + 9, ty + 18);

            // ── Name ─────────────────────────────────────────────────────────
            using (var br = new SolidBrush(unlocked ? Color.White : Color.Gray))
                g.DrawString(ach.Name, _nameFont, br, tx + 50, ty + 10);

            // ── Description ──────────────────────────────────────────────────
            string desc = unlocked ? ach.Description : "???";
            using (var br = new SolidBrush(unlocked ? Color.LightGray : Color.FromArgb(80, 80, 80)))
            {
                // Word-wrap via manual line break at 118 chars.
                string d = desc.Length > 50 ? desc.Substring(0, 50) + "\n" + desc.Substring(50) : desc;
                g.DrawString(d, _descFont, br, tx + 50, ty + 28);
            }

            // ── Unlocked star ─────────────────────────────────────────────────
            if (unlocked)
                using (var br = new SolidBrush(ach.Color))
                    g.DrawString("★", _nameFont, br, tx + TileW - 20, ty + 6);
        }

        private static void DrawButton(Graphics g, Rectangle r, string text, Color bg)
        {
            using (var br = new SolidBrush(bg))
                g.FillRectangle(br, r);
            using (var pen = new Pen(Color.FromArgb(160, Color.Gold), 1))
                g.DrawRectangle(pen, r);
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
            {
                var sz = g.MeasureString(text, f);
                g.DrawString(text, f, Brushes.White,
                    r.X + (r.Width  - sz.Width)  / 2f,
                    r.Y + (r.Height - sz.Height) / 2f);
            }
        }
    }
}
