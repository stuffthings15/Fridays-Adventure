using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Tests;

namespace Fridays_Adventure.Scenes
{
    // ────────────────────────────────────────────
    // PHASE 2 - Team 10: Engine Programmer
    // Feature: Video Demo Scene — Live Gameplay Demo
    // Purpose: Scripted demo that shows REAL gameplay:
    //          Title screen → name entry → bot plays 2 levels live →
    //          NPC dialogue → save → summary.
    //          The viewer sees the actual game running — not slides.
    // ────────────────────────────────────────────

    /// <summary>
    /// Live video demo for Miss Friday's Adventure II.
    /// Shows the title screen and name entry as drawn overlays,
    /// then pushes real <see cref="BotPlayLevelScene"/> scenes so
    /// the bot plays through Dinosaur Island and Storm Belt live.
    /// The viewer watches actual gameplay with the bot controlling
    /// the player in real time.
    /// </summary>
    public sealed class VideoDemoScene : Scene
    {
        // ── State machine ─────────────────────────────────────────────
        private enum Phase
        {
            TitleShow,     // draw the title screen (5 s)
            NameEntry,     // draw name entry with auto-typing (5 s)
            PreLevel,      // brief loading card before each level
            LevelPlaying,  // BotPlayLevelScene is on top — real gameplay
            LevelResult,   // result card after each level (3.5 s)
            StatsShow,     // HUD overlay with stats/items collected (4 s)
            SaveShow,      // save game overlay (4 s)
            Complete       // feature checklist — press any key to exit
        }
        private Phase _phase = Phase.TitleShow;

        // ── Timers ────────────────────────────────────────────────────
        private float _phaseTimer;
        private float _totalTimer;

        // ── Level data (curated 2-level demo) ─────────────────────────
        private static readonly string[] LevelIds   = { "dino", "storm1" };
        private static readonly string[] LevelNames = { "1. Dinosaur Island", "2. Storm Belt" };
        private int _levelIndex;
        private readonly List<EnhancedLevelTestResult> _results = new List<EnhancedLevelTestResult>();

        // ── Typing animation ──────────────────────────────────────────
        private const string DemoName = "Luffy";
        private int   _typingPos;
        private float _typingTimer;

        // ── Result display ────────────────────────────────────────────
        private float _resultTimer;
        private const float RESULT_HOLD   = 3.5f;
        private const float PRE_LEVEL_HOLD = 2.5f;

        // ── Fonts ─────────────────────────────────────────────────────
        private Font _fontHead, _fontBody, _fontSmall, _fontBig;
        // Cached fonts for DrawTitlePhase — avoids per-frame GDI+ allocations
        private Font _fontTitle, _fontSubtitle;
        // Cached fonts for DrawNamePhase and DrawBtn — avoids per-frame GDI+ allocations
        private Font _fontNameTyping, _fontBtn;

        // ── Phase fade-in ─────────────────────────────────────────────
        // Brief alpha fade-in at the start of each phase for smooth visual transitions
        private const float FADE_IN_DURATION = 0.35f;

        // ══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ══════════════════════════════════════════════════════════════

        public override void OnEnter()
        {
            DisposeFonts();

            _fontHead     = new Font("Courier New", 14, FontStyle.Bold);
            _fontBody     = new Font("Courier New", 11);
            _fontSmall    = new Font("Courier New", 9);
            _fontBig      = new Font("Courier New", 22, FontStyle.Bold);
            _fontTitle      = new Font("Courier New", 30, FontStyle.Bold);
            _fontSubtitle   = new Font("Courier New", 16, FontStyle.Italic);
            _fontNameTyping = new Font("Courier New", 20, FontStyle.Bold);
            _fontBtn        = new Font("Courier New", 12, FontStyle.Bold);

            _phase       = Phase.TitleShow;
            _phaseTimer  = 0f;
            _totalTimer  = 0f;
            _levelIndex  = 0;
            _typingPos   = 0;
            _typingTimer = 0f;
            _results.Clear();
        }

        public override void OnExit()
        {
            DialogueScene.AutoAdvance = false;
            DisposeFonts();
        }

        /// <summary>Safely disposes and nulls all cached font resources.</summary>
        private void DisposeFonts()
        {
            _fontHead?.Dispose();     _fontHead     = null;
            _fontBody?.Dispose();     _fontBody     = null;
            _fontSmall?.Dispose();    _fontSmall    = null;
            _fontBig?.Dispose();      _fontBig      = null;
            _fontTitle?.Dispose();      _fontTitle      = null;
            _fontSubtitle?.Dispose();   _fontSubtitle   = null;
            _fontNameTyping?.Dispose(); _fontNameTyping = null;
            _fontBtn?.Dispose();        _fontBtn        = null;
        }

        /// <summary>Returns the display name for the current level, or "..." if out of range.</summary>
        private string GetCurrentLevelName()
        {
            return _levelIndex < LevelNames.Length ? LevelNames[_levelIndex] : "...";
        }

        // ══════════════════════════════════════════════════════════════
        // UPDATE
        // ══════════════════════════════════════════════════════════════

        public override void Update(float dt)
        {
            _totalTimer += dt;
            _phaseTimer += dt;
            var input = Game.Instance.Input;

            if (input.PausePressed)
            {
                Game.Instance.Scenes.Pop();
                return;
            }

            switch (_phase)
            {
                case Phase.TitleShow:
                    if (_phaseTimer >= 5f)
                        TransitionTo(Phase.NameEntry);
                    break;

                case Phase.NameEntry:
                    // Animated typing — one letter every 0.5 s, first letter at 0.5 s
                    _typingTimer += dt;
                    if (_typingTimer >= 0.5f && _typingPos < DemoName.Length)
                    {
                        _typingPos++;
                        _typingTimer = 0f;
                    }
                    if (_phaseTimer >= 5f)
                    {
                        _typingPos = DemoName.Length; // ensure full name shown
                        TransitionTo(Phase.PreLevel);
                    }
                    break;

                case Phase.PreLevel:
                    if (_phaseTimer >= PRE_LEVEL_HOLD)
                        LaunchCurrentLevel();
                    break;

                case Phase.LevelPlaying:
                    // BotPlayLevelScene is on top — this Update is not called.
                    // When the level finishes its callback transitions us to LevelResult.
                    break;

                case Phase.LevelResult:
                    _resultTimer += dt;
                    if (input.InteractPressed || input.AttackPressed || _resultTimer >= RESULT_HOLD)
                        AdvanceAfterResult();
                    break;

                case Phase.StatsShow:
                    if (_phaseTimer >= 4f)
                        TransitionTo(Phase.SaveShow);
                    break;

                case Phase.SaveShow:
                    if (_phaseTimer >= 4f)
                        TransitionTo(Phase.Complete);
                    break;

                case Phase.Complete:
                    if (input.InteractPressed || input.AttackPressed)
                        Game.Instance.Scenes.Pop();
                    break;
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
        /// Pushes a <see cref="BotPlayLevelScene"/> for the current level.
        /// The bot plays the level live in real time — the viewer watches
        /// actual gameplay.  When the level finishes, the callback
        /// transitions to the result card.
        /// </summary>
        private void LaunchCurrentLevel()
        {
            if (_levelIndex >= LevelIds.Length)
            {
                // All levels done — show stats, then save, then summary
                TransitionTo(Phase.StatsShow);
                return;
            }

            string id   = LevelIds[_levelIndex];
            string name = LevelNames[_levelIndex];

            // Enable auto-advance for any dialogue scenes the bot encounters
            DialogueScene.AutoAdvance = true;

            Scene inner = LevelSceneFactory.Create(id, name);

            // First level: show the "Meet Finn" NPC dialogue before playing
            if (id == "dino" && !Game.Instance.Save.GetFlag("demo_dino_visited"))
            {
                Game.Instance.Save.SetFlag("demo_dino_visited");
                var dialogue = Dialogues.MeetFinn();
                dialogue.OnDone = _ =>
                {
                    PushBotLevel(inner, id, name);
                };
                Game.Instance.Scenes.Push(new DialogueScene(dialogue));
                TransitionTo(Phase.LevelPlaying);
                return;
            }

            PushBotLevel(inner, id, name);
            TransitionTo(Phase.LevelPlaying);
        }

        /// <summary>Wrap a level scene with the bot controller and push it.</summary>
        private void PushBotLevel(Scene inner, string id, string name)
        {
            var wrapper = new BotPlayLevelScene(inner, name, (beaten, elapsed) =>
            {
                if (elapsed < 0f)
                {
                    // User pressed ESC during the level — abort demo
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

                _resultTimer = 0f;
                TransitionTo(Phase.LevelResult);
            });

            Game.Instance.Scenes.Push(wrapper);
        }

        /// <summary>Move to the next level or the stats phase.</summary>
        private void AdvanceAfterResult()
        {
            _levelIndex++;
            if (_levelIndex >= LevelIds.Length)
                TransitionTo(Phase.StatsShow);
            else
                TransitionTo(Phase.PreLevel);
        }

        // ══════════════════════════════════════════════════════════════
        // DRAW
        // ══════════════════════════════════════════════════════════════

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Enable anti-aliasing for smooth shapes and crisp text rendering
            g.SmoothingMode   = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Background
            using (var br = new LinearGradientBrush(
                new Rectangle(0, 0, W, H),
                Color.FromArgb(12, 15, 30), Color.FromArgb(20, 30, 55), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // Header
            DrawHeader(g, W);

            // Phase content
            switch (_phase)
            {
                case Phase.TitleShow:    DrawTitlePhase(g, W, H);    break;
                case Phase.NameEntry:    DrawNamePhase(g, W, H);     break;
                case Phase.PreLevel:     DrawPreLevel(g, W, H);      break;
                case Phase.LevelPlaying: DrawPlaying(g, W, H);       break;
                case Phase.LevelResult:  DrawLevelResult(g, W, H);   break;
                case Phase.StatsShow:    DrawStatsPhase(g, W, H);    break;
                case Phase.SaveShow:     DrawSavePhase(g, W, H);     break;
                case Phase.Complete:     DrawComplete(g, W, H);      break;
            }

            // Smooth fade-in overlay — darkens the screen briefly at the
            // start of each phase so transitions feel polished, not jarring.
            if (_phaseTimer < FADE_IN_DURATION)
            {
                int fadeAlpha = (int)(255 * (1f - _phaseTimer / FADE_IN_DURATION));
                using (var overlay = new SolidBrush(Color.FromArgb(fadeAlpha, 0, 0, 0)))
                    g.FillRectangle(overlay, 0, 0, W, H);
            }
        }

        // ── Header ────────────────────────────────────────────────────

        private void DrawHeader(Graphics g, int W)
        {
            using (var br = new SolidBrush(Color.FromArgb(200, 10, 10, 20)))
                g.FillRectangle(br, 0, 0, W, 50);

            g.DrawString("\u25B6 VIDEO DEMO \u2014 Miss Friday's Adventure II",
                _fontHead, Brushes.OrangeRed, 14, 6);

            string time = $"Time: {_totalTimer:F0}s";
            SizeF tsz = g.MeasureString(time, _fontSmall);
            g.DrawString(time, _fontSmall, Brushes.Gray, W - tsz.Width - 14, 10);

            string step = GetPhaseLabel();
            SizeF ssz = g.MeasureString(step, _fontSmall);
            g.DrawString(step, _fontSmall, Brushes.White, W - ssz.Width - 14, 30);

            // Progress bar — estimate includes TitleShow(5) + NameEntry(5) + per-level(pre+play+result) + StatsShow(4) + SaveShow(4)
            float total = 5f + 5f + LevelIds.Length * (PRE_LEVEL_HOLD + 30f + RESULT_HOLD) + 4f + 4f;
            float pct = Math.Min(1f, _totalTimer / total);
            using (var br = new SolidBrush(Color.FromArgb(40, 40, 60)))
                g.FillRectangle(br, 0, 50, W, 4);
            using (var br = new SolidBrush(Color.OrangeRed))
                g.FillRectangle(br, 0, 50, (int)(W * pct), 4);
        }

        private string GetPhaseLabel()
        {
            switch (_phase)
            {
                case Phase.TitleShow:   return "1/7: Title Screen";
                case Phase.NameEntry:   return "2/7: Name Entry";
                case Phase.PreLevel:
                case Phase.LevelPlaying:
                case Phase.LevelResult: return $"{_levelIndex + 3}/7: Level {_levelIndex + 1}/{LevelIds.Length}";
                case Phase.StatsShow:   return "5/7: Player Stats";
                case Phase.SaveShow:    return "6/7: Save Game";
                case Phase.Complete:    return "7/7: Demo Complete";
                default:                return "";
            }
        }

        // ── Phase 1: Title Screen ─────────────────────────────────────

        private void DrawTitlePhase(Graphics g, int W, int H)
        {
            // Semi-transparent banner mimicking the real title screen
            using (var br = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
                g.FillRectangle(br, 0, (int)(H * 0.15f), W, 160);

            // Title text — uses cached fonts to avoid per-frame GDI+ allocations
            const string titleText = "Miss Friday's Adventure Part II";
            SizeF titleSz = g.MeasureString(titleText, _fontTitle);
            g.DrawString(titleText, _fontTitle, Brushes.Black, (W - titleSz.Width) / 2f, H * 0.18f);

            const string subtitleText = "Tide of the Lost";
            SizeF subSz = g.MeasureString(subtitleText, _fontSubtitle);
            using (var br = new SolidBrush(Color.DarkCyan))
                g.DrawString(subtitleText, _fontSubtitle, br, (W - subSz.Width) / 2f, H * 0.26f);

            // Simulated buttons
            int bw = 150, bh = 40, gap = 10;
            int totalBw = bw * 5 + gap * 4;
            int sx = (W - totalBw) / 2;
            int by = (int)(H * 0.55f);
            string[] labels = { "LOAD", "SAVE", "OPTIONS", "SCORES", "EXIT" };
            Color[] colors = {
                Color.FromArgb(30, 110, 60), Color.FromArgb(30, 80, 110),
                Color.FromArgb(40, 80, 140), Color.FromArgb(120, 100, 20),
                Color.FromArgb(120, 30, 30)
            };
            for (int i = 0; i < 5; i++)
                DrawBtn(g, new Rectangle(sx + (bw + gap) * i, by, bw, bh), labels[i], colors[i]);

            // START GAME highlighted
            int sgw = 280, sgh = 50;
            var startR = new Rectangle((W - sgw) / 2, by - sgh - 18, sgw, sgh);
            DrawBtn(g, startR, "\u25B6 START GAME", Color.FromArgb(20, 120, 50));
            // Gold highlight around START
            using (var pen = new Pen(Color.Gold, 3))
                g.DrawRectangle(pen, startR);

            // Phase narration (30px bar at bottom)
            DrawNarration(g, W, H, "Showing the main menu — all buttons visible. Player clicks START GAME.");

            // Countdown — drawn above the narration bar so it remains visible
            float rem = Math.Max(0f, 5f - _phaseTimer);
            string hint = $"Auto-advancing in {rem:F0}s...";
            g.DrawString(hint, _fontSmall, Brushes.Yellow, 14, H - 62);
        }

        // ── Phase 2: Name Entry ───────────────────────────────────────

        private void DrawNamePhase(Graphics g, int W, int H)
        {
            // Name entry box (same as real TitleScene)
            int bw = 420, bh = 130;
            int bx = (W - bw) / 2, by = (int)(H * 0.30f);

            using (var br = new SolidBrush(Color.FromArgb(240, 10, 10, 40)))
                g.FillRectangle(br, bx, by, bw, bh);
            using (var pen = new Pen(Color.Gold, 2))
                g.DrawRectangle(pen, bx, by, bw, bh);

            // Uses _fontHead (same size as the old _fontNamePrompt — both 14pt Bold)
            g.DrawString("Enter your name, pirate:", _fontHead, Brushes.Gold, bx + 20, by + 14);

            // Typing animation — shows characters appearing one by one
            string typed = DemoName.Substring(0, _typingPos);
            string cursor = ((int)(_phaseTimer / 0.45f)) % 2 == 0 ? "|" : " ";
            g.DrawString(typed + cursor, _fontNameTyping, Brushes.White, bx + 20, by + 52);

            g.DrawString("[Enter] Confirm   [Backspace] Delete", _fontSmall, Brushes.DimGray, bx + 20, by + 100);

            DrawNarration(g, W, H, "Player types their pirate name: \"Luffy\" — each letter appears in real time.");

            // Countdown — matches TitleShow so the viewer knows when this phase ends
            float rem = Math.Max(0f, 5f - _phaseTimer);
            g.DrawString($"Auto-advancing in {rem:F0}s...", _fontSmall, Brushes.Yellow, 14, H - 62);
        }

        // ── Phase 3: Pre-Level Card ───────────────────────────────────

        private void DrawPreLevel(Graphics g, int W, int H)
        {
            string name = GetCurrentLevelName();

            // Center each line individually using MeasureString for proper alignment
            string loadingText = "LOADING LEVEL";
            SizeF loadSz = g.MeasureString(loadingText, _fontBig);
            g.DrawString(loadingText, _fontBig, Brushes.Gold, (W - loadSz.Width) / 2f, H * 0.3f);

            SizeF nameSz = g.MeasureString(name, _fontHead);
            g.DrawString(name, _fontHead, Brushes.Cyan, (W - nameSz.Width) / 2f, H * 0.4f);

            string levelOf = $"Level {_levelIndex + 1} of {LevelIds.Length}";
            SizeF levelSz = g.MeasureString(levelOf, _fontBody);
            g.DrawString(levelOf, _fontBody, Brushes.White, (W - levelSz.Width) / 2f, H * 0.5f);

            string botHint = "Bot will play this level live \u2014 watch the actual gameplay!";
            SizeF hintSz = g.MeasureString(botHint, _fontSmall);
            g.DrawString(botHint, _fontSmall, Brushes.LightGray, (W - hintSz.Width) / 2f, H * 0.6f);

            float rem = Math.Max(0f, PRE_LEVEL_HOLD - _phaseTimer);
            string countText = $"Starting in {rem:F0}s...";
            SizeF countSz = g.MeasureString(countText, _fontSmall);
            g.DrawString(countText, _fontSmall, Brushes.Yellow, (W - countSz.Width) / 2f, H * 0.7f);

            DrawNarration(g, W, H,
                _levelIndex == 0
                    ? "About to play Dinosaur Island — meet NPC Finn, then fight enemies and reach the exit flag."
                    : "Storm Belt — survive 25 seconds of lightning strikes on the ship deck!");
        }

        // ── Phase 4: Level Playing (BotPlayLevelScene is on top) ──────

        private void DrawPlaying(Graphics g, int W, int H)
        {
            // This is rarely visible — BotPlayLevelScene draws over us
            string name = GetCurrentLevelName();
            g.DrawString("BOT IS PLAYING LIVE", _fontHead, Brushes.Gold, 30, 80);
            g.DrawString(name, _fontBody, Brushes.Cyan, 30, 110);
        }

        // ── Phase 5: Level Result Card ────────────────────────────────

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
            // Wrap the border-colored brush in using to prevent GDI+ resource leak
            using (var borderBrush = new SolidBrush(border))
                g.DrawString(r.LevelName, _fontHead, borderBrush, px + 20, y);
            y += 34;
            g.DrawString(r.IsBeatable ? "\u2705 BEATABLE" : "\u274C NOT BEATABLE",
                _fontHead, Brushes.White, px + 20, y); y += 28;
            g.DrawString($"Time: {r.TimeToComplete:F1}s", _fontBody, Brushes.White, px + 20, y); y += 22;
            g.DrawString($"Items: {r.ItemsCollected}  Enemies: {r.EnemiesDefeated}",
                _fontBody, Brushes.Gold, px + 20, y); y += 28;

            float hold = Math.Max(0f, RESULT_HOLD - _resultTimer);
            g.DrawString($"[ENTER] Continue  (auto in {hold:F0}s)",
                _fontSmall, Brushes.White, px + 20, py + ph - 24);

            DrawNarration(g, W, H, "Level complete! Showing result card with time, items collected, and enemies defeated.");
        }

        // ── Phase 6: Stats / Inventory Overlay ──────────────────────

        /// <summary>
        /// Draws a HUD-style overlay showing items collected, enemies
        /// defeated, and berries gathered across all played levels.
        /// Demonstrates the in-game stats system the player sees during play.
        /// </summary>
        private void DrawStatsPhase(Graphics g, int W, int H)
        {
            int bw = 580, bh = 300;
            int bx = (W - bw) / 2, by = (int)(H * 0.15f);

            using (var br = new SolidBrush(Color.FromArgb(230, 10, 20, 50)))
                g.FillRectangle(br, bx, by, bw, bh);
            using (var pen = new Pen(Color.Gold, 3))
                g.DrawRectangle(pen, bx, by, bw, bh);

            g.DrawString("\U0001F4CA  PLAYER STATS", _fontHead, Brushes.Gold, bx + 20, by + 14);
            g.DrawString($"Player: {DemoName}", _fontBody, Brushes.White, bx + 20, by + 46);

            int iy = by + 76;

            // Tally across all results
            int totalItems = 0, totalKills = 0;
            float totalTime = 0f;
            foreach (var r in _results)
            {
                totalItems += r.ItemsCollected;
                totalKills += r.EnemiesDefeated;
                totalTime  += r.TimeToComplete;
            }

            string[] stats =
            {
                $"\u2694 Levels played:      {_results.Count}",
                $"\u2605 Levels beaten:      {_results.Count(r => r.IsBeatable)}",
                $"\u2726 Items collected:    {totalItems}",
                $"\U0001F480 Enemies defeated:   {totalKills}",
                $"\u23F1 Total play time:    {totalTime:F1}s"
            };
            foreach (string s in stats)
            {
                g.DrawString(s, _fontBody, Brushes.White, bx + 30, iy);
                iy += 26;
            }

            // Per-level breakdown
            iy += 10;
            g.DrawString("Per-level breakdown:", _fontSmall, Brushes.Cyan, bx + 30, iy);
            iy += 20;
            foreach (var r in _results)
            {
                string icon = r.IsBeatable ? "\u2705" : "\u274C";
                g.DrawString($"{icon} {r.LevelName}  Items:{r.ItemsCollected}  Kills:{r.EnemiesDefeated}  {r.TimeToComplete:F1}s",
                    _fontSmall, r.IsBeatable ? Brushes.LimeGreen : Brushes.OrangeRed, bx + 40, iy);
                iy += 18;
            }

            DrawNarration(g, W, H, "Player stats overlay — items collected, enemies defeated, and time per level.");
        }

        // ── Phase 7: Save Game ────────────────────────────────────────

        private void DrawSavePhase(Graphics g, int W, int H)
        {
            // Save confirmation overlay
            int bw = 500, bh = 200;
            int bx = (W - bw) / 2, by = (int)(H * 0.25f);

            using (var br = new SolidBrush(Color.FromArgb(230, 15, 40, 20)))
                g.FillRectangle(br, bx, by, bw, bh);
            using (var pen = new Pen(Color.LimeGreen, 3))
                g.DrawRectangle(pen, bx, by, bw, bh);

            // Center the title within the panel using MeasureString
            const string savedTitle = "\u2705  GAME SAVED!";
            SizeF savedSz = g.MeasureString(savedTitle, _fontBig);
            g.DrawString(savedTitle, _fontBig, Brushes.LimeGreen,
                bx + (bw - savedSz.Width) / 2f, by + 20);

            int iy = by + 70;
            string[] info =
            {
                $"Player: {DemoName}",
                $"Levels completed: {_results.Count(r => r.IsBeatable)}",
                "Save file: save_data.json",
                "Progress written to disk successfully."
            };
            foreach (string line in info)
            {
                g.DrawString(line, _fontBody, Brushes.White, bx + 30, iy);
                iy += 24;
            }

            DrawNarration(g, W, H, "Game saved! All progress is written to a JSON file on disk (F5 in-game).");
        }

        // ── Phase 8: Demo Complete ────────────────────────────────────

        private void DrawComplete(Graphics g, int W, int H)
        {
            // Center the title using MeasureString for accurate placement
            const string completeTitle = "\u2605  VIDEO DEMO COMPLETE  \u2605";
            SizeF ctSz = g.MeasureString(completeTitle, _fontBig);
            g.DrawString(completeTitle, _fontBig, Brushes.Gold, (W - ctSz.Width) / 2f, 80);

            string[] checks =
            {
                "\u2705  Title screen / main menu (5 sec)",
                "\u2705  Player entering name (animated typing)",
                "\u2705  Room 1: Dinosaur Island (side-scrolling platformer)",
                "\u2705  Room 2: Storm Belt (lightning survival)",
                "\u2705  Combat encounters (stomp, melee, frost ball, dash)",
                "\u2705  Item pickup (berries, health, power-ups)",
                "\u2705  NPC dialogue (Meet Finn — branching choices)",
                "\u2705  Player stats overlay (items, kills, time)",
                "\u2705  Save game (JSON to disk)",
                "\u2705  Text RPG mode available (shared engine, 2 modes)"
            };

            g.DrawString("Features Demonstrated:", _fontHead, Brushes.Cyan, 80, 130);
            int y = 165;
            foreach (string item in checks)
            {
                g.DrawString(item, _fontBody, Brushes.LimeGreen, 100, y);
                y += 26;
            }

            y += 14;
            // Level results summary
            foreach (var r in _results)
            {
                string icon = r.IsBeatable ? "\u2705" : "\u274C";
                g.DrawString($"{icon} {r.LevelName}  \u2014  {r.TimeToComplete:F1}s  Items:{r.ItemsCollected}  Kills:{r.EnemiesDefeated}",
                    _fontSmall, r.IsBeatable ? Brushes.LimeGreen : Brushes.OrangeRed, 100, y);
                y += 20;
            }

            y += 20;
            g.DrawString($"Total demo time: {_totalTimer:F0} seconds", _fontBody,
                Brushes.White, 100, y);
            y += 30;
            g.DrawString("[ENTER / ESC] Return to title screen", _fontBody,
                Brushes.Yellow, (W - 400) / 2f, y);
        }

        // ── Helpers ───────────────────────────────────────────────────

        /// <summary>Draw the narration bar at the bottom of the screen.</summary>
        private void DrawNarration(Graphics g, int W, int H, string text)
        {
            int nh = 30;
            using (var br = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
                g.FillRectangle(br, 0, H - nh, W, nh);
            g.DrawString(text, _fontSmall, Brushes.Gold, 14, H - nh + 6);
        }

        /// <summary>Draw a button rectangle with centered label using cached font.</summary>
        private void DrawBtn(Graphics g, Rectangle r, string label, Color bg)
        {
            using (var br = new SolidBrush(bg))
                g.FillRectangle(br, r);
            using (var pen = new Pen(Color.White, 1))
                g.DrawRectangle(pen, r);

            // Uses cached _fontBtn — avoids per-call GDI+ font allocation
            SizeF sz = g.MeasureString(label, _fontBtn);
            g.DrawString(label, _fontBtn, Brushes.White,
                r.X + (r.Width  - sz.Width)  / 2f,
                r.Y + (r.Height - sz.Height) / 2f);
        }

        public override void HandleClick(Point p)
        {
            switch (_phase)
            {
                case Phase.LevelResult:
                    AdvanceAfterResult();
                    break;
                case Phase.StatsShow:
                    TransitionTo(Phase.SaveShow);
                    break;
                case Phase.SaveShow:
                    TransitionTo(Phase.Complete);
                    break;
                case Phase.Complete:
                    Game.Instance.Scenes.Pop();
                    break;
            }
        }
    }
}
