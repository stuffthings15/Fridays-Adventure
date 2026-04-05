// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 – Multi-Team Implementation
// Scenes/WorldTitleScene.cs
// Purpose: SMB3-style full-screen "WORLD X" title card shown when entering
//          a new world for the first time.
// ────────────────────────────────────────────────────────────────────────────
// Team 1  (Game Director)       – Idea 11: World number progression trigger
// Team 1  (Game Director)       – Idea 12: Auto-advance after delay
// Team 9  (UI Programmer)       – Idea 14: Full-screen "WORLD X" display
// Team 9  (UI Programmer)       – Idea 15: Sub-title theme description text
// Team 10 (Engine Programmer)   – Idea 1:  Fade-in / fade-out screen transition
// Team 14 (Environment Artist)  – Idea 12: World-themed background gradient
// Team 17 (VFX Artist)          – Idea 10: Star sparkle overlay
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Full-screen "WORLD X" intro card.
    /// Fades in, displays 2.5 seconds, then fades out and calls <see cref="_onDone"/>.
    ///
    /// Team 1  (Game Director)  — Idea 11–12.
    /// Team 9  (UI Programmer)  — Idea 14–15.
    /// Team 10 (Engine Programmer) — Idea 1: fade transition.
    /// Team 14 (Environment Artist) — Idea 12.
    /// Team 17 (VFX Artist)     — Idea 10.
    /// </summary>
    public sealed class WorldTitleScene : Scene
    {
        // ── Config ────────────────────────────────────────────────────────────
        private readonly int    _worldNumber;
        private readonly string _worldName;
        private readonly Action _onDone;

        // ── Phase timer (Team 10 — Idea 1: fade transitions) ──────────────────
        private const float FadeInDuration  = 0.6f;
        private const float HoldDuration    = 2.5f;
        private const float FadeOutDuration = 0.5f;
        private float _timer;

        // ── Sparkle state (Team 17 — Idea 10) ─────────────────────────────────
        private readonly Random _rng = new Random();
        private float   _sparkleTimer;
        private const float SparkleInterval = 0.08f;

        // ── Title text bounce ─────────────────────────────────────────────────
        private float _bounceTimer;

        // ── Fonts ─────────────────────────────────────────────────────────────
        private static readonly Font _worldFont = new Font("Courier New", 52, FontStyle.Bold);
        private static readonly Font _nameFont  = new Font("Courier New", 18, FontStyle.Bold);
        private static readonly Font _subFont   = new Font("Courier New", 13);

        /// <summary>
        /// Creates a WorldTitleScene.
        /// </summary>
        /// <param name="worldNumber">1-based world number.</param>
        /// <param name="worldName">Theme name, e.g. "Dinosaur Shores".</param>
        /// <param name="onDone">Action invoked when the card finishes.</param>
        public WorldTitleScene(int worldNumber, string worldName, Action onDone)
        {
            _worldNumber = worldNumber;
            _worldName   = worldName;
            _onDone      = onDone;
        }

        public override void OnEnter()
        {
            Game.Instance.Audio.StopMusic();
        }

        public override void OnExit() { }

        // ── Update ─────────────────────────────────────────────────────────────

        private bool _hasTransitioned;

        public override void Update(float dt)
        {
            _timer        += dt;
            _bounceTimer  += dt;

            // Sparkle spawning (Team 17 — Idea 10)
            _sparkleTimer += dt;
            if (_sparkleTimer >= SparkleInterval)
            {
                _sparkleTimer = 0f;
                int W = Game.Instance.CanvasWidth;
                int H = Game.Instance.CanvasHeight;
                ParticleSystem.SpawnCoinSparkle(
                    _rng.Next(W), _rng.Next(H / 4, H * 3 / 4));
            }

            float totalDuration = FadeInDuration + HoldDuration + FadeOutDuration;
            if (_timer >= totalDuration && !_hasTransitioned)
            {
                _hasTransitioned = true;
                Game.Instance.Scenes.Pop();
                _onDone?.Invoke();
            }

            // Skip on interact press
            if ((Game.Instance.Input.InteractPressed || Game.Instance.Input.JumpPressed) && !_hasTransitioned)
            {
                _hasTransitioned = true;
                Game.Instance.Scenes.Pop();
                _onDone?.Invoke();
            }
        }

        // ── Draw ───────────────────────────────────────────────────────────────

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Compute fade alpha (Team 10 — Idea 1)
            float alpha;
            if (_timer < FadeInDuration)
                alpha = _timer / FadeInDuration;
            else if (_timer < FadeInDuration + HoldDuration)
                alpha = 1f;
            else
                alpha = 1f - (_timer - FadeInDuration - HoldDuration) / FadeOutDuration;
            alpha = Math.Max(0f, Math.Min(1f, alpha));
            int a = (int)(alpha * 255);

            // Background gradient (Team 14 — Idea 12)
            Color bgTop, bgBot;
            GetWorldColors(_worldNumber, out bgTop, out bgBot);
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                Color.FromArgb(a, bgTop), Color.FromArgb(a, bgBot), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // Horizontal separator lines
            int lineY = H / 2 - 10;
            using (var pen = new Pen(Color.FromArgb(a, Color.White), 3))
            {
                g.DrawLine(pen, 40, lineY - 44, W - 40, lineY - 44);
                g.DrawLine(pen, 40, lineY + 80, W - 40, lineY + 80);
            }

            // "WORLD X" title (Team 9 — Idea 14)
            float bounce = (float)Math.Sin(_bounceTimer * 4f) * 4f;
            string worldLabel = $"WORLD {_worldNumber}";
            var wsz = g.MeasureString(worldLabel, _worldFont);
            using (var br = new SolidBrush(Color.FromArgb(a, Color.White)))
                g.DrawString(worldLabel, _worldFont, br,
                    (W - wsz.Width) / 2f,
                    lineY - 38 + bounce);

            // World name sub-title (Team 9 — Idea 15)
            var nsz = g.MeasureString(_worldName, _nameFont);
            using (var br = new SolidBrush(Color.FromArgb(a, Color.FromArgb(255, 220, 120))))
                g.DrawString(_worldName, _nameFont, br,
                    (W - nsz.Width) / 2f,
                    lineY + 50);

            // Descriptive tagline (Team 9 — Idea 15)
            string tag = GetWorldTag(_worldNumber);
            var tsz = g.MeasureString(tag, _subFont);
            using (var br = new SolidBrush(Color.FromArgb((int)(a * 0.7f), Color.LightCyan)))
                g.DrawString(tag, _subFont, br,
                    (W - tsz.Width) / 2f,
                    lineY + 78);

            // Stars icon decorations
            using (var br = new SolidBrush(Color.FromArgb(a, Color.Gold)))
            {
                DrawStar(g, br, W * 0.1f, H * 0.2f, 20);
                DrawStar(g, br, W * 0.9f, H * 0.2f, 20);
                DrawStar(g, br, W * 0.1f, H * 0.8f, 14);
                DrawStar(g, br, W * 0.9f, H * 0.8f, 14);
            }

            // "Press any key to skip" hint
            float hintAlpha = (float)(Math.Sin(_bounceTimer * 2) * 0.4 + 0.6) * alpha;
            const string hint = "Press ↵ to skip";
            var hsz = g.MeasureString(hint, _subFont);
            using (var br = new SolidBrush(Color.FromArgb((int)(hintAlpha * 255), Color.White)))
                g.DrawString(hint, _subFont, br,
                    (W - hsz.Width) / 2f, H - 40);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns world-themed gradient colors.
        /// Team 14 (Environment Artist) — Idea 12.
        /// </summary>
        private static void GetWorldColors(int world, out Color top, out Color bot)
        {
            switch ((world - 1) % 8)
            {
                case 0:  top = Color.FromArgb(30, 120, 50);  bot = Color.FromArgb(10, 60, 20);  break;  // Green
                case 1:  top = Color.FromArgb(40, 80, 200);  bot = Color.FromArgb(10, 30, 100); break;  // Sky
                case 2:  top = Color.FromArgb(180, 80, 20);  bot = Color.FromArgb(100, 30, 0);  break;  // Desert
                case 3:  top = Color.FromArgb(20, 140, 160); bot = Color.FromArgb(5, 60, 90);   break;  // Ocean
                case 4:  top = Color.FromArgb(140, 30, 140); bot = Color.FromArgb(60, 10, 60);  break;  // Dark
                case 5:  top = Color.FromArgb(160, 200, 230);bot = Color.FromArgb(80, 140, 200);break;  // Ice
                case 6:  top = Color.FromArgb(200, 50, 30);  bot = Color.FromArgb(120, 10, 0);  break;  // Fire
                default: top = Color.FromArgb(20, 20, 20);   bot = Color.FromArgb(5, 5, 5);     break;  // Final
            }
        }

        /// <summary>
        /// Returns a flavour tagline per world.
        /// Team 9 (UI Programmer) — Idea 15.
        /// </summary>
        private static string GetWorldTag(int world)
        {
            switch (world)
            {
                case 1: return "Ancient jungles teeming with primeval beasts.";
                case 2: return "Wind-swept islands drifting among the clouds.";
                case 3: return "The iron depths of the Marine blockade.";
                case 4: return "Crystal reefs and sunken ruins await.";
                case 5: return "Frozen tundra where the cold kills faster than steel.";
                case 6: return "The abyss yawns open. There is no turning back.";
                default: return "A new world filled with untold dangers…";
            }
        }

        /// <summary>
        /// Draws a simple 5-point star.
        /// Team 17 (VFX Artist) — Idea 10.
        /// </summary>
        private static void DrawStar(Graphics g, Brush br, float cx, float cy, float r)
        {
            var pts = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = i * (float)Math.PI / 5f - (float)Math.PI / 2f;
                float radius = i % 2 == 0 ? r : r * 0.4f;
                pts[i] = new PointF(
                    cx + (float)Math.Cos(angle) * radius,
                    cy + (float)Math.Sin(angle) * radius);
            }
            g.FillPolygon(br, pts);
        }
    }
}
