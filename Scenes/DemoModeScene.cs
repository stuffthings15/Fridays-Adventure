using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Tests;

namespace Fridays_Adventure.Scenes
{
    // ────────────────────────────────────────────
    // PHASE 2 - Team 10: Engine Programmer
    // Feature: Demo Mode Scene
    // Purpose: Main menu entry — runs bot through all levels frame-by-frame so the
    //          player can watch it playing live with a scrolling viewport.
    // ────────────────────────────────────────────

    /// <summary>
    /// Demo mode — bot runs each level frame-by-frame so the player can watch it
    /// on a scrolling mini-viewport, then see per-level results.
    /// Accessible from the main menu via the WATCH DEMO button.
    /// </summary>
    public sealed class DemoModeScene : Scene
    {
        // ── State machine ─────────────────────────────────────────────────
        private enum DemoState { Menu, Running, LevelResult, Summary }
        private DemoState _state = DemoState.Menu;

        // When true the menu is skipped and the demo starts immediately.
        // Used by the AFK auto-launch from TitleScene.
        private readonly bool _autoStart;

        // ── Level data ────────────────────────────────────────────────────
        private static readonly string[] LevelIds =
        {
            "dino","storm1","sky","blockade","wano","warlord1",
            "harbor","coral","tundra","storm2","warlord2",
            "dive_gate","sunken_gate","kelp","boiling_vent","abyss","centipede_final"
        };

        private static readonly string[] LevelNames =
        {
            "1. Dinosaur Island","2. Storm Belt","3. Sky Island","4. Marine Blockade",
            "5. Blade Nation","6. Warlord: Sudo","7. Harbor Town","8. Coral Reef",
            "9. Tundra Peak","10. Tempest Strait","11. Warlord: Vanta","12. Dive Gate",
            "13. Sunken Gate","14. Kelp Maze","15. Vent Ruins","16. Abyss","17. Centipede Boss"
        };

        private int _levelIndex = 0;

        // ── Results ───────────────────────────────────────────────────────
        private readonly List<EnhancedLevelTestResult> _demoResults = new List<EnhancedLevelTestResult>();
        private int   _resultIndex = 0;
        private float _resultTimer = 0f;
        private const float RESULT_HOLD = 3f;

        // ── UI ────────────────────────────────────────────────────────────
        private float _uiTimer = 0f;
        private int W, H;
        private Font _fontTitle;
        private Font _fontHead;
        private Font _fontBody;
        private Font _fontSmall;
        private Rectangle _btnStart;
        private Rectangle _btnBack;
        private readonly Random _rng = new Random(42);

        /// <summary>Default constructor — shows the WATCH DEMO menu first.</summary>
        public DemoModeScene() : this(autoStart: false) { }

        /// <summary>
        /// Auto-start constructor — skips the menu and runs the bot immediately.
        /// Used by the AFK idle timer in TitleScene.
        /// </summary>
        public DemoModeScene(bool autoStart) { _autoStart = autoStart; }

        public override void OnEnter()
        {
            _fontTitle = new Font("Courier New", 24, FontStyle.Bold);

            _fontHead  = new Font("Courier New", 13, FontStyle.Bold);
            _fontBody  = new Font("Courier New", 10);
            _fontSmall = new Font("Courier New", 8);
            _state       = DemoState.Menu;
            _levelIndex  = 0;
            _demoResults.Clear();
            _resultIndex = 0;
            _resultTimer = 0f;
            _uiTimer     = 0f;

            // AFK auto-launch: skip the menu and start playing immediately
            if (_autoStart)
                StartDemo();
        }

        public override void OnExit()
        {
            _fontTitle?.Dispose();
            _fontHead?.Dispose();
            _fontBody?.Dispose();
            _fontSmall?.Dispose();
        }

        // ── Update ────────────────────────────────────────────────────────
        public override void Update(float dt)
        {
            _uiTimer += dt;
            var input = Game.Instance.Input;

            switch (_state)
            {
                case DemoState.Menu:
                    if (input.PausePressed)             { Game.Instance.Scenes.Pop(); return; }
                    if (input.InteractPressed || input.AttackPressed) StartDemo();
                    break;

                case DemoState.Running:
                    // BotPlayLevelScene is on top of the stack while a level plays.
                    // DemoModeScene.Update is not called during that time.
                    // This branch handles the rare gap between levels.
                    if (input.PausePressed) { Game.Instance.Scenes.Pop(); return; }
                    TickBot(dt);
                    break;

                case DemoState.LevelResult:
                    _resultTimer += dt;
                    if (input.InteractPressed || input.AttackPressed || _resultTimer >= RESULT_HOLD)
                        AdvanceAfterResult();
                    break;

                case DemoState.Summary:
                    if (input.InteractPressed || input.AttackPressed || input.PausePressed)
                        Game.Instance.Scenes.Pop();
                    break;
            }
        }

        // ── Demo control ──────────────────────────────────────────────────
        private void StartDemo()
        {
            _levelIndex  = 0;
            _demoResults.Clear();
            _resultIndex = 0;
            // Go straight into the first real level
            _state = DemoState.Running;
            LaunchNextDemoLevel();
        }

        /// <summary>
        /// Pushes a BotPlayLevelScene for the current level index.
        /// The callback advances to the next level or shows the summary.
        /// </summary>
        private void LaunchNextDemoLevel()
        {
            if (_levelIndex >= LevelIds.Length)
            {
                _state = DemoState.Summary;
                return;
            }

            string id   = LevelIds[_levelIndex];
            string name = LevelNames[_levelIndex];
            int    idx  = _levelIndex;   // capture for lambda

            // Build the real game scene
            Scene inner = LevelSceneFactory.Create(id, name);

            // Wrap it so the bot controls the player
            var wrapper = new BotPlayLevelScene(inner, name, (beaten, elapsed) =>
            {
                // elapsed = -1 means the user pressed ESC to abort the demo
                if (elapsed < 0f)
                {
                    // Pop DemoModeScene itself — returns to TitleScene
                    Game.Instance.Scenes.Pop();
                    return;
                }
                var result = new EnhancedLevelTestResult
                {
                    LevelId        = id,
                    LevelName      = name,
                    IsBeatable     = beaten,
                    TimeToComplete = elapsed,
                    FailureReason  = beaten ? "" : "Timeout or insufficient progress",
                    BotData        = new Tests.AutoTestBot()
                };
                _demoResults.Add(result);

                _resultTimer = 0f;
                _resultIndex = _demoResults.Count - 1;
                _state       = DemoState.LevelResult;   // show the result card
            });

            Game.Instance.Scenes.Push(wrapper);
        }

        private void TickBot(float dt) { /* no-op — BotPlayLevelScene owns the frame loop */ }
        private void SimulateEncounters(float dt) { /* no-op */ }

        private void AdvanceAfterResult()
        {
            _levelIndex++;
            _state = DemoState.Running;
            LaunchNextDemoLevel();   // will set state to Summary when all levels done
        }

        // ── Draw ──────────────────────────────────────────────────────────
        public override void Draw(Graphics g)
        {
            W = (int)g.VisibleClipBounds.Width;  if (W <= 0) W = 800;
            H = (int)g.VisibleClipBounds.Height; if (H <= 0) H = 600;

            using (var br = new SolidBrush(Color.FromArgb(230, 15, 15, 35)))
                g.FillRectangle(br, 0, 0, W, H);

            switch (_state)
            {
                case DemoState.Menu:        DrawMenu(g);        break;
                case DemoState.Running:     DrawWaiting(g);     break;
                case DemoState.LevelResult: DrawLevelResult(g); break;
                case DemoState.Summary:     DrawSummary(g);     break;
            }
        }

        // ── Menu ──────────────────────────────────────────────────────────
        private void DrawMenu(Graphics g)
        {
            g.DrawString("BOT DEMO MODE", _fontTitle, Brushes.Gold, 30, 30);
            int y = 90;
            g.DrawString("Watch the AI bot play through all 17 levels live.", _fontBody, Brushes.White, 30, y); y += 24;
            g.DrawString("The bot runs frame-by-frame in a scrolling viewport.", _fontBody, Brushes.White, 30, y); y += 24;
            g.DrawString("You will see it move, jump, attack, collect items", _fontBody, Brushes.White, 30, y); y += 24;
            g.DrawString("and defeat enemies in real time.", _fontBody, Brushes.White, 30, y); y += 36;

            string[] features =
            {
                "Scrolling viewport follows the bot in real time",
                "Jump arc and attack slash visual indicators",
                "Item collection events logged with coordinates",
                "Enemy encounters logged with type and position",
                "Orange border when stuck-detection triggers",
                "Per-level result card shown after each level",
                "Full summary screen with beatable tally"
            };
            g.DrawString("What you will see:", _fontHead, Brushes.LimeGreen, 30, y); y += 28;
            foreach (var f in features)
            {
                g.DrawString("  • " + f, _fontSmall, Brushes.Cyan, 50, y);
                y += 18;
            }
            y += 20;
            g.DrawString("Press [ENTER] or click START DEMO to begin.", _fontBody, Brushes.Yellow, 30, y);

            int bw = 260, bh = 50;
            _btnStart = new Rectangle((W - bw) / 2, H - 130, bw, bh);
            DrawBtn(g, _btnStart, "START DEMO", Color.FromArgb(200, 30, 130, 30));

            _btnBack = new Rectangle((W - 150) / 2, H - 65, 150, 38);
            DrawBtn(g, _btnBack, "[ESC] BACK", Color.FromArgb(160, 100, 40, 0));
        }

        // ── Waiting (real game is on top of stack, drawn over us) ──────────
        private void DrawWaiting(Graphics g)
        {
            string name = _levelIndex < LevelNames.Length ? LevelNames[_levelIndex] : "...";
            g.DrawString("BOT DEMO MODE", _fontTitle, Brushes.Gold, 30, 30);
            g.DrawString($"Playing: {name}",   _fontHead,  Brushes.Cyan,   30, 80);
            g.DrawString($"Level {_levelIndex + 1} / {LevelIds.Length}", _fontBody, Brushes.White, 30, 112);
            g.DrawString("The bot is playing the actual game above...", _fontBody, Brushes.LightGray, 30, 136);
            DrawProgressBar(g, 20, H - 40, W - 40, 16);
        }

        private void DrawProgressBar(Graphics g, int x, int y, int bw, int bh)
        {
            float pct = LevelIds.Length > 0 ? _levelIndex / (float)LevelIds.Length : 0f;
            using (var br = new SolidBrush(Color.FromArgb(80, Color.Gray)))
                g.FillRectangle(br, x, y, bw, bh);
            using (var br = new SolidBrush(Color.FromArgb(200, Color.LimeGreen)))
                g.FillRectangle(br, x, y, (int)(bw * pct), bh);
            using (var pen = new Pen(Color.White, 1))
                g.DrawRectangle(pen, x, y, bw, bh);
            string txt = $"{_levelIndex}/{LevelIds.Length} levels";
            SizeF sz = g.MeasureString(txt, _fontSmall);
            g.DrawString(txt, _fontSmall, Brushes.White, x + (bw - sz.Width) / 2f, y + 1);
        }

        // ── Level result card ─────────────────────────────────────────────
        private void DrawLevelResult(Graphics g)
        {
            if (_resultIndex >= _demoResults.Count) return;
            var r = _demoResults[_resultIndex];

            int px = 40, py = 60, pw = W - 80, ph = H - 140;
            Color border = r.IsBeatable ? Color.LimeGreen : Color.OrangeRed;
            using (var br = new SolidBrush(Color.FromArgb(200, 20, 30, 60)))
                g.FillRectangle(br, px, py, pw, ph);
            using (var pen = new Pen(border, 3))
                g.DrawRectangle(pen, px, py, pw, ph);

            int y = py + 20;
            g.DrawString(r.LevelName, _fontHead, new SolidBrush(border), px + 20, y); y += 34;
            g.DrawString(r.IsBeatable ? "✅ BEATABLE" : "❌ NOT BEATABLE", _fontHead, Brushes.White, px + 20, y); y += 28;
            g.DrawString($"Time to complete : {r.TimeToComplete:F1}s",            _fontBody, Brushes.White,     px + 20, y); y += 22;
            g.DrawString($"Distance traveled: {r.BotData?.DistanceTraveled:F0}px", _fontBody, Brushes.White,    px + 20, y); y += 22;
            g.DrawString($"Items collected  : {r.ItemsCollected}",                 _fontBody, Brushes.Gold,      px + 20, y); y += 22;
            g.DrawString($"Enemies defeated : {r.EnemiesDefeated}",                _fontBody, Brushes.OrangeRed, px + 20, y); y += 22;
            if (r.BotGotStuck)
            {
                g.DrawString($"⚠ Bot stuck for {r.StuckDuration:F1}s", _fontBody, Brushes.Orange, px + 20, y);
                y += 22;
            }
            if (!string.IsNullOrEmpty(r.FailureReason))
            {
                g.DrawString($"Issue: {r.FailureReason}", _fontBody, Brushes.OrangeRed, px + 20, y);
                y += 22;
            }
            if (r.VisualDebugData != null)
            {
                y += 6;
                g.DrawString(r.VisualDebugData.GetAnalysisSummary(), _fontSmall, Brushes.Cyan, px + 20, y);
            }

            float hold = Math.Max(0f, RESULT_HOLD - _resultTimer);
            string nav = _levelIndex < LevelIds.Length
                ? $"[ENTER] Next level  (auto in {hold:F0}s)"
                : $"[ENTER] View summary  (auto in {hold:F0}s)";
            g.DrawString(nav, _fontSmall, Brushes.White, px + 20, py + ph - 24);
        }

        // ── Summary ───────────────────────────────────────────────────────
        private void DrawSummary(Graphics g)
        {
            g.DrawString("DEMO COMPLETE — SUMMARY", _fontTitle, Brushes.Gold, 30, 20);
            int beatable = _demoResults.Count(r => r.IsBeatable);
            g.DrawString($"Beatable     : {beatable} / {_demoResults.Count}", _fontHead, Brushes.LimeGreen, 30, 72);
            g.DrawString($"Not Beatable : {_demoResults.Count - beatable} / {_demoResults.Count}", _fontHead, Brushes.OrangeRed, 30, 96);
            var bt = _demoResults.Where(r => r.IsBeatable).Select(r => r.TimeToComplete).ToList();
            if (bt.Count > 0)
                g.DrawString($"Avg completion time: {bt.Average():F1}s", _fontBody, Brushes.Cyan, 30, 122);

            int y = 155;
            foreach (var r in _demoResults.Take((H - y - 80) / 18))
            {
                string icon = r.IsBeatable ? "✅" : "❌";
                g.DrawString($"{icon} {r.LevelName,-28} {r.TimeToComplete:F1}s  I:{r.ItemsCollected}  K:{r.EnemiesDefeated}",
                    _fontSmall, r.IsBeatable ? Brushes.LimeGreen : Brushes.OrangeRed, 30, y);
                y += 18;
            }
            g.DrawString("[ENTER / ESC] Back to main menu", _fontBody, Brushes.Yellow, 30, H - 50);
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private static void DrawBtn(Graphics g, Rectangle r, string label, Color bg)
        {
            using (var br = new SolidBrush(bg))
                g.FillRectangle(br, r);
            using (var pen = new Pen(Color.White, 2))
                g.DrawRectangle(pen, r);
            using (var f = new Font("Courier New", 12, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(label, f);
                g.DrawString(label, f, Brushes.White,
                    r.X + (r.Width  - sz.Width)  / 2f,
                    r.Y + (r.Height - sz.Height) / 2f);
            }
        }

        public override void HandleClick(System.Drawing.Point p)
        {
            switch (_state)
            {
                case DemoState.Menu:
                    if (_btnStart.Contains(p)) StartDemo();
                    if (_btnBack.Contains(p))  Game.Instance.Scenes.Pop();
                    break;
                case DemoState.LevelResult:
                    AdvanceAfterResult();
                    break;
                case DemoState.Summary:
                    Game.Instance.Scenes.Pop();
                    break;
            }
        }
    }
}
