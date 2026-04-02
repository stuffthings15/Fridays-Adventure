using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  LevelIntroScene.cs  —  SMB3-style World-Level Entry Card
    //
    //  Displayed for ~2.5 s before every gameplay level.
    //  Shows a dark background with the world number, level number, level name,
    //  archetype portrait hint, and a "READY?" prompt that blinks before fading
    //  to the actual level scene.
    //
    //  Team 1 (Game Director) — Idea 1: SMB3 level intro card animation.
    //  Team 1 (Game Director) — Idea 2: World-X / Level-X label system.
    // ═══════════════════════════════════════════════════════════════════════════
    public sealed class LevelIntroScene : Scene
    {
        // ── Configuration ──────────────────────────────────────────────────────
        private const float CardSlideDuration = 0.45f;   // seconds for card to slide in
        private const float HoldDuration      = 1.60f;   // seconds card is fully visible
        private const float FadeDuration      = 0.35f;   // seconds for fade-out
        private const float TotalDuration     = CardSlideDuration + HoldDuration + FadeDuration;

        // ── State ─────────────────────────────────────────────────────────────
        private readonly string  _levelName;   // e.g. "Ice Cliffs"
        private readonly int     _worldNum;    // 1-based world number
        private readonly int     _levelNum;    // 1-based level within world
        private readonly bool    _isAirship;   // Team 1 Idea 10: airship level marker
        private readonly bool    _isToadHouse; // Team 1 Idea 5: Toad House bonus room flag
        private readonly Action  _nextScene;   // callback that pushes the actual level

        private float _timer;

        // ── Font resources ────────────────────────────────────────────────────
        private Font _worldFont;
        private Font _nameFont;
        private Font _readyFont;
        private Font _hintsFont;

        /// <summary>
        /// Creates the intro card.
        /// </summary>
        /// <param name="worldNum">SMB3 world number (e.g. 1).</param>
        /// <param name="levelNum">Level within the world (e.g. 2).</param>
        /// <param name="levelName">Friendly level name (e.g. "Ice Cliffs").</param>
        /// <param name="nextScene">Lambda that pushes the actual gameplay scene.</param>
        /// <param name="isAirship">Pass true for Airship-type levels.</param>
        /// <param name="isToadHouse">Pass true for Toad House bonus rooms.</param>
        public LevelIntroScene(int worldNum, int levelNum, string levelName,
                               Action nextScene,
                               bool isAirship = false, bool isToadHouse = false)
        {
            _worldNum   = worldNum;
            _levelNum   = levelNum;
            _levelName  = levelName;
            _nextScene  = nextScene;
            _isAirship  = isAirship;
            _isToadHouse = isToadHouse;
        }

        public override void OnEnter()
        {
            _worldFont = new Font("Courier New", 26, FontStyle.Bold);
            _nameFont  = new Font("Courier New", 14, FontStyle.Bold);
            _readyFont = new Font("Courier New", 11, FontStyle.Bold);
            _hintsFont = new Font("Courier New", 9,  FontStyle.Regular);

            // Log the level entry for QA breadcrumb.
            DebugLogger.PushBreadcrumb($"LevelIntro: W{_worldNum}-{_levelNum} '{_levelName}'");
            DebugLogger.LogInfo("LevelIntroScene", $"Showing intro for W{_worldNum}-{_levelNum} '{_levelName}' airship={_isAirship}");

            // Play the SMB3 world-entry jingle via AudioManager.
            Game.Instance.Audio.BeepLevelIntro();
        }

        public override void OnExit()
        {
            _worldFont?.Dispose();
            _nameFont?.Dispose();
            _readyFont?.Dispose();
            _hintsFont?.Dispose();
        }

        public override void Update(float dt)
        {
            _timer += dt;

            // Once the full animation has played, transition to the actual level.
            if (_timer >= TotalDuration)
            {
                _nextScene?.Invoke();
            }
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // ── Dark SMB3-style background ─────────────────────────────────
            g.Clear(Color.FromArgb(10, 8, 20));

            // Soft starfield background dots.
            DrawStarfield(g, W, H);

            // ── Card slide-in from the left ────────────────────────────────
            float slideProgress = Math.Min(1f, _timer / CardSlideDuration);
            // EaseOutBack gives a satisfying overshoot snap.
            float easedSlide = EasingFunctions.EaseOutBack(slideProgress);
            int cardW = (int)(W * 0.72f);
            int cardH = 160;
            int cardX = (int)((W - cardW) / 2f);
            int cardY = (int)(H / 2f - cardH / 2f);
            // Start position is off-screen to the right.
            float startX = W + cardW;
            float targetX = cardX;
            float drawX  = startX + (targetX - startX) * easedSlide;

            // ── Fade-out overlay ───────────────────────────────────────────
            float fadeProgress = Math.Max(0f, (_timer - CardSlideDuration - HoldDuration) / FadeDuration);
            int fadeAlpha = (int)(255 * fadeProgress);

            // ── Card shadow ────────────────────────────────────────────────
            using (var br = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                g.FillRectangle(br, drawX + 6, cardY + 6, cardW, cardH);

            // ── Card background ────────────────────────────────────────────
            // Airship = dark steel; Toad House = green; normal = navy SMB3 blue.
            Color cardBg = _isAirship  ? Color.FromArgb(220, 30, 30, 40)
                         : _isToadHouse ? Color.FromArgb(220, 20, 60, 20)
                         :                Color.FromArgb(220, 10, 25, 70);
            using (var br = new SolidBrush(cardBg))
                g.FillRectangle(br, drawX, cardY, cardW, cardH);

            // Gold border (two-pixel thick).
            Color borderColor = _isAirship ? Color.Silver : (_isToadHouse ? Color.LimeGreen : Color.Gold);
            using (var pen = new Pen(borderColor, 3))
                g.DrawRectangle(pen, drawX, cardY, cardW, cardH);

            // Inner subtle border.
            using (var pen = new Pen(Color.FromArgb(60, borderColor), 1))
                g.DrawRectangle(pen, drawX + 4, cardY + 4, cardW - 8, cardH - 8);

            // ── World-Level label (top-left of card) ──────────────────────
            string worldLabel = _isAirship
                ? $"WORLD {_worldNum}  ✈  AIRSHIP"
                : _isToadHouse
                    ? $"WORLD {_worldNum}  ♣  BONUS"
                    : $"WORLD {_worldNum} - {_levelNum}";

            using (var br = new SolidBrush(borderColor))
                g.DrawString(worldLabel, _worldFont, br, drawX + 16f, cardY + 14f);

            // ── Level name ────────────────────────────────────────────────
            using (var br = new SolidBrush(Color.White))
                g.DrawString(_levelName, _nameFont, br, drawX + 20f, cardY + 68f);

            // ── Archetype hint line ───────────────────────────────────────
            string hint = CharacterHint(Game.Instance.SelectedCharacter);
            using (var br = new SolidBrush(Color.FromArgb(180, Color.LightCyan)))
                g.DrawString(hint, _hintsFont, br, drawX + 20f, cardY + 96f);

            // ── Star rating for this level (if already beaten) ────────────
            int stars = Game.Instance.GetLevelStars(_worldNum, _levelNum);
            DrawStarRating(g, (int)(drawX + cardW - 80), cardY + cardH - 36, stars);

            // ── Blinking READY prompt ─────────────────────────────────────
            bool showReady = (int)(_timer * 6) % 2 == 0;
            if (showReady && _timer < CardSlideDuration + HoldDuration)
                using (var br = new SolidBrush(Color.LimeGreen))
                    g.DrawString("READY?", _readyFont, br, (W - 70f) / 2f, cardY + cardH + 18f);

            // ── Fade-out overlay ───────────────────────────────────────────
            if (fadeAlpha > 0)
                using (var br = new SolidBrush(Color.FromArgb(Math.Min(255, fadeAlpha), Color.Black)))
                    g.FillRectangle(br, 0, 0, W, H);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        /// <summary>Draws a simple grid of faint star dots behind the card.</summary>
        private static void DrawStarfield(Graphics g, int W, int H)
        {
            var rng = new Random(42); // deterministic seed so stars don't shimmer
            for (int i = 0; i < 60; i++)
            {
                int sx = rng.Next(W);
                int sy = rng.Next(H);
                int alpha = rng.Next(60, 160);
                using (var br = new SolidBrush(Color.FromArgb(alpha, Color.White)))
                    g.FillRectangle(br, sx, sy, 2, 2);
            }
        }

        /// <summary>
        /// Returns a short archetype-specific tip shown on the intro card.
        /// Team 1 (Game Director) — Idea 9: contextual archetype briefing.
        /// </summary>
        private static string CharacterHint(PlayableCharacter arch)
        {
            switch (arch)
            {
                case PlayableCharacter.Orca:
                    return "Orca: Use Ground Pound to break obstacles and stun enemies.";
                case PlayableCharacter.Swan:
                    return "Swan: Hold Jump to glide over long gaps and tricky sections.";
                default:
                    return "Miss Friday: Balance Ice abilities to control the battlefield.";
            }
        }

        /// <summary>
        /// Draws 1–3 gold star icons to represent the previous best rating.
        /// Team 1 (Game Director) — Idea 3: star ratings per level.
        /// </summary>
        private static void DrawStarRating(Graphics g, int x, int y, int stars)
        {
            for (int i = 0; i < 3; i++)
            {
                Color c = (i < stars) ? Color.Gold : Color.FromArgb(50, Color.Gold);
                using (var br = new SolidBrush(c))
                    g.FillEllipse(br, x + i * 22, y, 14, 14);
                using (var f = new Font("Courier New", 8))
                    g.DrawString("★", f, i < stars ? Brushes.Gold : Brushes.DimGray, x + i * 22 - 1f, y);
            }
        }

        // LevelIntroScene does not respond to pointer clicks — auto-advances by timer.
        public override void HandleClick(Point p) { }
    }
}
