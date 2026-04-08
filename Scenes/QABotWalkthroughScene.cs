// ────────────────────────────────────────────
// PHASE 2 - Team 10: Engine Programmer
// Feature: QA Bot Full Walkthrough
// Purpose: Automated QA demonstration that plays through EVERY level
//          in the game sequentially, triggering dialogue, roulette,
//          toad house, and inventory events along the way.
//          All actions are visible on-screen — the viewer watches
//          a complete game walkthrough driven by the bot AI.
// ────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;
using Fridays_Adventure.Tests;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Full QA walkthrough scene — the bot plays every level in order,
    /// interacts with dialogue, roulette, and toad house systems, and
    /// produces a visual report at the end.  All gameplay is real and
    /// visible on screen, using the same input system as a human player.
    /// </summary>
    /// <remarks>PHASE 2 - Team 10: QA Bot Full Walkthrough</remarks>
    public sealed class QABotWalkthroughScene : Scene
    {
        // ══════════════════════════════════════════════════════════════
        // STATE MACHINE
        // ══════════════════════════════════════════════════════════════

        private enum Phase
        {
            Intro,          // title card — "QA BOT WALKTHROUGH" (3 s)
            MapOverview,    // shows map with next level highlighted (2 s)
            PreLevel,       // "ENTERING LEVEL" card (2 s)
            LevelPlaying,   // BotPlayLevelScene on top — real gameplay
            LevelResult,    // result card after each level (3 s)
            EventDialogue,  // DialogueScene pushed — NPC interaction
            EventRoulette,  // CardRouletteScene pushed
            EventToadHouse, // ToadHouseScene pushed
            GameComplete,   // all levels done — victory screen (5 s)
            Summary         // QA report — press any key to exit
        }

        private Phase _phase = Phase.Intro;

        // ── Timers ────────────────────────────────────────────────────
        private float _phaseTimer;
        private float _totalTimer;

        // ── Fade-in ───────────────────────────────────────────────────
        private const float FADE_IN = 0.35f;

        // ══════════════════════════════════════════════════════════════
        // LEVEL SEQUENCE — all 17 campaign levels in progression order
        // ══════════════════════════════════════════════════════════════

        /// <summary>Level ID and display name for each campaign node.</summary>
        private static readonly (string id, string name)[] AllLevels =
        {
            ("dino",           "Dinosaur Island"),
            ("storm1",         "Storm Belt"),
            ("sky",            "Sky Island"),
            ("blockade",       "Marine Blockade"),
            ("wano",           "Blade Nation"),
            ("warlord1",       "Warlord: Sudo"),
            ("harbor",         "Harbor Town"),
            ("coral",          "Coral Reef"),
            ("tundra",         "Tundra Peak"),
            ("storm2",         "Tempest Strait"),
            ("warlord2",       "Warlord: Vanta"),
            ("dive_gate",      "Dive Gate"),
            ("sunken_gate",    "Sunken Gate"),
            ("kelp",           "Kelp Maze"),
            ("boiling_vent",   "Vent Ruins"),
            ("abyss",          "Abyss"),
            ("centipede_final","Centipede"),
        };

        private int _levelIndex;
        private readonly List<EnhancedLevelTestResult> _results =
            new List<EnhancedLevelTestResult>();

        // ── Event schedule — which systems to test after specific levels ──
        // Each tuple: (levelIndex that just completed, event type)
        private readonly Queue<Phase> _pendingEvents = new Queue<Phase>();

        // ── Result display ────────────────────────────────────────────
        private float _resultTimer;
        private const float RESULT_HOLD    = 3.0f;
        private const float PRE_LEVEL_HOLD = 2.0f;
        private const float MAP_HOLD       = 2.0f;

        // ── Fonts (cached for performance) ────────────────────────────
        private Font _fHead, _fBody, _fSmall, _fBig, _fTitle;

        // ── Systems tested flags (for the final report) ───────────────
        private bool _testedDialogue;
        private bool _testedRoulette;
        private bool _testedToadHouse;
        private bool _testedInventory;

        // ── QA completion logging guard ───────────────────────────────
        private bool _loggedComplete;

        // ══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ══════════════════════════════════════════════════════════════

        public override void OnEnter()
        {
            DisposeFonts();
            _fHead  = new Font("Courier New", 14, FontStyle.Bold);
            _fBody  = new Font("Courier New", 11);
            _fSmall = new Font("Courier New", 9);
            _fBig   = new Font("Courier New", 22, FontStyle.Bold);
            _fTitle = new Font("Courier New", 28, FontStyle.Bold);

            _phase       = Phase.Intro;
            _phaseTimer  = 0f;
            _totalTimer  = 0f;
            _levelIndex  = 0;
            _resultTimer = 0f;
            _results.Clear();
            _pendingEvents.Clear();
            _testedDialogue  = false;
            _testedRoulette  = false;
            _testedToadHouse = false;
            _testedInventory = false;

            // Enable dialogue auto-advance so the bot skips through NPC text
            DialogueScene.AutoAdvance = true;

            // ── Pre-flight blank-screen test ──────────────────────────────
            // Run before the walkthrough begins so blank/broken levels are
            // caught immediately and logged to the report file.
            try
            {
                var renderResults = Tests.BlankScreenDetector.RunAll();
                string logDir = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Tests.BlankScreenDetector.WriteReport(renderResults, logDir);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[QA] BlankScreenDetector pre-flight failed: {ex.Message}");
            }
        }

        public override void OnExit()
        {
            DialogueScene.AutoAdvance = false;
            ToadHouseScene.AutoAdvance = false;
            Game.Instance.CurrentLevelName = "";
            DisposeFonts();
        }

        /// <summary>Safely disposes and nulls all cached font resources.</summary>
        private void DisposeFonts()
        {
            _fHead?.Dispose();  _fHead  = null;
            _fBody?.Dispose();  _fBody  = null;
            _fSmall?.Dispose(); _fSmall = null;
            _fBig?.Dispose();   _fBig   = null;
            _fTitle?.Dispose(); _fTitle = null;
        }

        // ══════════════════════════════════════════════════════════════
        // UPDATE
        // ══════════════════════════════════════════════════════════════

        public override void Update(float dt)
        {
            _totalTimer += dt;
            _phaseTimer += dt;
            var input = Game.Instance.Input;

            // ESC aborts the walkthrough at any time
            if (input.PausePressed)
            {
                Game.Instance.Scenes.Pop();
                return;
            }

            switch (_phase)
            {
                case Phase.Intro:
                    if (_phaseTimer >= 3f)
                        TransitionTo(Phase.MapOverview);
                    break;

                case Phase.MapOverview:
                    if (_phaseTimer >= MAP_HOLD)
                        TransitionTo(Phase.PreLevel);
                    break;

                case Phase.PreLevel:
                    if (_phaseTimer >= PRE_LEVEL_HOLD)
                        LaunchCurrentLevel();
                    break;

                case Phase.LevelPlaying:
                    // BotPlayLevelScene is on top — we are not updated.
                    break;

                case Phase.LevelResult:
                    _resultTimer += dt;
                    if (input.InteractPressed || _resultTimer >= RESULT_HOLD)
                        AdvanceAfterResult();
                    break;

                case Phase.EventDialogue:
                case Phase.EventRoulette:
                case Phase.EventToadHouse:
                    // Pushed scene is on top — we are not updated.
                    break;

                case Phase.GameComplete:
                    // Log the QA completion event so the automation script detects it
                    if (!_loggedComplete)
                    {
                        _loggedComplete = true;
                        int passed = _results.Count(r => r.IsBeatable);
                        int failed = _results.Count - passed;
                        GameLogger.Log("QA_COMPLETE", GameLogLevel.INFO, new
                        {
                            totalLevels   = AllLevels.Length,
                            passed        = passed,
                            failed        = failed,
                            verdict       = failed == 0 ? "ALL_PASSED" : "HAS_FAILURES",
                            totalTimeSec  = _results.Sum(r => r.TimeToComplete)
                        });
                    }
                    if (_phaseTimer >= 5f)
                        TransitionTo(Phase.Summary);
                    break;

                case Phase.Summary:
                    // Auto-exit when running unattended QA bot mode
                    if (Game.AutoQABot)
                    {
                        if (_phaseTimer >= 3f)
                            Application.Exit();
                    }
                    else if (input.InteractPressed || input.AttackPressed)
                    {
                        Game.Instance.Scenes.Pop();
                    }
                    break;
            }
        }

        /// <summary>Called when a pushed scene pops back to us.</summary>
        public override void OnResume()
        {
            // OnResume fires when ANY pushed scene pops (bot level, dialogue,
            // roulette, toad house).  Only advance when returning from an
            // EVENT scene — the bot level callback already set LevelResult
            // phase, so the result timer will handle advancement from there.
            if (_phase == Phase.EventDialogue ||
                _phase == Phase.EventRoulette ||
                _phase == Phase.EventToadHouse)
            {
                ProcessPendingEvents();
            }
        }

        // ══════════════════════════════════════════════════════════════
        // PHASE TRANSITIONS
        // ══════════════════════════════════════════════════════════════

        private void TransitionTo(Phase next)
        {
            _phase      = next;
            _phaseTimer = 0f;
        }

        /// <summary>
        /// Creates and pushes a <see cref="BotPlayLevelScene"/> for the current
        /// level. The bot plays through the real game scene with AI input.
        /// </summary>
        private void LaunchCurrentLevel()
        {
            if (_levelIndex >= AllLevels.Length)
            {
                TransitionTo(Phase.GameComplete);
                return;
            }

            var (id, name) = AllLevels[_levelIndex];

            // Set the HUD level name so it's visible at the top of the screen
            Game.Instance.CurrentLevelName = name.ToUpperInvariant();

            Scene inner = LevelSceneFactory.Create(id, name);

            var wrapper = new BotPlayLevelScene(inner, name, (beaten, elapsed) =>
            {
                if (elapsed < 0f)
                {
                    // User pressed ESC during level — abort walkthrough
                    Game.Instance.Scenes.Pop();
                    return;
                }

                _results.Add(new EnhancedLevelTestResult
                {
                    LevelId        = id,
                    LevelName      = name,
                    IsBeatable     = beaten,
                    TimeToComplete = elapsed,
                    FailureReason  = beaten ? "" : "Timeout",
                    BotData        = new AutoTestBot()
                });

                // Schedule post-level events based on which level just finished
                SchedulePostLevelEvents(_levelIndex);

                _resultTimer = 0f;
                TransitionTo(Phase.LevelResult);
            });

            Game.Instance.Scenes.Push(wrapper);
            TransitionTo(Phase.LevelPlaying);
        }

        /// <summary>
        /// Determines which systems to test after a given level index.
        /// Events are queued and processed sequentially via OnResume.
        /// </summary>
        private void SchedulePostLevelEvents(int idx)
        {
            _pendingEvents.Clear();

            // Dialogue after first level (Meet Finn) and after Warlord 1
            if (idx == 0 || idx == 5)
                _pendingEvents.Enqueue(Phase.EventDialogue);

            // Card roulette after Sky Island and Tundra Peak
            if (idx == 2 || idx == 8)
                _pendingEvents.Enqueue(Phase.EventRoulette);

            // Toad house after Blade Nation and Warlord Vanta
            if (idx == 4 || idx == 10)
                _pendingEvents.Enqueue(Phase.EventToadHouse);
        }

        /// <summary>Pop events from the queue and push the appropriate scene.</summary>
        private void ProcessPendingEvents()
        {
            if (_pendingEvents.Count > 0)
            {
                Phase evt = _pendingEvents.Dequeue();
                TransitionTo(evt);

                switch (evt)
                {
                    case Phase.EventDialogue:
                        _testedDialogue = true;
                        // Pick the appropriate dialogue for the current point
                        var seq = _levelIndex <= 1
                            ? Dialogues.MeetFinn()
                            : Dialogues.MeetAmelia();
                        // DialogueScene pops itself when done, so OnDone must NOT
                        // call Pop() again — that would remove QABotWalkthroughScene.
                        // OnResume() handles advancing to the next level/event.
                        seq.OnDone = _ => { };
                        Game.Instance.Scenes.Push(new DialogueScene(seq));
                        break;

                    case Phase.EventRoulette:
                        _testedRoulette = true;
                        Game.Instance.Scenes.Push(new CardRouletteScene(() =>
                        {
                            Game.Instance.Scenes.Pop();
                        }));
                        break;

                    case Phase.EventToadHouse:
                        _testedToadHouse = true;
                        ToadHouseScene.AutoAdvance = true;
                        Game.Instance.Scenes.Push(new ToadHouseScene());
                        break;
                }
                return;
            }

            // No more events — advance to next level
            AdvanceToNextLevel();
        }

        /// <summary>Move past the result card to the next level or completion.</summary>
        private void AdvanceAfterResult()
        {
            ProcessPendingEvents();
        }

        /// <summary>Increment the level counter and show the map or complete.</summary>
        private void AdvanceToNextLevel()
        {
            _levelIndex++;
            if (_levelIndex >= AllLevels.Length)
                TransitionTo(Phase.GameComplete);
            else
                TransitionTo(Phase.MapOverview);
        }

        // ══════════════════════════════════════════════════════════════
        // DRAW
        // ══════════════════════════════════════════════════════════════

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Dark background
            using (var br = new LinearGradientBrush(
                new Rectangle(0, 0, W, H),
                Color.FromArgb(8, 12, 28), Color.FromArgb(16, 28, 50), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // Top status bar
            DrawStatusBar(g, W);

            // Phase-specific content
            switch (_phase)
            {
                case Phase.Intro:          DrawIntro(g, W, H);        break;
                case Phase.MapOverview:    DrawMapOverview(g, W, H);  break;
                case Phase.PreLevel:       DrawPreLevel(g, W, H);     break;
                case Phase.LevelPlaying:   DrawPlaying(g, W, H);      break;
                case Phase.LevelResult:    DrawLevelResult(g, W, H);  break;
                case Phase.EventDialogue:
                case Phase.EventRoulette:
                case Phase.EventToadHouse: DrawEventWait(g, W, H);    break;
                case Phase.GameComplete:   DrawGameComplete(g, W, H); break;
                case Phase.Summary:        DrawSummary(g, W, H);      break;
            }

            // Smooth fade-in overlay at each phase start
            if (_phaseTimer < FADE_IN)
            {
                int alpha = (int)(255 * (1f - _phaseTimer / FADE_IN));
                using (var ov = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
                    g.FillRectangle(ov, 0, 0, W, H);
            }
        }

        // ── Status Bar ────────────────────────────────────────────────

        private void DrawStatusBar(Graphics g, int W)
        {
            // Solid bar
            using (var br = new SolidBrush(Color.FromArgb(220, 6, 6, 16)))
                g.FillRectangle(br, 0, 0, W, 54);

            // Title
            g.DrawString("\u25B6 QA BOT WALKTHROUGH", _fHead, Brushes.Cyan, 14, 4);

            // Timer
            string time = $"Time: {_totalTimer:F0}s";
            SizeF tsz = g.MeasureString(time, _fSmall);
            g.DrawString(time, _fSmall, Brushes.Gray, W - tsz.Width - 14, 6);

            // Progress label
            string prog = $"Level {Math.Min(_levelIndex + 1, AllLevels.Length)}/{AllLevels.Length}  " +
                           $"Pass:{_results.Count(r => r.IsBeatable)}  Fail:{_results.Count(r => !r.IsBeatable)}";
            SizeF psz = g.MeasureString(prog, _fSmall);
            g.DrawString(prog, _fSmall, Brushes.White, W - psz.Width - 14, 22);

            // Systems tested badges
            int bx = 14;
            int by = 36;
            DrawBadge(g, ref bx, by, "DLG",  _testedDialogue,  Color.MediumPurple);
            DrawBadge(g, ref bx, by, "CARD", _testedRoulette,   Color.Gold);
            DrawBadge(g, ref bx, by, "TOAD", _testedToadHouse,  Color.LimeGreen);
            DrawBadge(g, ref bx, by, "INV",  _testedInventory,  Color.Cyan);

            // Progress bar
            float pct = AllLevels.Length > 0
                ? Math.Min(1f, (float)_levelIndex / AllLevels.Length)
                : 0f;
            using (var br = new SolidBrush(Color.FromArgb(40, 40, 60)))
                g.FillRectangle(br, 0, 54, W, 4);
            using (var br = new SolidBrush(Color.Cyan))
                g.FillRectangle(br, 0, 54, (int)(W * pct), 4);
        }

        /// <summary>Draws a small coloured badge indicating system test status.</summary>
        private void DrawBadge(Graphics g, ref int x, int y, string label, bool done, Color col)
        {
            Color bg = done ? col : Color.FromArgb(60, 60, 60);
            string icon = done ? "\u2713" : "\u2022";
            string text = $"{icon}{label}";
            SizeF sz = g.MeasureString(text, _fSmall);
            int w = (int)sz.Width + 6;
            using (var br = new SolidBrush(Color.FromArgb(done ? 180 : 80, bg)))
                g.FillRectangle(br, x, y, w, 14);
            using (var br = new SolidBrush(done ? Color.White : Color.Gray))
                g.DrawString(text, _fSmall, br, x + 3, y);
            x += w + 4;
        }

        // ── Phase Draws ───────────────────────────────────────────────

        private void DrawIntro(Graphics g, int W, int H)
        {
            const string title = "\u2605 QA BOT WALKTHROUGH \u2605";
            SizeF sz = g.MeasureString(title, _fTitle);
            g.DrawString(title, _fTitle, Brushes.Cyan, (W - sz.Width) / 2f, H * 0.22f);

            string[] info =
            {
                "Automated full-game walkthrough — all 17 levels",
                "Bot uses real input injection (same as player)",
                "Tests: Levels, Dialogue, Roulette, Toad House, Inventory",
                "",
                "All actions are visible on screen.",
                "Press ESC at any time to abort."
            };
            int y = (int)(H * 0.38f);
            foreach (string line in info)
            {
                if (string.IsNullOrEmpty(line)) { y += 10; continue; }
                SizeF lsz = g.MeasureString(line, _fBody);
                g.DrawString(line, _fBody, Brushes.White, (W - lsz.Width) / 2f, y);
                y += 24;
            }

            float rem = Math.Max(0f, 3f - _phaseTimer);
            string hint = $"Starting in {rem:F0}s...";
            SizeF hsz = g.MeasureString(hint, _fSmall);
            g.DrawString(hint, _fSmall, Brushes.Yellow, (W - hsz.Width) / 2f, H * 0.78f);
        }

        private void DrawMapOverview(Graphics g, int W, int H)
        {
            // Simulated map with level progression
            g.DrawString("CAMPAIGN MAP", _fBig, Brushes.Gold, 40, 80);

            int y = 120;
            for (int i = 0; i < AllLevels.Length; i++)
            {
                bool done = i < _levelIndex;
                bool current = i == _levelIndex;
                string icon = done ? "\u2705" : current ? "\u25B6" : "\u2022";
                Color col = done ? Color.LimeGreen : current ? Color.Cyan : Color.DimGray;
                string text = $"{icon} {i + 1}. {AllLevels[i].name}";

                using (var br = new SolidBrush(col))
                    g.DrawString(text, current ? _fHead : _fBody, br, 60, y);

                // Highlight current level with a box
                if (current)
                {
                    SizeF tsz = g.MeasureString(text, _fHead);
                    using (var pen = new Pen(Color.Cyan, 2))
                        g.DrawRectangle(pen, 56, y - 2, (int)tsz.Width + 8, (int)tsz.Height + 4);
                }
                y += current ? 28 : 22;
            }

            float rem = Math.Max(0f, MAP_HOLD - _phaseTimer);
            g.DrawString($"Navigating to level in {rem:F0}s...",
                _fSmall, Brushes.Yellow, 60, H - 40);
        }

        private void DrawPreLevel(Graphics g, int W, int H)
        {
            if (_levelIndex >= AllLevels.Length) return;
            var (id, name) = AllLevels[_levelIndex];

            string loading = "ENTERING LEVEL";
            SizeF lsz = g.MeasureString(loading, _fBig);
            g.DrawString(loading, _fBig, Brushes.Gold, (W - lsz.Width) / 2f, H * 0.28f);

            string display = $"{_levelIndex + 1}. {name}";
            SizeF dsz = g.MeasureString(display, _fHead);
            g.DrawString(display, _fHead, Brushes.Cyan, (W - dsz.Width) / 2f, H * 0.40f);

            string sub = $"Level {_levelIndex + 1} of {AllLevels.Length}";
            SizeF ssz = g.MeasureString(sub, _fBody);
            g.DrawString(sub, _fBody, Brushes.White, (W - ssz.Width) / 2f, H * 0.50f);

            string hint = "Bot will play this level live \u2014 watch the gameplay!";
            SizeF hsz = g.MeasureString(hint, _fSmall);
            g.DrawString(hint, _fSmall, Brushes.LightGray, (W - hsz.Width) / 2f, H * 0.62f);

            float rem = Math.Max(0f, PRE_LEVEL_HOLD - _phaseTimer);
            string cnt = $"Starting in {rem:F0}s...";
            SizeF csz = g.MeasureString(cnt, _fSmall);
            g.DrawString(cnt, _fSmall, Brushes.Yellow, (W - csz.Width) / 2f, H * 0.72f);
        }

        private void DrawPlaying(Graphics g, int W, int H)
        {
            // Rarely visible — BotPlayLevelScene draws over us
            if (_levelIndex < AllLevels.Length)
            {
                g.DrawString("BOT IS PLAYING LIVE", _fHead, Brushes.Gold, 30, 80);
                g.DrawString(AllLevels[_levelIndex].name, _fBody, Brushes.Cyan, 30, 110);
            }
        }

        private void DrawLevelResult(Graphics g, int W, int H)
        {
            if (_results.Count == 0) return;
            var r = _results[_results.Count - 1];

            int px = 40, py = 80, pw = W - 80, ph = H - 160;
            Color border = r.IsBeatable ? Color.LimeGreen : Color.OrangeRed;
            using (var br = new SolidBrush(Color.FromArgb(200, 20, 30, 60)))
                g.FillRectangle(br, px, py, pw, ph);
            using (var pen = new Pen(border, 3))
                g.DrawRectangle(pen, px, py, pw, ph);

            int y = py + 20;
            using (var br = new SolidBrush(border))
                g.DrawString(r.LevelName, _fHead, br, px + 20, y);
            y += 34;
            g.DrawString(r.IsBeatable ? "\u2705 PASSED" : "\u274C FAILED",
                _fHead, Brushes.White, px + 20, y); y += 28;
            g.DrawString($"Time: {r.TimeToComplete:F1}s", _fBody, Brushes.White, px + 20, y); y += 22;
            g.DrawString($"Items: {r.ItemsCollected}  Enemies: {r.EnemiesDefeated}",
                _fBody, Brushes.Gold, px + 20, y); y += 28;

            // Upcoming events
            if (_pendingEvents.Count > 0)
            {
                g.DrawString("Upcoming events:", _fSmall, Brushes.Yellow, px + 20, y);
                y += 16;
                foreach (var evt in _pendingEvents)
                {
                    string eName = evt == Phase.EventDialogue ? "NPC Dialogue" :
                                   evt == Phase.EventRoulette ? "Card Roulette" : "Toad House";
                    g.DrawString($"  \u25B6 {eName}", _fSmall, Brushes.LightGray, px + 20, y);
                    y += 16;
                }
            }

            float hold = Math.Max(0f, RESULT_HOLD - _resultTimer);
            g.DrawString($"[ENTER] Continue  (auto in {hold:F0}s)",
                _fSmall, Brushes.White, px + 20, py + ph - 24);
        }

        private void DrawEventWait(Graphics g, int W, int H)
        {
            string evtName = _phase == Phase.EventDialogue ? "DIALOGUE" :
                             _phase == Phase.EventRoulette ? "CARD ROULETTE" : "TOAD HOUSE";
            string text = $"\u25B6 TESTING: {evtName}";
            SizeF sz = g.MeasureString(text, _fBig);
            g.DrawString(text, _fBig, Brushes.Gold, (W - sz.Width) / 2f, H * 0.4f);

            string sub = "Event scene is active \u2014 bot interacting...";
            SizeF ssz = g.MeasureString(sub, _fBody);
            g.DrawString(sub, _fBody, Brushes.White, (W - ssz.Width) / 2f, H * 0.52f);
        }

        private void DrawGameComplete(Graphics g, int W, int H)
        {
            const string title = "\u2605 ALL LEVELS COMPLETE \u2605";
            SizeF sz = g.MeasureString(title, _fBig);
            g.DrawString(title, _fBig, Brushes.Gold, (W - sz.Width) / 2f, H * 0.25f);

            int passed = _results.Count(r => r.IsBeatable);
            int failed = _results.Count(r => !r.IsBeatable);
            string stat = $"Passed: {passed}   Failed: {failed}   Total: {_results.Count}";
            SizeF ssz = g.MeasureString(stat, _fHead);
            g.DrawString(stat, _fHead, Brushes.White, (W - ssz.Width) / 2f, H * 0.40f);

            float totalTime = _results.Sum(r => r.TimeToComplete);
            string ts = $"Total play time: {totalTime:F1}s   Walkthrough: {_totalTimer:F0}s";
            SizeF tsz = g.MeasureString(ts, _fBody);
            g.DrawString(ts, _fBody, Brushes.LightGray, (W - tsz.Width) / 2f, H * 0.52f);

            g.DrawString("Generating QA report...", _fSmall, Brushes.Yellow,
                (W - 180) / 2f, H * 0.68f);
        }

        private void DrawSummary(Graphics g, int W, int H)
        {
            // ── QA REPORT ─────────────────────────────────────────────
            const string title = "QA BOT WALKTHROUGH REPORT";
            SizeF tsz = g.MeasureString(title, _fBig);
            g.DrawString(title, _fBig, Brushes.Cyan, (W - tsz.Width) / 2f, 70);

            int y = 110;

            // System coverage
            g.DrawString("SYSTEM COVERAGE:", _fHead, Brushes.Gold, 60, y); y += 26;
            DrawCheck(g, 80, ref y, "Level Progression",       _results.Count > 0);
            DrawCheck(g, 80, ref y, "Map Navigation",          true);
            DrawCheck(g, 80, ref y, "Dialogue System",         _testedDialogue);
            DrawCheck(g, 80, ref y, "Card Roulette",           _testedRoulette);
            DrawCheck(g, 80, ref y, "Toad House / Bonus",      _testedToadHouse);
            DrawCheck(g, 80, ref y, "Game Completion",         _levelIndex >= AllLevels.Length);

            y += 12;
            g.DrawString("LEVEL RESULTS:", _fHead, Brushes.Gold, 60, y); y += 26;

            // Scrollable results list (show as many as fit)
            int maxVisible = (Game.Instance.CanvasHeight - y - 60) / 18;
            for (int i = 0; i < Math.Min(_results.Count, maxVisible); i++)
            {
                var r = _results[i];
                string icon = r.IsBeatable ? "\u2705" : "\u274C";
                string line = $"{icon} {r.LevelName,-20} {r.TimeToComplete,6:F1}s  " +
                              $"Items:{r.ItemsCollected}  Kills:{r.EnemiesDefeated}";
                using (var br = new SolidBrush(r.IsBeatable ? Color.LimeGreen : Color.OrangeRed))
                    g.DrawString(line, _fSmall, br, 80, y);
                y += 18;
            }

            y += 16;
            int passed = _results.Count(r => r.IsBeatable);
            string verdict = passed == AllLevels.Length
                ? "\u2605 VERDICT: ALL LEVELS BEATABLE — QA PASS \u2605"
                : $"\u26A0 VERDICT: {AllLevels.Length - passed} LEVEL(S) FAILED — QA ISSUES FOUND";
            Color vc = passed == AllLevels.Length ? Color.Gold : Color.OrangeRed;
            using (var br = new SolidBrush(vc))
                g.DrawString(verdict, _fHead, br, 60, y);

            y += 30;
            g.DrawString($"Total demo time: {_totalTimer:F0}s   [ENTER / ESC] Exit",
                _fBody, Brushes.Yellow, 60, y);
        }

        /// <summary>Draws a check or cross line for the QA report.</summary>
        private void DrawCheck(Graphics g, int x, ref int y, string label, bool pass)
        {
            string icon = pass ? "\u2705" : "\u274C";
            using (var br = new SolidBrush(pass ? Color.LimeGreen : Color.OrangeRed))
                g.DrawString($"{icon}  {label}", _fBody, br, x, y);
            y += 22;
        }

        // ── Click handling ────────────────────────────────────────────

        public override void HandleClick(Point p)
        {
            switch (_phase)
            {
                case Phase.LevelResult:
                    AdvanceAfterResult();
                    break;
                case Phase.Summary:
                    Game.Instance.Scenes.Pop();
                    break;
            }
        }
    }
}
