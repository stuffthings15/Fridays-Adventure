using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  CardMiniGameScene.cs  —  UI Programmer + Lead Game Designer:
    //                           N-Spade / Card match mini-game
    //
    //  Idea (Team 4 Designer):  Card flip mini-game triggered after 80+ coins.
    //  Idea (Team 9 UI Prog):   Card matching UI with flip animation and rewards.
    //
    //  Rules (SMB3 N-Spade):
    //    — 8 face-down cards in a 2×4 grid.
    //    — Flip one card at a time; cards reveal Coin, Super Mushroom, Star, or Flower.
    //    — Matching three of a kind awards the prize (extra life for Star, etc.).
    //    — After all flips, scene pops back to overworld.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SMB3-style N-Spade card flip mini-game.
    /// Team 9 (UI Programmer) — UI/animation/reward logic.
    /// Team 4 (Lead Game Designer) — card prize table and game rules.
    /// </summary>
    public sealed class CardMiniGameScene : Scene
    {
        // ── Card data ─────────────────────────────────────────────────────────
        private enum CardFace { Coin, Mushroom, Star, Flower }

        private sealed class Card
        {
            public CardFace Face;
            public bool     FaceUp;
            public float    FlipProgress;  // 0=face-down, 1=face-up
            public bool     Matched;
            public Rectangle Rect;
        }

        private readonly Card[] _cards = new Card[8];
        private int   _flippedIndex = -1;   // currently face-up unmatched card
        private bool  _awarding;
        private float _awardTimer;
        private int   _moveCount;
        private string _awardMessage = "";

        // ── Matched tallies ───────────────────────────────────────────────────
        private readonly Dictionary<CardFace, int> _matched =
            new Dictionary<CardFace, int>
            { [CardFace.Coin]=0, [CardFace.Mushroom]=0, [CardFace.Star]=0, [CardFace.Flower]=0 };

        // ── Font ─────────────────────────────────────────────────────────────
        private Font _titleFont;
        private Font _cardFont;
        private Font _msgFont;

        // ── Button ────────────────────────────────────────────────────────────
        private Rectangle _doneBtn;

        public override void OnEnter()
        {
            _titleFont = new Font("Courier New", 18, FontStyle.Bold);
            _cardFont  = new Font("Courier New", 28, FontStyle.Bold);
            _msgFont   = new Font("Courier New", 13, FontStyle.Bold);

            BuildCards();
            Game.Instance.Audio.ContinueOrPlay("event");
            DebugLogger.LogInfo("CardMiniGame", "N-Spade mini-game started.");
        }

        public override void OnExit()
        {
            _titleFont?.Dispose();
            _cardFont?.Dispose();
            _msgFont?.Dispose();
        }

        private void BuildCards()
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            int cw = 90, ch = 120, gx = 20, gy = 30;
            int totalW = 4 * cw + 3 * gx;
            int startX = (W - totalW) / 2;
            int startY = H / 2 - ch - gy / 2;

            // Two rows of four.
            CardFace[] faces = { CardFace.Coin, CardFace.Mushroom, CardFace.Star, CardFace.Flower,
                                  CardFace.Coin, CardFace.Mushroom, CardFace.Star, CardFace.Flower };

            // Fisher-Yates shuffle.
            var rng = new Random();
            for (int i = faces.Length - 1; i > 0; i--)
            { int j = rng.Next(i + 1); var t = faces[i]; faces[i] = faces[j]; faces[j] = t; }

            for (int i = 0; i < 8; i++)
            {
                int col = i % 4, row = i / 4;
                _cards[i] = new Card
                {
                    Face = faces[i],
                    Rect = new Rectangle(startX + col * (cw + gx), startY + row * (ch + gy), cw, ch)
                };
            }

            int W2 = Game.Instance.CanvasWidth;
            _doneBtn = new Rectangle(W2 / 2 - 70, H - 60, 140, 36);
        }

        public override void Update(float dt)
        {
            // Animate card flips.
            foreach (var c in _cards)
            {
                float target = c.FaceUp ? 1f : 0f;
                c.FlipProgress = EasingFunctions.Lerp(c.FlipProgress, target, 8f * dt);
            }

            if (_awarding)
            {
                _awardTimer -= dt;
                if (_awardTimer <= 0f) { _awarding = false; CheckGameEnd(); }
            }
        }

        public override void HandleClick(System.Drawing.Point p)
        {
            // Done button.
            if (_doneBtn.Contains(p)) { FinishGame(); return; }

            // Card click.
            for (int i = 0; i < _cards.Length; i++)
            {
                var c = _cards[i];
                if (c.Matched || c.FaceUp || !c.Rect.Contains(p)) continue;

                c.FaceUp = true;
                _moveCount++;

                if (_flippedIndex < 0)
                {
                    // First flip in this turn.
                    _flippedIndex = i;
                }
                else
                {
                    // Second flip — check match.
                    var first  = _cards[_flippedIndex];
                    var second = c;

                    if (first.Face == second.Face)
                    {
                        // Match!
                        first.Matched  = true;
                        second.Matched = true;
                        _matched[first.Face]++;
                        AwardPrize(first.Face);
                    }
                    else
                    {
                        // No match — flip back after delay.
                        int fi = _flippedIndex, si = i;
                        System.Threading.Tasks.Task.Delay(700).ContinueWith(_ =>
                        {
                            _cards[fi].FaceUp = false;
                            _cards[si].FaceUp = false;
                        });
                    }
                    _flippedIndex = -1;
                }
                break;
            }
        }

        private void AwardPrize(CardFace face)
        {
            _awarding   = true;
            _awardTimer = 2.5f;
            switch (face)
            {
                case CardFace.Coin:
                    Game.Instance.AddCoins(10);
                    _awardMessage = "+10 COINS!";
                    break;
                case CardFace.Mushroom:
                    PowerUpInventory.SetReserveItem(SuitType.Mushroom);
                    _awardMessage = "SUPER MUSHROOM!";
                    break;
                case CardFace.Star:
                    Game.Instance.CurrentLives++;
                    _awardMessage = "1UP! (STAR)";
                    break;
                case CardFace.Flower:
                    PowerUpInventory.SetReserveItem(SuitType.FireFlower);
                    _awardMessage = "FIRE FLOWER!";
                    break;
            }
            Game.Instance.FloatingText.Spawn(
                _awardMessage, Game.Instance.CanvasWidth / 2,
                Game.Instance.CanvasHeight / 2 - 40,
                Color.Gold, large: true);
            DebugLogger.LogInfo("CardMiniGame", $"Prize awarded: {_awardMessage}");
        }

        private void CheckGameEnd()
        {
            // If all cards are matched, finish automatically.
            bool allDone = true;
            foreach (var c in _cards) if (!c.Matched) { allDone = false; break; }
            if (allDone) FinishGame();
        }

        private void FinishGame()
        {
            DebugLogger.LogInfo("CardMiniGame", $"Finished in {_moveCount} moves.");
            Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // ── Background ────────────────────────────────────────────────────
            using (var br = new SolidBrush(Color.FromArgb(18, 6, 40)))
                g.FillRectangle(br, 0, 0, W, H);
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                Color.FromArgb(40, 20, 80), Color.FromArgb(10, 5, 30), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // ── Title ─────────────────────────────────────────────────────────
            g.DrawString("N-SPADE  CARD GAME", _titleFont, Brushes.Gold, W / 2f - 160, 20);
            using (var f = new Font("Courier New", 10))
                g.DrawString($"Moves: {_moveCount}   Match pairs to win prizes!",
                    f, Brushes.Gray, W / 2f - 140, 60);

            // ── Cards ─────────────────────────────────────────────────────────
            foreach (var c in _cards)
                DrawCard(g, c);

            // ── Award message ─────────────────────────────────────────────────
            if (_awarding && !string.IsNullOrEmpty(_awardMessage))
            {
                using (var br = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                    g.FillRectangle(br, W / 2 - 160, H / 2 - 60, 320, 56);
                g.DrawString(_awardMessage, _msgFont, Brushes.Gold, W / 2f - 120, H / 2f - 50);
            }

            // ── Done button ───────────────────────────────────────────────────
            SMB3UIArtLibrary.DrawButton(g, _doneBtn, "LEAVE GAME", Color.SlateBlue);
        }

        private void DrawCard(Graphics g, Card c)
        {
            Rectangle r = c.Rect;
            float fp    = c.FlipProgress;

            if (c.Matched)
            {
                // Dimmed matched card.
                using (var br = new SolidBrush(Color.FromArgb(50, 30, 80)))
                    g.FillRectangle(br, r);
                return;
            }

            // Squish to simulate flip (scale width by |cos(progress*π)|)
            float cosW  = (float)Math.Abs(Math.Cos(fp * Math.PI));
            int drawW   = Math.Max(4, (int)(r.Width * cosW));
            int drawX   = r.X + (r.Width - drawW) / 2;

            bool showFace = fp > 0.5f;

            // Card body.
            Color bodyColor = showFace ? FaceColor(c.Face) : Color.FromArgb(40, 60, 200);
            using (var br = new SolidBrush(bodyColor))
                g.FillRectangle(br, drawX, r.Y, drawW, r.Height);
            using (var pen = new Pen(Color.FromArgb(200, 220, 220, 220), 2))
                g.DrawRectangle(pen, drawX, r.Y, drawW, r.Height);

            if (showFace && drawW > 20)
            {
                string icon = FaceIcon(c.Face);
                g.DrawString(icon, _cardFont, Brushes.White,
                    drawX + (drawW - 30) / 2f, r.Y + r.Height / 2f - 20);
            }
            else if (!showFace && drawW > 20)
            {
                // Card back — SMB3-style diamond pattern.
                using (var pen = new Pen(Color.FromArgb(80, 200, 200, 255)))
                    g.DrawLine(pen, drawX, r.Y, drawX + drawW, r.Y + r.Height);
            }
        }

        private static Color FaceColor(CardFace f)
        {
            switch (f)
            {
                case CardFace.Coin:     return Color.FromArgb(180, 140, 10);
                case CardFace.Mushroom: return Color.FromArgb(160, 40, 40);
                case CardFace.Star:     return Color.FromArgb(60, 60, 160);
                case CardFace.Flower:   return Color.FromArgb(50, 120, 50);
                default:                return Color.DimGray;
            }
        }

        private static string FaceIcon(CardFace f)
        {
            switch (f)
            {
                case CardFace.Coin:     return "●";
                case CardFace.Mushroom: return "♦";
                case CardFace.Star:     return "★";
                case CardFace.Flower:   return "✿";
                default:                return "?";
            }
        }
    }

    // ── SMB3UIArtLibrary — reusable UI drawing helpers (Team 9 + Team 15) ────
    /// <summary>
    /// Reusable SMB3-style UI panel, button, and icon drawing helpers.
    /// Used by multiple scenes for consistent visual language.
    /// Team 9 (UI Programmer) + Team 15 (UI/UX Artist) — new art library.
    /// </summary>
    public static class SMB3UIArtLibrary
    {
        // ── Panel ─────────────────────────────────────────────────────────────
        /// <summary>Draws a dark rounded-corner SMB3 panel with a colored border.</summary>
        public static void DrawPanel(Graphics g, Rectangle r, Color borderColor, int alpha = 200)
        {
            using (var br = new SolidBrush(Color.FromArgb(alpha, 8, 8, 20)))
                g.FillRectangle(br, r);
            using (var pen = new Pen(Color.FromArgb(180, borderColor), 2))
                g.DrawRectangle(pen, r);
        }

        // ── Button ────────────────────────────────────────────────────────────
        /// <summary>Draws a labeled button with SMB3 styling.</summary>
        public static void DrawButton(Graphics g, Rectangle r, string label,
            Color accent, bool hovered = false)
        {
            Color bg = hovered
                ? Color.FromArgb(140, accent.R, accent.G, accent.B)
                : Color.FromArgb(60,  accent.R, accent.G, accent.B);

            using (var br = new SolidBrush(bg))
                g.FillRectangle(br, r);
            using (var pen = new Pen(Color.FromArgb(200, accent), 2))
                g.DrawRectangle(pen, r);

            using (var f  = new Font("Courier New", 10, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(label, f);
                g.DrawString(label, f, Brushes.White,
                    r.X + (r.Width  - sz.Width)  / 2f,
                    r.Y + (r.Height - sz.Height) / 2f);
            }
        }

        // ── Star rating ───────────────────────────────────────────────────────
        /// <summary>Draws 0–3 gold/empty stars for level ratings.</summary>
        public static void DrawStarRating(Graphics g, int x, int y, int stars)
        {
            using (var f = new Font("Courier New", 14, FontStyle.Bold))
            {
                for (int i = 0; i < 3; i++)
                {
                    Brush br = i < stars ? Brushes.Gold : Brushes.DimGray;
                    g.DrawString("★", f, br, x + i * 22, y);
                }
            }
        }

        // ── Coin icon ─────────────────────────────────────────────────────────
        /// <summary>Draws a spinning coin icon (static version with frame index).</summary>
        public static void DrawCoinIcon(Graphics g, int x, int y, int frame)
        {
            // Widths for the 4-frame spin animation.
            int[] widths = { 14, 10, 4, 10 };
            int w  = widths[frame % 4];
            int dx = x + (14 - w) / 2;
            using (var br = new SolidBrush(Color.Gold))
                g.FillEllipse(br, dx, y, w, 14);
            using (var pen = new Pen(Color.Goldenrod, 1))
                g.DrawEllipse(pen, dx, y, w, 14);
        }

        // ── Map node tooltip ──────────────────────────────────────────────────
        /// <summary>Draws a small tooltip bubble at a map node position.</summary>
        public static void DrawNodeTooltip(Graphics g, int nx, int ny, string name, string status)
        {
            using (var f    = new Font("Courier New", 8, FontStyle.Bold))
            using (var fSub = new Font("Courier New", 7))
            {
                SizeF sz  = g.MeasureString(name, f);
                int  tw   = (int)sz.Width + 14;
                int  th   = 36;
                int  tx   = nx - tw / 2;
                int  ty   = ny - th - 14;

                using (var br = new SolidBrush(Color.FromArgb(200, 10, 10, 30)))
                    g.FillRectangle(br, tx, ty, tw, th);
                using (var pen = new Pen(Color.FromArgb(160, Color.CornflowerBlue), 1))
                    g.DrawRectangle(pen, tx, ty, tw, th);

                g.DrawString(name,   f,    Brushes.White, tx + 6, ty + 4);
                g.DrawString(status, fSub, Brushes.Gray,  tx + 6, ty + 20);

                // Pointer triangle.
                using (var br = new SolidBrush(Color.FromArgb(200, 10, 10, 30)))
                    g.FillRectangle(br, nx - 4, ty + th, 8, 4);
            }
        }
    }
}
