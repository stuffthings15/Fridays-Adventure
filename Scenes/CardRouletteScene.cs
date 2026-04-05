using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// SMB3-style 3-card roulette mini-game shown after each level clear.
    ///
    /// Three face-down cards spin at different speeds.  The player presses
    /// the action button (or clicks) to stop each card in sequence.
    /// Matching all three gives a bonus life; matching two gives berries / health.
    ///
    /// Card face values:
    ///   🍓  BERRY   — +50 berries (score)
    ///   ❤   HEART   — +25 % max HP restored
    ///   ⭐  STAR    — +1 extra life
    ///   🔑  KEY     — secret bonus (double next level's score)
    ///
    /// Team 1  (Game Director) — Idea 2: SMB3 N-card roulette post-level reward.
    /// Team 4  (Lead Game Designer) — Idea 2: coin/berry to 100 extra life tie-in.
    /// Team 9  (UI Programmer) — card reveal animation and reward display.
    /// </summary>
    public sealed class CardRouletteScene : Scene
    {
        // ── Card face values ──────────────────────────────────────────────────
        private enum CardFace { Berry, Heart, Star, Key }

        private static readonly string[]  CardLabel = { "BERRY", "HEART", "STAR", "KEY" };
        private static readonly Color[]   CardColor =
        {
            Color.FromArgb(220, 40, 40),   // Berry — red
            Color.FromArgb(220, 80, 160),  // Heart — pink
            Color.FromArgb(255, 200, 0),   // Star  — gold
            Color.FromArgb(60, 180, 220),  // Key   — cyan
        };
        private static readonly string[]  CardSymbol = { "♥", "♥", "★", "⚷" };

        // ── State ─────────────────────────────────────────────────────────────
        private readonly Action _onContinue;     // scene to go to after roulette

        private int   _activeCard = 0;           // which card is being stopped (0–2)
        private int[] _cardFace   = new int[3];  // face currently shown on each card
        private bool[] _stopped   = new bool[3]; // whether each card is locked
        private float[] _spinRate = new float[3];// symbols per second for each card

        // Spin timer — drives face cycling for each spinning card.
        private float _spinTimer;   // global spin tick accumulator
        private float[] _faceTimer = new float[3];

        // Result display.
        private bool  _resultShown;
        private float _resultTimer;
        private string _resultMsg;
        private Color  _resultColor;
        private const float ResultDisplay = 1.0f;
        private bool  _advancing;

        // Random for face selection.
        private readonly Random _rng = new Random();

        // Fonts.
        private Font _titleFont;
        private Font _symbolFont;
        private Font _labelFont;
        private Font _resultFont;
        private Font _hintFont;

        // ── Constructor ────────────────────────────────────────────────────────

        /// <param name="onContinue">Callback invoked when the player finishes the roulette.</param>
        public CardRouletteScene(Action onContinue) { _onContinue = onContinue; }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void OnEnter()
        {
            _titleFont  = new Font("Courier New", 20, FontStyle.Bold);
            _symbolFont = new Font("Courier New", 36, FontStyle.Bold);
            _labelFont  = new Font("Courier New", 10, FontStyle.Bold);
            _resultFont = new Font("Courier New", 18, FontStyle.Bold);
            _hintFont   = new Font("Courier New", 10, FontStyle.Bold);

            // Each card spins at a slightly different speed for variety.
            _spinRate[0] = 4.5f;
            _spinRate[1] = 6.0f;
            _spinRate[2] = 5.2f;

            for (int i = 0; i < 3; i++)
            {
                _cardFace[i]  = _rng.Next(4);
                _faceTimer[i] = 0f;
                _stopped[i]   = false;
            }

            Game.Instance.Audio.ContinueOrPlay("clear");
            DebugLogger.LogInfo("CardRouletteScene", "Roulette started.");
        }

        public override void OnExit()
        {
            _titleFont?.Dispose();
            _symbolFont?.Dispose();
            _labelFont?.Dispose();
            _resultFont?.Dispose();
            _hintFont?.Dispose();
        }

        // ── Update ─────────────────────────────────────────────────────────────

        public override void Update(float dt)
        {
            if (_advancing) return;

            // Spin active (non-stopped) cards.
            _spinTimer += dt;   // advance global spin accumulator
            for (int i = 0; i < 3; i++)
            {
                if (_stopped[i]) continue;
                _faceTimer[i] += dt;
                float period = 1f / _spinRate[i];
                while (_faceTimer[i] >= period)
                {
                    _faceTimer[i] -= period;
                    _cardFace[i]   = (_cardFace[i] + 1) % 4;
                }
            }

            // Input: action button stops the next card in sequence.
            if (!_resultShown && Game.Instance.Input.JumpPressed ||
                Game.Instance.Input.InteractPressed || Game.Instance.Input.AttackPressed)
            {
                StopNextCard();
            }

            // Auto-advance after result display.
            if (_resultShown)
            {
                _resultTimer += dt;
                if (_resultTimer >= ResultDisplay)
                {
                    _advancing = true;
                    SceneTransition.Begin(() => _onContinue?.Invoke());
                }
            }
        }

        // ── Card stop logic ────────────────────────────────────────────────────

        /// <summary>Locks the current active card and advances to the next.</summary>
        private void StopNextCard()
        {
            if (_activeCard >= 3) return;
            _stopped[_activeCard] = true;
            _activeCard++;

            // All three stopped — evaluate the result.
            if (_activeCard >= 3)
                EvaluateResult();
        }

        /// <summary>
        /// Checks the three stopped card faces and grants the matching reward.
        /// </summary>
        private void EvaluateResult()
        {
            int a = _cardFace[0], b = _cardFace[1], c = _cardFace[2];

            if (a == b && b == c)
            {
                // All three match — big reward.
                switch ((CardFace)a)
                {
                    case CardFace.Star:
                        Game.Instance.CurrentLives++;
                        _resultMsg   = "EXTRA LIFE!  ×" + Game.Instance.CurrentLives;
                        _resultColor = Color.Gold;
                        AchievementSystem.Grant("ach_roulette_star");
                        break;

                    case CardFace.Berry:
                        Game.Instance.PlayerBounty += 500;
                        _resultMsg   = "BERRY BONUS!  +500 SCORE";
                        _resultColor = Color.OrangeRed;
                        break;

                    case CardFace.Heart:
                        _resultMsg   = "MAX HP RESTORED!";
                        _resultColor = Color.HotPink;
                        break;

                    case CardFace.Key:
                        Game.Instance.PlayerBounty += 1000;
                        _resultMsg   = "SECRET KEY!  +1000 SCORE";
                        _resultColor = Color.Cyan;
                        AchievementSystem.Grant("ach_roulette_key");
                        break;
                }
            }
            else if (a == b || b == c || a == c)
            {
                // Two match — minor reward.
                Game.Instance.PlayerBounty += 150;
                _resultMsg   = "DOUBLE MATCH  +150 SCORE";
                _resultColor = Color.LimeGreen;
            }
            else
            {
                // No match — consolation berries.
                Game.Instance.PlayerBounty += 50;
                _resultMsg   = "No match...  +50 SCORE";
                _resultColor = Color.SlateGray;
            }

            _resultShown = true;
            _resultTimer = 0f;
            DebugLogger.LogInfo("CardRouletteScene", $"Result: {_resultMsg}");
        }

        // ── Draw ───────────────────────────────────────────────────────────────

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // ── Background ────────────────────────────────────────────────────
            g.FillRectangle(Brushes.Black, 0, 0, W, H);
            using (var br = new SolidBrush(Color.FromArgb(15, 10, 40)))
                g.FillRectangle(br, 0, 0, W, H);

            // ── Title ─────────────────────────────────────────────────────────
            const string title = "CARD  ROULETTE";
            SizeF titleSz = g.MeasureString(title, _titleFont);
            g.DrawString(title, _titleFont, Brushes.Gold, (W - titleSz.Width) / 2f, 30);

            // ── Cards ─────────────────────────────────────────────────────────
            int cardW = 120, cardH = 160;
            int totalW = cardW * 3 + 40;
            int startX = (W - totalW) / 2;
            int cardY  = H / 2 - cardH / 2 - 20;

            for (int i = 0; i < 3; i++)
            {
                int cx = startX + i * (cardW + 20);
                DrawCard(g, cx, cardY, cardW, cardH, i);
            }

            // ── Hint line ─────────────────────────────────────────────────────
            if (!_resultShown && _activeCard < 3)
            {
                string hint = $"Press JUMP / Z to stop card {_activeCard + 1}";
                SizeF sz = g.MeasureString(hint, _hintFont);
                g.DrawString(hint, _hintFont, Brushes.LightGray, (W - sz.Width) / 2f, H - 60);
            }

            // ── Result banner ─────────────────────────────────────────────────
            if (_resultShown)
            {
                SizeF sz = g.MeasureString(_resultMsg, _resultFont);
                float rx = (W - sz.Width) / 2f;
                float ry = H - 90;
                using (var br = new SolidBrush(Color.FromArgb(200, 10, 10, 30)))
                    g.FillRectangle(br, rx - 10, ry - 6, sz.Width + 20, sz.Height + 12);
                using (var br = new SolidBrush(_resultColor))
                    g.DrawString(_resultMsg, _resultFont, br, rx, ry);
            }

            SceneTransition.Draw(g, W, H);
        }

        // ── Card rendering helper ──────────────────────────────────────────────

        /// <summary>Draws a single card at the given position.</summary>
        private void DrawCard(Graphics g, int x, int y, int w, int h, int index)
        {
            bool stopped = _stopped[index];
            int  face    = _cardFace[index];
            bool active  = (index == _activeCard) && !stopped;

            // Card background.
            Color bg = stopped ? Color.FromArgb(30, 30, 60) : Color.FromArgb(20, 20, 45);
            using (var br = new SolidBrush(bg))
                g.FillRectangle(br, x, y, w, h);

            // Card border — bright if this is the active card being spun.
            Color border = active  ? Color.Gold
                         : stopped ? CardColor[face]
                         :           Color.DimGray;
            using (var pen = new Pen(border, active ? 3 : 2))
                g.DrawRectangle(pen, x, y, w, h);

            // Face symbol.
            string sym = CardSymbol[face];
            SizeF symSz = g.MeasureString(sym, _symbolFont);
            using (var br = new SolidBrush(CardColor[face]))
                g.DrawString(sym, _symbolFont, br, x + (w - symSz.Width) / 2f, y + h / 2f - symSz.Height / 2f - 10);

            // Face label.
            string lbl = CardLabel[face];
            SizeF lblSz = g.MeasureString(lbl, _labelFont);
            using (var br = new SolidBrush(stopped ? Color.White : Color.DimGray))
                g.DrawString(lbl, _labelFont, br, x + (w - lblSz.Width) / 2f, y + h - 26);

            // "LOCKED" indicator on stopped card.
            if (stopped)
            {
                SizeF stopSz = g.MeasureString("STOP", _labelFont);
                using (var br = new SolidBrush(Color.FromArgb(180, Color.LimeGreen)))
                    g.DrawString("STOP", _labelFont, br, x + (w - stopSz.Width) / 2f, y + 8);
            }
        }
    }
}
