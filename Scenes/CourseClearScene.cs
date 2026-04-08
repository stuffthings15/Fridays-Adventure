using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// SMB3-style "COURSE CLEAR!" fanfare screen.
    ///
    /// Displays after each level is successfully completed.  Shows:
    ///   • "COURSE CLEAR!" banner (slides in with bounce easing).
    ///   • Time bonus — counts down remaining seconds and adds them to score.
    ///   • Player grade: S / A / B / C / F  based on time + deaths.
    ///   • Continue button / auto-advance after 4 seconds.
    ///
    /// Team 1  (Game Director)  — Idea 1: SMB3-style course-clear fanfare.
    /// Team 1  (Game Director)  — Idea 8: player rank/grade display.
    /// Team 9  (UI Programmer)  — Idea 8: world/level identifier on this screen.
    /// Team 15 (UI/UX Artist)   — Idea 10: "GET READY!" / "COURSE CLEAR!" screen styling.
    /// Team 18 (Sound Designer) — Idea 3: course clear fanfare SFX hook.
    /// </summary>
    public sealed class CourseClearScene : Scene
    {
        // ── Construction data ─────────────────────────────────────────────────
        private readonly string  _levelName;     // e.g. "Dinosaur Island"
        private readonly int     _timeRemaining; // seconds left on level timer (0 = no timer)
        private readonly int     _deathCount;    // deaths taken during this level run
        private readonly Action  _onContinue;    // called when player continues

        // ── Banner slide-in ───────────────────────────────────────────────────
        private float _bannerTimer;
        private const float BannerDuration = 0.55f;

        // ── Time bonus countdown ──────────────────────────────────────────────
        private bool  _bonusCounting;
        private float _bonusTimer;       // drives the tick rate
        private int   _bonusRemaining;   // remaining seconds to count down
        private const float BonusTickRate = 0.04f; // seconds per bonus tick
        private const int   BonusPerSec  = 10;     // score added per remaining second
        private const int   MaxBonusCountdownSeconds = 30; // cap to prevent long post-clear stalls

        // ── Auto-advance ──────────────────────────────────────────────────────
        private float _autoTimer;
        private const float AutoDelay = 1.25f;
        private bool  _advancing;

        // ── Grade ─────────────────────────────────────────────────────────────
        private string _grade;
        private Color  _gradeColor;

        // ── Fonts ─────────────────────────────────────────────────────────────
        private Font _titleFont;
        private Font _subFont;
        private Font _gradeFont;
        private Font _infoFont;

        /// <summary>
        /// Creates a CourseClearScene.
        /// </summary>
        /// <param name="levelName">Display name of the just-cleared level.</param>
        /// <param name="timeRemaining">Seconds remaining on level timer (0 = not used).</param>
        /// <param name="deathCount">Player deaths during this run.</param>
        /// <param name="onContinue">Callback invoked when the player continues.</param>
        public CourseClearScene(string levelName, int timeRemaining, int deathCount, Action onContinue)
        {
            _levelName     = levelName;
            _timeRemaining = timeRemaining;
            _deathCount    = deathCount;
            _onContinue    = onContinue;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void OnEnter()
        {
            _titleFont = new Font("Courier New", 26, FontStyle.Bold);
            _subFont   = new Font("Courier New", 14, FontStyle.Bold);
            _gradeFont = new Font("Courier New", 60, FontStyle.Bold);
            _infoFont  = new Font("Courier New", 11, FontStyle.Bold);

            // Start banner slide-in immediately.
            _bannerTimer    = 0f;
            _bonusRemaining = Math.Max(0, Math.Min(_timeRemaining, MaxBonusCountdownSeconds));
            _bonusCounting  = false;
            _autoTimer      = 0f;

            // Calculate grade before counting so the grade letter is ready.
            _grade      = CalcGrade(_timeRemaining, _deathCount);
            _gradeColor = GradeColor(_grade);

            // Mark level as cleared for the overworld progression gate.
            Game.Instance.LevelJustCompleted = true;

            // Play course-clear music via AudioManager (SMB3-style fanfare).
            Game.Instance.Audio.ContinueOrPlay("clear");

            // Log to session stats.
            SessionStats.Instance.RecordLevelCompleted();

            DebugLogger.LogInfo("CourseClearScene", $"Level cleared: {_levelName}, Grade={_grade}, Deaths={_deathCount}");
        }

        public override void OnExit()
        {
            _titleFont?.Dispose();
            _subFont?.Dispose();
            _gradeFont?.Dispose();
            _infoFont?.Dispose();
        }

        // ── Update ─────────────────────────────────────────────────────────────

        public override void Update(float dt)
        {
            // Banner slide-in.
            if (_bannerTimer < BannerDuration) _bannerTimer += dt;

            // Start bonus countdown once banner finishes.
            if (!_bonusCounting && _bannerTimer >= BannerDuration)
            {
                _bonusCounting = true;
            }

            // Bonus tick countdown.
            if (_bonusCounting && _bonusRemaining > 0)
            {
                _bonusTimer += dt;
                while (_bonusTimer >= BonusTickRate && _bonusRemaining > 0)
                {
                    _bonusTimer    -= BonusTickRate;
                    _bonusRemaining--;
                    // Add score for each second ticked down.
                    Game.Instance.PlayerBounty += BonusPerSec;
                    Game.Instance.FloatingText.Spawn(
                        $"+{BonusPerSec}", 450, 280, Color.Gold);
                }
            }

            // Auto-advance once countdown finishes (or if skipped).
            bool countDone = _bonusCounting && _bonusRemaining <= 0;
            if (countDone || _timeRemaining == 0)
                _autoTimer += dt;

            var input = Game.Instance.Input;
            // Player can press any action key to skip/advance.
            if ((input.InteractPressed || input.JumpPressed || input.AttackPressed) && !_advancing)
            {
                _bonusRemaining = 0;
                _autoTimer      = AutoDelay;
            }

            // Bot/Demo mode: skip the bonus countdown immediately so the
            // scene auto-advances without waiting for input.
            if (DialogueScene.AutoAdvance && _bonusRemaining > 0)
                _bonusRemaining = 0;

            if (_autoTimer >= AutoDelay && !_advancing)
            {
                _advancing = true;
                SceneTransition.Begin(() => _onContinue?.Invoke());
            }
        }

        // ── Draw ───────────────────────────────────────────────────────────────

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // ── Dark navy background (SMB3 clear screen palette) ───────────────
            using (var br = new LinearGradientBrush(
                new Point(0, 0), new Point(0, H),
                Color.FromArgb(10, 10, 40), Color.FromArgb(20, 30, 80)))
                g.FillRectangle(br, 0, 0, W, H);

            // ── Gold star decorations (SMB3 style) ────────────────────────────
            DrawStarField(g, W, H);

            // ── "COURSE CLEAR!" banner (bounces in from above) ────────────────
            float bannerT = _bannerTimer / BannerDuration;
            float bannerY = -80f + EasingFunctions.EaseOutBounce(bannerT) * (H / 2f - 80f);

            string title = "COURSE  CLEAR!";
            SizeF titleSz = g.MeasureString(title, _titleFont);
            // Shadow
            g.DrawString(title, _titleFont, Brushes.DarkOrange, (W - titleSz.Width) / 2f + 3, bannerY + 3);
            g.DrawString(title, _titleFont, Brushes.Gold,       (W - titleSz.Width) / 2f,     bannerY);

            // ── Level name ────────────────────────────────────────────────────
            string levelStr = $"~  {_levelName}  ~";
            SizeF levelSz = g.MeasureString(levelStr, _subFont);
            g.DrawString(levelStr, _subFont, Brushes.LightCyan, (W - levelSz.Width) / 2f, H / 2f - 26);

            // ── Time bonus row ────────────────────────────────────────────────
            if (_bonusCounting)
            {
                int shown = _bonusRemaining;
                g.DrawString($"TIME BONUS: {shown,5} × {BonusPerSec}", _infoFont, Brushes.White, W / 2f - 110, H / 2f + 30);
                g.DrawString($"TOTAL SCORE: {Game.Instance.PlayerBounty:N0}", _infoFont, Brushes.Yellow, W / 2f - 100, H / 2f + 58);
            }

            // ── Grade letter (right side) ─────────────────────────────────────
            if (_bonusCounting)
            {
                SizeF gsz = g.MeasureString(_grade, _gradeFont);
                float gx = W - gsz.Width - 60;
                float gy = H / 2f - gsz.Height / 2f;
                // Glow shadow
                using (var br = new SolidBrush(Color.FromArgb(80, _gradeColor)))
                    g.DrawString(_grade, _gradeFont, br, gx + 4, gy + 4);
                using (var br = new SolidBrush(_gradeColor))
                    g.DrawString(_grade, _gradeFont, br, gx, gy);
            }

            // ── "Press any button" prompt ─────────────────────────────────────
            if (_bonusRemaining <= 0)
            {
                bool blink = (int)((_autoTimer) * 4) % 2 == 0;
                if (blink)
                {
                    const string prompt = "[ Press any button to continue ]";
                    SizeF promptSz = g.MeasureString(prompt, _infoFont);
                    g.DrawString(prompt, _infoFont, Brushes.LightYellow, (W - promptSz.Width) / 2f, H - 60);
                }
            }

            // Curtain wipe overlay.
            SceneTransition.Draw(g, W, H);
        }

        // ── Grade calculation ──────────────────────────────────────────────────

        /// <summary>
        /// Calculates a letter grade based on time remaining and deaths.
        /// S = perfect (0 deaths, lots of time). F = gave up.
        /// </summary>
        private static string CalcGrade(int timeLeft, int deaths)
        {
            if (deaths == 0 && timeLeft >= 180) return "S";
            if (deaths <= 1 && timeLeft >= 90)  return "A";
            if (deaths <= 2 && timeLeft >= 30)  return "B";
            if (deaths <= 4)                    return "C";
            return "F";
        }

        /// <summary>Returns the color for each grade letter (SMB3-style palette).</summary>
        private static Color GradeColor(string grade)
        {
            switch (grade)
            {
                case "S": return Color.Gold;
                case "A": return Color.LimeGreen;
                case "B": return Color.DeepSkyBlue;
                case "C": return Color.Orange;
                default:  return Color.OrangeRed;
            }
        }

        // ── Background helpers ─────────────────────────────────────────────────

        /// <summary>Draws a small static star field for the SMB3 clear background.</summary>
        /// <remarks>Uses Kenney CC0 star sprite if available, GDI ellipse fallback.</remarks>
        private static void DrawStarField(Graphics g, int W, int H)
        {
            // Kenney CC0 star sprite for higher-quality background decoration
            Bitmap starTile = Data.SpriteManager.GetScaled("item_star.png", 10, 10);
            // Deterministic positions so they don't flicker every frame.
            for (int i = 0; i < 30; i++)
            {
                int x = (i * 83 + 17) % (W - 10);
                int y = (i * 47 + 29) % (H - 10);
                int r = (i % 3 == 0) ? 8 : 5;
                if (starTile != null)
                    g.DrawImage(starTile, x, y, r, r);
                else
                    using (var br = new SolidBrush(Color.FromArgb(180, Color.Gold)))
                        g.FillEllipse(br, x, y, r, r);
            }
        }

        // ── Unused abstract stubs ──────────────────────────────────────────────
        public override void HandleClick(Point p)
        {
            if (!_advancing)
            { _bonusRemaining = 0; _autoTimer = AutoDelay; }
        }
    }
}
