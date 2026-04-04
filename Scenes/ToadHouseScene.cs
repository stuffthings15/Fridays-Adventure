// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 – Multi-Team Implementation
// Scenes/ToadHouseScene.cs
// Purpose: SMB3-style Toad House bonus room. Player opens one of three chests
//          to receive a random power-up item placed in the reserve slot.
// ────────────────────────────────────────────────────────────────────────────
// Team 1  (Game Director)       – Idea 5:  Toad House entry/exit game flow
// Team 1  (Game Director)       – Idea 6:  Random item reward selection
// Team 1  (Game Director)       – Idea 7:  Item stored in PowerUpInventory.Reserve
// Team 9  (UI Programmer)       – Idea 11: Three-chest selection UI
// Team 9  (UI Programmer)       – Idea 12: Item reveal animation (label bounce)
// Team 9  (UI Programmer)       – Idea 13: Continue/exit button
// Team 14 (Environment Artist)  – Idea 9:  Mushroom house background
// Team 14 (Environment Artist)  – Idea 10: Toad NPC drawing
// Team 14 (Environment Artist)  – Idea 11: Chest visuals (closed / open)
// Team 17 (VFX Artist)          – Idea 7:  Chest open sparkle burst
// Team 17 (VFX Artist)          – Idea 8:  Item emerge glow pulse
// Team 17 (VFX Artist)          – Idea 9:  Celebration confetti on open
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// SMB3 Toad House bonus room.
    /// Player clicks one of three "?" chests to receive a random item.
    /// Item is placed into the PowerUpInventory reserve slot.
    ///
    /// Team 1 (Game Director) — Ideas 5–7.
    /// Team 9 (UI Programmer) — Ideas 11–13.
    /// </summary>
    public sealed class ToadHouseScene : Scene
    {
        // ── Chest data ────────────────────────────────────────────────────────
        private const int ChestCount = 3;
        private bool[] _opened       = new bool[ChestCount];
        private SuitType[] _prizes   = new SuitType[ChestCount];

        private int   _chosenChest   = -1;  // which chest the player opened
        private float _revealTimer;          // drives item emerge animation
        private const float RevealDuration = 1.2f;

        // ── Continue button ───────────────────────────────────────────────────
        private Rectangle _continueBtn;
        private bool       _canContinue;

        // ── Fonts ─────────────────────────────────────────────────────────────
        private static readonly Font _titleFont = new Font("Courier New", 20, FontStyle.Bold);
        private static readonly Font _bodyFont  = new Font("Courier New", 12, FontStyle.Bold);
        private static readonly Font _smFont    = new Font("Courier New", 10, FontStyle.Bold);

        // ── Timer ─────────────────────────────────────────────────────────────
        private float _anim;

        private static readonly Random _rng = new Random();

        // ── Prize pool (Team 1 — Idea 6) ─────────────────────────────────────
        private static readonly SuitType[] PrizePool =
        {
            SuitType.Mushroom,
            SuitType.Mushroom,   // higher weight for mushroom
            SuitType.FireFlower,
            SuitType.Leaf,
            SuitType.Star,
            SuitType.PWing,
        };

        public ToadHouseScene() { }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void OnEnter()
        {
            // Assign random (and distinct) prizes to chests (Team 1 — Idea 6)
            var pool = new System.Collections.Generic.List<SuitType>(PrizePool);
            for (int i = 0; i < ChestCount; i++)
            {
                int idx = _rng.Next(pool.Count);
                _prizes[i] = pool[idx];
                pool.RemoveAt(idx);
            }

            Game.Instance.Audio.ContinueOrPlay("toadhouse");
        }

        public override void OnExit() { }

        // ── Update ─────────────────────────────────────────────────────────────

        public override void Update(float dt)
        {
            _anim += dt;

            if (_chosenChest >= 0)
            {
                _revealTimer += dt;
                if (_revealTimer >= RevealDuration)
                    _canContinue = true;
            }

            // Keyboard: Enter to continue
            if (_canContinue && Game.Instance.Input.InteractPressed)
                Continue();
        }

        // ── Click handling ────────────────────────────────────────────────────

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
            if (_canContinue && _continueBtn.Contains(p)) { Continue(); return; }
            if (_chosenChest >= 0) return;   // already opened one

            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            for (int i = 0; i < ChestCount; i++)
            {
                var r = ChestRect(i, W, H);
                if (r.Contains(p))
                {
                    OpenChest(i);
                    return;
                }
            }
        }

        // ── Chest open logic (Team 1 — Idea 7 / Team 17 — Idea 7–9) ─────────

        private void OpenChest(int index)
        {
            _chosenChest         = index;
            _opened[index]       = true;
            SuitType prize       = _prizes[index];

            // Store in reserve slot (Team 1 — Idea 7)
            PowerUpInventory.SetReserve(prize);

            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            var r = ChestRect(index, W, H);

            // Sparkle + confetti VFX (Team 17 — Ideas 7, 9)
            ParticleSystem.SpawnCoinSparkle(r.X + r.Width / 2f, r.Y);
            ParticleSystem.SpawnConfetti(r.X + r.Width / 2f, r.Y, 30);
        }

        private void Continue()
        {
            Game.Instance.PendingToadHouse = false;
            Game.Instance.Scenes.Pop();
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            DrawBackground(g, W, H);
            DrawToad(g, W, H);
            DrawTitle(g, W, H);
            DrawChests(g, W, H);
            DrawReveal(g, W, H);
            if (_canContinue) DrawContinue(g, W, H);
            DrawDevMenuButton(g);
        }

        // ── Background (Team 14 — Idea 9) ─────────────────────────────────────

        private void DrawBackground(Graphics g, int W, int H)
        {
            // Mushroom house warm interior (Team 14)
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                Color.FromArgb(255, 230, 180), Color.FromArgb(220, 160, 80), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // Floor
            using (var br = new SolidBrush(Color.FromArgb(180, 120, 50)))
                g.FillRectangle(br, 0, H - 60, W, 60);

            // Floor planks
            using (var pen = new Pen(Color.FromArgb(140, 90, 30), 2))
                for (int fx = 0; fx < W; fx += 60)
                    g.DrawLine(pen, fx, H - 60, fx, H);

            // Decorative mushroom spots on walls
            float s = (float)Math.Sin(_anim * 0.8f) * 3f;
            using (var br = new SolidBrush(Color.FromArgb(80, 255, 100, 100)))
            {
                g.FillEllipse(br, 40, H / 2f - 30 + s, 40, 40);
                g.FillEllipse(br, W - 80, H / 2f - 30 - s, 40, 40);
            }
        }

        // ── Toad NPC (Team 14 — Idea 10) ──────────────────────────────────────

        private void DrawToad(Graphics g, int W, int H)
        {
            int tx = W / 2 - 20;
            int ty = H / 4;

            // Body (blue vest)
            using (var br = new SolidBrush(Color.FromArgb(80, 100, 200)))
                g.FillEllipse(br, tx, ty + 28, 40, 30);
            // Head
            using (var br = new SolidBrush(Color.FromArgb(240, 200, 160)))
                g.FillEllipse(br, tx + 4, ty, 32, 32);
            // Mushroom cap
            using (var br = new SolidBrush(Color.FromArgb(220, 50, 50)))
                g.FillEllipse(br, tx - 4, ty - 10, 48, 26);
            // Spots on cap
            using (var br = new SolidBrush(Color.White))
            {
                g.FillEllipse(br, tx + 4, ty - 6, 10, 10);
                g.FillEllipse(br, tx + 26, ty - 4, 10, 10);
            }
            // Eyes
            using (var br = new SolidBrush(Color.Black))
            {
                g.FillEllipse(br, tx + 8, ty + 10, 8, 8);
                g.FillEllipse(br, tx + 24, ty + 10, 8, 8);
            }
            // Smile
            using (var pen = new Pen(Color.Black, 2))
                g.DrawArc(pen, tx + 8, ty + 22, 24, 10, 0, 180);

            // Speech bubble
            string msg = _chosenChest < 0
                ? "Choose a chest!"
                : $"Enjoy the {SuitName(_prizes[_chosenChest])}!";
            using (var br = new SolidBrush(Color.FromArgb(230, Color.White)))
                g.FillRoundedRectangle(br, tx - 60, ty - 40, 180, 30, 8);
            g.DrawString(msg, _smFont, Brushes.Black, tx - 56, ty - 35);
        }

        // ── Chest visuals (Team 14 — Idea 11) ─────────────────────────────────

        private void DrawChests(Graphics g, int W, int H)
        {
            for (int i = 0; i < ChestCount; i++)
            {
                var r = ChestRect(i, W, H);
                DrawChest(g, r, _opened[i]);
            }
        }

        private void DrawChest(Graphics g, Rectangle r, bool open)
        {
            // Chest body
            Color bodyColor = open
                ? Color.FromArgb(160, 120, 40)
                : Color.FromArgb(120, 80, 20);

            using (var br = new SolidBrush(bodyColor))
                g.FillRectangle(br, r);

            if (!open)
            {
                // '?' on closed chest
                using (var f = new Font("Courier New", 22, FontStyle.Bold))
                {
                    var sz = g.MeasureString("?", f);
                    g.DrawString("?", f, Brushes.Gold,
                        r.X + (r.Width  - sz.Width)  / 2f,
                        r.Y + (r.Height - sz.Height) / 2f);
                }
                // Lock
                using (var br = new SolidBrush(Color.Gold))
                    g.FillEllipse(br, r.X + r.Width / 2 - 8, r.Y + r.Height - 20, 16, 16);
            }
            else
            {
                // Open lid
                using (var pen = new Pen(Color.FromArgb(100, 60, 10), 3))
                    g.DrawLine(pen, r.X, r.Y, r.X + r.Width / 3f, r.Y - 16);
                // Interior (dark)
                using (var br = new SolidBrush(Color.FromArgb(50, 30, 10)))
                    g.FillRectangle(br, r.X + 4, r.Y + 4, r.Width - 8, r.Height - 8);
            }

            // Border
            using (var pen = new Pen(Color.FromArgb(80, 50, 10), 3))
                g.DrawRectangle(pen, r);

            // Glow hint on hover (always subtle pulse — Team 17 Idea 8)
            if (!open)
            {
                float glow = (float)(Math.Sin(_anim * 3 + r.X * 0.01f) * 0.5 + 0.5);
                using (var br = new SolidBrush(Color.FromArgb((int)(40 * glow), 255, 220, 0)))
                    g.FillRectangle(br, r);
            }
        }

        // ── Item reveal label (Team 9 — Idea 12) ──────────────────────────────

        private void DrawReveal(Graphics g, int W, int H)
        {
            if (_chosenChest < 0) return;

            float t     = Math.Min(1f, _revealTimer / RevealDuration);
            float scale = EasingFunctions.EaseOutElastic(t);
            float alpha = Math.Min(255f, t * 400f);

            string label = $"Got {SuitName(_prizes[_chosenChest])}!";
            var r = ChestRect(_chosenChest, W, H);

            float ly = r.Y - 50 - (1f - t) * 30f;   // float upward
            float lx = r.X + r.Width / 2f;

            using (var f = new Font("Courier New", (int)(16 * scale + 1), FontStyle.Bold))
            using (var br = new SolidBrush(Color.FromArgb((int)alpha, Color.LimeGreen)))
            {
                var sz = g.MeasureString(label, f);
                g.DrawString(label, f, br, lx - sz.Width / 2f, ly);
            }
        }

        // ── Continue button (Team 9 — Idea 13) ────────────────────────────────

        private void DrawContinue(Graphics g, int W, int H)
        {
            int bw = 200, bh = 38;
            _continueBtn = new Rectangle((W - bw) / 2, H - 90, bw, bh);

            float pulse = (float)(Math.Sin(_anim * 4) * 0.5 + 0.5);
            Color btnColor = Color.FromArgb(60, 180, 60);

            using (var br = new SolidBrush(btnColor))
                g.FillRectangle(br, _continueBtn);
            using (var pen = new Pen(Color.FromArgb((int)(180 + 75 * pulse), Color.LimeGreen), 2))
                g.DrawRectangle(pen, _continueBtn);
            using (var br = new SolidBrush(Color.White))
            {
                var sz = g.MeasureString("CONTINUE →", _bodyFont);
                g.DrawString("CONTINUE →", _bodyFont, br,
                    _continueBtn.X + (_continueBtn.Width  - sz.Width)  / 2f,
                    _continueBtn.Y + (_continueBtn.Height - sz.Height) / 2f);
            }
        }

        // ── Title (Team 9 — Idea 11) ──────────────────────────────────────────

        private void DrawTitle(Graphics g, int W, int H)
        {
            const string title = "★  TOAD HOUSE  ★";
            var sz = g.MeasureString(title, _titleFont);
            g.DrawString(title, _titleFont, Brushes.DarkRed,
                (W - sz.Width) / 2f, 12);

            const string sub = "Open a chest to receive a prize!";
            if (_chosenChest < 0)
            {
                var ss = g.MeasureString(sub, _smFont);
                g.DrawString(sub, _smFont, Brushes.DimGray,
                    (W - ss.Width) / 2f, 48);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Rectangle ChestRect(int index, int W, int H)
        {
            int cw = 80, ch = 70;
            int spacing = 40;
            int totalW  = ChestCount * cw + (ChestCount - 1) * spacing;
            int startX  = (W - totalW) / 2;
            int cx = startX + index * (cw + spacing);
            int cy = H / 2 - ch / 2 + 30;
            return new Rectangle(cx, cy, cw, ch);
        }

        private static string SuitName(SuitType suit)
        {
            switch (suit)
            {
                case SuitType.Mushroom:   return "Super Mushroom";
                case SuitType.FireFlower: return "Fire Flower";
                case SuitType.Leaf:       return "Super Leaf";
                case SuitType.Star:       return "Star";
                case SuitType.PWing:      return "P-Wing";
                default:                  return suit.ToString();
            }
        }
    }

    // ── Helper extension for rounded rectangle ────────────────────────────────
    internal static class GfxExt
    {
        public static void FillRoundedRectangle(this Graphics g, Brush br,
            float x, float y, float w, float h, float r)
        {
            var path = new GraphicsPath();
            path.AddArc(x, y, r * 2, r * 2, 180, 90);
            path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
            path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            g.FillPath(br, path);
        }
    }
}
