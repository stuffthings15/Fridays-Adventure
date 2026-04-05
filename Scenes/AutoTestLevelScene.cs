using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Fridays_Adventure.Tests;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// PHASE 2 - Team 10: Engine Programmer
    /// Feature: Automated Level Beatability Test Scene
    /// Purpose: In-game scene for running automated bot-driven level tests
    /// </summary>
    public sealed class AutoTestLevelScene : Scene
    {
        private bool _testRunning = false;
        private List<LevelAutoTestResult> _results = new List<LevelAutoTestResult>();
        private List<EnhancedLevelTestResult> _enhancedResults = new List<EnhancedLevelTestResult>();
        private int _currentDisplayIndex = 0;
        private float _displayTimer = 0f;
        private const float DISPLAY_DURATION = 2f; // Show each result for 2 seconds
        private int _canvasWidth = 800;
        private int _canvasHeight = 600;

        private Font _titleFont;
        private Font _headFont;
        private Font _bodyFont;
        private Font _smallFont;
        private Font _buttonFont;

        // Button rectangles for clickable areas
        private Rectangle _startButtonRect;
        private Rectangle _rerunButtonRect;
        private Rectangle _backButtonRect;
        private Rectangle _visualTestButtonRect;

        // Test mode selection
        private TestMode _testMode = TestMode.Statistical;
        private bool _showModeSelection = true;

        // Live test progress tracking
        private int _currentTestLevelIndex = 0;
        private string[] _testLevelIds = new string[0];
        private string[] _testLevelNames = new string[0];
        private BotVisualDebugger _currentVisualDebugger = null;
        private List<string> _onScreenLog = new List<string>();
        private AutoTestBot _activeVisualBot = null;
        private EnhancedLevelTestResult _activeVisualResult = null;

        private const int MAX_LOG_LINES = 15;

        public override void OnEnter()
        {
            _titleFont = new Font("Courier New", 18, FontStyle.Bold);
            _headFont = new Font("Courier New", 12, FontStyle.Bold);
            _bodyFont = new Font("Courier New", 10);
            _smallFont = new Font("Courier New", 8);
            _buttonFont = new Font("Courier New", 12, FontStyle.Bold);
            _testRunning = false;
            _results.Clear();
            _enhancedResults.Clear();
            _showModeSelection = true;
            _currentDisplayIndex = 0;
        }

        public override void OnExit()
        {
            _titleFont?.Dispose();
            _headFont?.Dispose();
            _bodyFont?.Dispose();
            _smallFont?.Dispose();
            _buttonFont?.Dispose();
        }

        public override void Update(float dt)
        {
            if (_testRunning)
            {
                // Process one level per frame during testing
                ProcessNextTestLevel();
                return;
            }

            var input = Game.Instance.Input;

            // Mode selection screen - keyboard input for 1/2
            if (_showModeSelection && _results.Count == 0 && !_testRunning)
            {
                // Key 1 - Statistical mode
                if (input.IsPressed(System.Windows.Forms.Keys.D1) || input.IsPressed(System.Windows.Forms.Keys.NumPad1))
                {
                    _testMode = TestMode.Statistical;
                    _showModeSelection = false;
                    StartTest();
                    return;
                }
                // Key 2 - Visual mode
                if (input.IsPressed(System.Windows.Forms.Keys.D2) || input.IsPressed(System.Windows.Forms.Keys.NumPad2))
                {
                    _testMode = TestMode.Visual;
                    _showModeSelection = false;
                    StartTest();
                    return;
                }
            }

            // Handle keyboard input
            if (input.InteractPressed)
            {
                // Enter key pressed
                if (_results.Count == 0 && !_showModeSelection)
                {
                    // Start the test
                    StartTest();
                }
                else if (_results.Count > 0)
                {
                    // Rerun the test
                    RerunTest();
                }
            }

            if (input.PausePressed)
            {
                // ESC to go back
                Game.Instance.Scenes.Pop();
            }

            if (_results.Count > 0)
            {
                _displayTimer += dt;
                if (_displayTimer >= DISPLAY_DURATION)
                {
                    _displayTimer = 0f;
                    _currentDisplayIndex = (_currentDisplayIndex + 1) % _results.Count;
                }
            }
        }

        public override void Draw(Graphics g)
        {
            _canvasWidth = (int)g.VisibleClipBounds.Width;
            _canvasHeight = (int)g.VisibleClipBounds.Height;
            if (_canvasWidth <= 0) _canvasWidth = 800;
            if (_canvasHeight <= 0) _canvasHeight = 600;

            // Background
            using (var br = new SolidBrush(Color.FromArgb(220, 20, 20, 40)))
                g.FillRectangle(br, 0, 0, _canvasWidth, _canvasHeight);

            // Title
            g.DrawString("AUTOMATED LEVEL BEATABILITY TEST - SMART BOT", _titleFont, Brushes.Gold, 20, 20);

            if (_testRunning)
            {
                // Show live test progress with on-screen logging
                DrawLiveTestProgress(g, _canvasWidth, _canvasHeight);
            }
            else if (_showModeSelection && _results.Count == 0)
            {
                // Show test mode selection
                DrawModeSelection(g, _canvasWidth, _canvasHeight);
            }
            else if (_results.Count == 0)
            {
                // Show instructions
                DrawInstructions(g, _canvasWidth, _canvasHeight);
            }
            else
            {
                // Show results
                DrawResults(g, _canvasWidth, _canvasHeight);
            }
        }

        /// <summary>
        /// Display live test progress with on-screen logging
        /// </summary>
        private void DrawLiveTestProgress(Graphics g, int W, int H)
        {
            // Progress panel
            int panelX = 40;
            int panelY = 80;
            int panelW = W - 80;
            int panelH = H - 140;

            // Background
            using (var br = new SolidBrush(Color.FromArgb(180, 40, 40, 80)))
                g.FillRectangle(br, panelX, panelY, panelW, panelH);

            using (var pen = new Pen(Color.Cyan, 2))
                g.DrawRectangle(pen, panelX, panelY, panelW, panelH);

            // Header
            int y = panelY + 15;
            g.DrawString("🤖 LIVE TEST PROGRESS", _headFont, Brushes.Cyan, panelX + 20, y);
            y += 30;

            // Current test info
            g.DrawString($"Current Level: [{_currentTestLevelIndex + 1}/{_testLevelIds.Length}]", _bodyFont, Brushes.Yellow, panelX + 20, y);
            y += 25;
            if (_currentTestLevelIndex < _testLevelNames.Length)
            {
                g.DrawString($"Testing: {_testLevelNames[_currentTestLevelIndex]}", _bodyFont, Brushes.LimeGreen, panelX + 20, y);
            }
            y += 30;

            // Visual Bot Rendering (if in visual mode)
            if (_testMode == TestMode.Visual && _activeVisualBot != null && _currentVisualDebugger != null)
            {
                int vpX = panelX + 20;
                int vpY = y;
                int vpW = panelW - 40;
                int vpH = 110;

                // Viewport background — mimics a level floor/sky
                using (var br = new SolidBrush(Color.FromArgb(160, 30, 50, 100)))
                    g.FillRectangle(br, vpX, vpY, vpW, vpH);

                // Ground line at bottom of viewport
                using (var pen = new Pen(Color.SaddleBrown, 3))
                    g.DrawLine(pen, vpX, vpY + vpH - 10, vpX + vpW, vpY + vpH - 10);

                // Viewport border
                using (var pen = new Pen(Color.Cyan, 2))
                    g.DrawRectangle(pen, vpX, vpY, vpW, vpH);

                // Clip drawing to viewport so bot can't escape the box
                var clip = g.Clip;
                g.SetClip(new Rectangle(vpX, vpY, vpW, vpH));

                // Scroll: keep bot centred horizontally in the viewport
                float botWorldX = _activeVisualBot.BotX;
                float botWorldY = _activeVisualBot.BotY;
                float cameraX   = botWorldX - vpW * 0.4f;  // look ahead a little

                // Draw simple tiled ground tiles behind bot
                using (var tileBr = new SolidBrush(Color.FromArgb(80, Color.SaddleBrown)))
                {
                    for (int tx = (int)(cameraX / 32) * 32; tx < cameraX + vpW + 32; tx += 32)
                    {
                        int screenTileX = (int)(vpX + tx - cameraX);
                        g.FillRectangle(tileBr, screenTileX, vpY + vpH - 10, 32, 10);
                        using (var pen = new Pen(Color.FromArgb(60, Color.White), 1))
                            g.DrawRectangle(pen, screenTileX, vpY + vpH - 10, 32, 10);
                    }
                }

                // Bot screen position — Y is fixed to ground level in viewport
                int botScreenX = (int)(vpX + botWorldX - cameraX);
                int botScreenY = vpY + vpH - 10 - 20;  // sits on ground
                float scale = 1.25f;

                // Draw the bot using the visual debugger's method (pass adjusted origin)
                _currentVisualDebugger.DrawBotVisualAt(g, botScreenX, botScreenY, scale);

                g.Clip = clip;  // restore clip

                // HUD strip below viewport
                int hudY = vpY + vpH + 4;
                g.DrawString($"Pos: ({botWorldX:F0}, {botWorldY:F0})", _smallFont, Brushes.White, vpX, hudY);
                g.DrawString($"State: {_activeVisualBot.State}", _smallFont, Brushes.Yellow, vpX + 140, hudY);
                g.DrawString($"T: {_activeVisualBot.TimeInLevel:F1}s", _smallFont, Brushes.Cyan, vpX + 260, hudY);
                g.DrawString($"Dist: {_activeVisualBot.DistanceTraveled:F0}px", _smallFont, Brushes.LimeGreen, vpX + 340, hudY);
                g.DrawString($"Items: {_activeVisualBot.ItemsCollected}", _smallFont, Brushes.Gold, vpX + 450, hudY);
                g.DrawString($"Enemies: {_activeVisualBot.EnemiesDefeated}", _smallFont, Brushes.OrangeRed, vpX + 520, hudY);

                y += vpH + 26;
            }

            // Log display
            g.DrawString("═══ LIVE LOG ═══", _bodyFont, Brushes.White, panelX + 20, y);
            y += 20;

            int logX = panelX + 30;
            int logY = y;
            foreach (var logLine in _onScreenLog)
            {
                Color logColor = logLine.Contains("✅") ? Color.LimeGreen :
                                logLine.Contains("❌") ? Color.OrangeRed :
                                logLine.Contains("🤖") ? Color.Cyan : Color.White;
                g.DrawString(logLine, _smallFont, new SolidBrush(logColor), logX, logY);
                logY += 16;
            }

            // Progress bar
            int progressBarX = panelX + 20;
            int progressBarY = panelY + panelH - 45;
            int progressBarW = panelW - 40;
            int progressBarH = 20;

            float progress = _currentTestLevelIndex / (float)_testLevelIds.Length;
            int filledWidth = (int)(progressBarW * progress);

            // Background
            using (var br = new SolidBrush(Color.DarkGray))
                g.FillRectangle(br, progressBarX, progressBarY, progressBarW, progressBarH);

            // Filled portion
            using (var br = new SolidBrush(Color.LimeGreen))
                g.FillRectangle(br, progressBarX, progressBarY, filledWidth, progressBarH);

            // Border
            using (var pen = new Pen(Color.White, 2))
                g.DrawRectangle(pen, progressBarX, progressBarY, progressBarW, progressBarH);

            // Progress text
            string progressText = $"{_currentTestLevelIndex}/{_testLevelIds.Length} Levels";
            SizeF progressTextSize = g.MeasureString(progressText, _smallFont);
            g.DrawString(progressText, _smallFont, Brushes.White, 
                progressBarX + (progressBarW - progressTextSize.Width) / 2, 
                progressBarY + (progressBarH - progressTextSize.Height) / 2);
        }

        /// <summary>
        /// Draw test mode selection screen
        /// </summary>
        private void DrawModeSelection(Graphics g, int W, int H)
        {
            g.DrawString("═══════════════════════════════════════════════════════════", _bodyFont, Brushes.White, 20, 80);
            g.DrawString("SELECT TEST MODE", _headFont, Brushes.White, 20, 110);
            g.DrawString("═══════════════════════════════════════════════════════════", _bodyFont, Brushes.White, 20, 140);

            int y = 180;
            g.DrawString("STATISTICAL MODE  ⚠ SIMULATION ONLY", _bodyFont, Brushes.Yellow, 30, y); y += 25;
            g.DrawString("  • Fast rough-estimate — does NOT run the real game", _smallFont, Brushes.OrangeRed, 50, y); y += 20;
            g.DrawString("  • Results are guesses based on distance/time math", _smallFont, Brushes.OrangeRed, 50, y); y += 20;
            g.DrawString("  • Use only for a quick smoke check, NOT for real QA", _smallFont, Brushes.Orange, 50, y); y += 30;

            g.DrawString("VISUAL MODE  ✅ REAL GAME BOT (recommended)", _bodyFont, Brushes.LimeGreen, 30, y); y += 25;
            g.DrawString("  • Runs the ACTUAL level with a real player entity", _smallFont, Brushes.Cyan, 50, y); y += 20;
            g.DrawString("  • Bot sprints, jumps, attacks using real input", _smallFont, Brushes.Cyan, 50, y); y += 20;
            g.DrawString("  • GodMode protects against unfair deaths mid-test", _smallFont, Brushes.Cyan, 50, y); y += 20;
            g.DrawString("  • Only real beatability results — trust these", _smallFont, Brushes.Cyan, 50, y); y += 30;

            // Mode selection buttons
            int btnW = 250, btnH = 50;
            int btnY = H - 180;

            // Statistical button
            _startButtonRect = new Rectangle((W / 2 - btnW - 20), btnY, btnW, btnH);
            using (var br = new SolidBrush(Color.FromArgb(200, Color.Gold)))
                g.FillRectangle(br, _startButtonRect);
            using (var pen = new Pen(Color.Yellow, 3))
                g.DrawRectangle(pen, _startButtonRect);
            using (var f = new Font("Courier New", 12, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString("[1] STATISTICAL", f);
                g.DrawString("[1] STATISTICAL", f, Brushes.Black, 
                    _startButtonRect.X + (_startButtonRect.Width - sz.Width) / 2,
                    _startButtonRect.Y + (_startButtonRect.Height - sz.Height) / 2);
            }

            // Visual button
            _visualTestButtonRect = new Rectangle((W / 2 + 20), btnY, btnW, btnH);
            using (var br = new SolidBrush(Color.FromArgb(200, Color.LimeGreen)))
                g.FillRectangle(br, _visualTestButtonRect);
            using (var pen = new Pen(Color.White, 3))
                g.DrawRectangle(pen, _visualTestButtonRect);
            using (var f = new Font("Courier New", 12, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString("[2] VISUAL", f);
                g.DrawString("[2] VISUAL", f, Brushes.Black, 
                    _visualTestButtonRect.X + (_visualTestButtonRect.Width - sz.Width) / 2,
                    _visualTestButtonRect.Y + (_visualTestButtonRect.Height - sz.Height) / 2);
            }

            // Back button
            int backBtnX = (W - 150) / 2;
            int backBtnY = H - 70;
            _backButtonRect = new Rectangle(backBtnX, backBtnY, 150, 40);

            using (var br = new SolidBrush(Color.FromArgb(120, Color.DarkOrange)))
                g.FillRectangle(br, _backButtonRect);
            using (var pen = new Pen(Color.Orange, 2))
                g.DrawRectangle(pen, _backButtonRect);

            g.DrawString("[ESC] BACK", _bodyFont, Brushes.White, backBtnX + 20, backBtnY + 8);
        }

        /// <summary>
        /// Process one level test per frame for smooth real-time visualization
        /// </summary>
        private void ProcessNextTestLevel()
        {
            if (_currentTestLevelIndex >= _testLevelIds.Length)
            {
                // All tests complete
                _testRunning = false;
                _results = _testMode == TestMode.Statistical
                    ? LevelAutoTestManager.AllResults
                    : _enhancedResults.Select(e => new LevelAutoTestResult
                    {
                        LevelId = e.LevelId,
                        LevelName = e.LevelName,
                        IsBeatable = e.IsBeatable,
                        TimeToComplete = e.TimeToComplete,
                        ItemsCollected = e.ItemsCollected,
                        EnemiesDefeated = e.EnemiesDefeated,
                        FailureReason = e.FailureReason,
                        BotData = e.BotData
                    }).ToList();

                _activeVisualBot = null;
                _activeVisualResult = null;
                _currentVisualDebugger = null;
                _currentTestLevelIndex = 0;
                _onScreenLog.Clear();
                return;
            }

            // Test one level
            string levelId = _testLevelIds[_currentTestLevelIndex];
            string levelName = _testLevelNames[_currentTestLevelIndex];

            AddLog($"[{_currentTestLevelIndex + 1}/{_testLevelIds.Length}] Testing: {levelName}...");

            if (_testMode == TestMode.Statistical)
            {
                var result = LevelAutoTestManager.TestLevelSingle(levelId, levelName);
                LevelAutoTestManager.AllResults.Add(result);

                string status = result.IsBeatable ? "✅ BEATABLE" : "❌ NOT BEATABLE";
                AddLog($"        Status: {status}");
                AddLog($"        {result.BotData.GetSummary()}");

                if (!string.IsNullOrEmpty(result.FailureReason))
                {
                    AddLog($"        Issue: {result.FailureReason}");
                }

                AddLog("");
                _currentTestLevelIndex++;
            }
            else  // Visual mode
            {
                // Initialize one level run and then advance frame-by-frame so it is visible.
                if (_activeVisualBot == null)
                {
                    _activeVisualBot = new AutoTestBot();
                    _activeVisualBot.Initialize(100f, 300f);

                    _currentVisualDebugger = new BotVisualDebugger();
                    _currentVisualDebugger.StartLevelDebug(_activeVisualBot, levelName);

                    _activeVisualResult = new EnhancedLevelTestResult
                    {
                        LevelId = levelId,
                        LevelName = levelName,
                        BotData = _activeVisualBot,
                        VisualDebugData = _currentVisualDebugger
                    };
                }

                const float step = 0.016f;
                _activeVisualBot.Update(step);
                _currentVisualDebugger.Update(step);
                SimulateVisualLevelEvents(_activeVisualBot, _activeVisualBot.TimeInLevel, levelId);

                bool finished = false;
                if (_activeVisualBot.State == AutoTestBot.BotState.WonLevel)
                {
                    _activeVisualResult.IsBeatable = true;
                    _activeVisualResult.TimeToComplete = _activeVisualBot.TimeInLevel;
                    _activeVisualResult.ItemsCollected = _activeVisualBot.ItemsCollected;
                    _activeVisualResult.EnemiesDefeated = _activeVisualBot.EnemiesDefeated;
                    finished = true;
                }
                else if (_activeVisualBot.TimeInLevel >= 60f)
                {
                    _activeVisualResult.IsBeatable = false;
                    _activeVisualResult.FailureReason = "Timeout - Level took too long";
                    finished = true;
                }

                if (finished)
                {
                    if (_activeVisualBot.DistanceTraveled < 50f)
                    {
                        _activeVisualResult.IsBeatable = false;
                        _activeVisualResult.FailureReason = "Bot made insufficient progress";
                    }

                    _activeVisualResult.BotGotStuck = _currentVisualDebugger.WasStuckAtAnyPoint();
                    _activeVisualResult.StuckDuration = _currentVisualDebugger.GetStuckDuration();
                    _activeVisualResult.DetailedActionLog = _currentVisualDebugger.GetActionLog();
                    _enhancedResults.Add(_activeVisualResult);

                    string status = _activeVisualResult.IsBeatable ? "✅ BEATABLE" : "❌ NOT BEATABLE";
                    AddLog($"        Status: {status}");
                    if (_activeVisualResult.BotGotStuck)
                        AddLog("        ⚠️ Bot got stuck!");
                    AddLog($"        {_activeVisualResult.GetSummary()}");

                    if (!string.IsNullOrEmpty(_activeVisualResult.FailureReason))
                    {
                        AddLog($"        Issue: {_activeVisualResult.FailureReason}");
                    }

                    AddLog("");
                    _currentTestLevelIndex++;
                    _activeVisualBot = null;
                    _activeVisualResult = null;
                }
            }
        }

        private static void SimulateVisualLevelEvents(AutoTestBot bot, float time, string levelId)
        {
            // Item and enemy counters only — NO artificial level completion.
            // Visual mode now uses BotPlayLevelScene (real game), so this
            // helper is only invoked from the old Statistical fallback path.
            if ((time % 5f) < 0.1f && time > 0)
                bot.CollectItem();

            if ((time % 8f) < 0.1f && time > 0)
                bot.DefeatEnemy();
        }

        /// <summary>
        /// Add message to on-screen log display
        /// </summary>
        private void AddLog(string message)
        {
            _onScreenLog.Add(message);
            if (_onScreenLog.Count > MAX_LOG_LINES)
            {
                _onScreenLog.RemoveAt(0);
            }
        }

        private void DrawInstructions(Graphics g, int W, int H)
        {
            g.DrawString("═══════════════════════════════════════════════════════════", _bodyFont, Brushes.White, 20, 80);
            g.DrawString("AUTOMATED LEVEL TESTING SYSTEM", _headFont, Brushes.White, 20, 110);
            g.DrawString("═══════════════════════════════════════════════════════════", _bodyFont, Brushes.White, 20, 140);

            int y = 180;
            g.DrawString("This system tests all 18 levels using an intelligent AI bot.", _bodyFont, Brushes.LimeGreen, 30, y); y += 25;
            g.DrawString("The bot uses abilities strategically:", _bodyFont, Brushes.LimeGreen, 30, y); y += 25;
            g.DrawString("  • Frost Ball attacks - Freeze enemies", _smallFont, Brushes.LimeGreen, 50, y); y += 20;
            g.DrawString("  • Dash ability - Escape obstacles", _smallFont, Brushes.LimeGreen, 50, y); y += 20;
            g.DrawString("  • Double jump & wall slides", _smallFont, Brushes.LimeGreen, 50, y); y += 20;
            g.DrawString("  • Stomp attacks on enemies", _smallFont, Brushes.LimeGreen, 50, y); y += 20;
            g.DrawString("  • Stuck detection & recovery", _smallFont, Brushes.LimeGreen, 50, y); y += 40;
            g.DrawString("📝 Comprehensive logs generated for analysis:", _bodyFont, Brushes.Yellow, 30, y); y += 25;
            g.DrawString("  • Action timeline | Ability usage | Bot decisions", _smallFont, Brushes.Cyan, 50, y); y += 20;
            g.DrawString("  • Saved to: Logs/bot-tests/", _smallFont, Brushes.Cyan, 50, y); y += 40;

            // Draw prominent START button
            int btnX = (W - 300) / 2;
            int btnY = H - 140;
            int btnW = 300;
            int btnH = 50;

            _startButtonRect = new Rectangle(btnX, btnY, btnW, btnH);

            // Button background (gold/yellow)
            using (var br = new SolidBrush(Color.FromArgb(200, Color.Gold)))
                g.FillRectangle(br, _startButtonRect);

            // Button border
            using (var pen = new Pen(Color.Yellow, 3))
                g.DrawRectangle(pen, _startButtonRect);

            // Button text
            string btnText = "[ENTER] START TEST";
            SizeF btnTextSize = g.MeasureString(btnText, _buttonFont);
            float textX = btnX + (btnW - btnTextSize.Width) / 2;
            float textY = btnY + (btnH - btnTextSize.Height) / 2;
            g.DrawString(btnText, _buttonFont, Brushes.Black, textX, textY);

            // Draw back button
            int backBtnX = (W - 200) / 2;
            int backBtnY = H - 70;
            int backBtnW = 200;
            int backBtnH = 40;

            _backButtonRect = new Rectangle(backBtnX, backBtnY, backBtnW, backBtnH);

            using (var br = new SolidBrush(Color.FromArgb(120, Color.DarkOrange)))
                g.FillRectangle(br, _backButtonRect);

            using (var pen = new Pen(Color.Orange, 2))
                g.DrawRectangle(pen, _backButtonRect);

            string backText = "[ESC] BACK";
            SizeF backTextSize = g.MeasureString(backText, _bodyFont);
            float backTextX = backBtnX + (backBtnW - backTextSize.Width) / 2;
            float backTextY = backBtnY + (backBtnH - backTextSize.Height) / 2;
            g.DrawString(backText, _bodyFont, Brushes.White, backTextX, backTextY);
        }

        private void DrawResults(Graphics g, int W, int H)
        {
            if (_currentDisplayIndex >= _results.Count)
                return;

            var result = _results[_currentDisplayIndex];

            // Current result panel
            int panelX = 40;
            int panelY = 80;
            int panelW = W - 80;
            int panelH = H - 180;

            // Background
            using (var br = new SolidBrush(Color.FromArgb(180, 40, 40, 80)))
                g.FillRectangle(br, panelX, panelY, panelW, panelH);

            Color borderColor = result.IsBeatable ? Color.LimeGreen : Color.OrangeRed;
            using (var pen = new Pen(borderColor, 3))
                g.DrawRectangle(pen, panelX, panelY, panelW, panelH);

            // Display info
            int y = panelY + 20;
            Color textColor = result.IsBeatable ? Color.LimeGreen : Color.OrangeRed;
            g.DrawString($"{result.LevelName}", _headFont, new SolidBrush(textColor), panelX + 20, y);
            y += 40;

            g.DrawString($"Status: {(result.IsBeatable ? "✅ BEATABLE" : "❌ NOT BEATABLE")}", _bodyFont, Brushes.White, panelX + 20, y); y += 25;
            g.DrawString($"Time to Complete: {result.TimeToComplete:F1}s", _bodyFont, Brushes.White, panelX + 20, y); y += 25;
            g.DrawString($"Distance Traveled: {result.BotData.DistanceTraveled:F0}px", _bodyFont, Brushes.White, panelX + 20, y); y += 25;
            g.DrawString($"Items Collected: {result.ItemsCollected}", _bodyFont, Brushes.White, panelX + 20, y); y += 25;
            g.DrawString($"Enemies Defeated: {result.EnemiesDefeated}", _bodyFont, Brushes.White, panelX + 20, y); y += 30;

            if (!string.IsNullOrEmpty(result.FailureReason))
            {
                g.DrawString($"Issue: {result.FailureReason}", _bodyFont, Brushes.OrangeRed, panelX + 20, y);
            }

            // Progress indicator
            int totalBeatable = _results.Where(r => r.IsBeatable).Count();
            int progressY = H - 120;
            g.DrawString($"Progress: {_currentDisplayIndex + 1} / {_results.Count}  |  Beatable: {totalBeatable}/{_results.Count}", _bodyFont, Brushes.Cyan, 40, progressY);

            // Draw Rerun button
            int rerunBtnX = (W - 250) / 2;
            int rerunBtnY = H - 90;
            int rerunBtnW = 250;
            int rerunBtnH = 40;

            _rerunButtonRect = new Rectangle(rerunBtnX, rerunBtnY, rerunBtnW, rerunBtnH);

            using (var br = new SolidBrush(Color.FromArgb(150, Color.LimeGreen)))
                g.FillRectangle(br, _rerunButtonRect);

            using (var pen = new Pen(Color.LimeGreen, 2))
                g.DrawRectangle(pen, _rerunButtonRect);

            string rerunText = "[ENTER] RERUN TEST";
            SizeF rerunTextSize = g.MeasureString(rerunText, _bodyFont);
            float rerunTextX = rerunBtnX + (rerunBtnW - rerunTextSize.Width) / 2;
            float rerunTextY = rerunBtnY + (rerunBtnH - rerunTextSize.Height) / 2;
            g.DrawString(rerunText, _bodyFont, Brushes.Black, rerunTextX, rerunTextY);

            // Draw back button
            int backBtnX = (W - 150) / 2;
            int backBtnY = H - 40;
            int backBtnW = 150;
            int backBtnH = 35;

            _backButtonRect = new Rectangle(backBtnX, backBtnY, backBtnW, backBtnH);

            using (var br = new SolidBrush(Color.FromArgb(120, Color.DarkOrange)))
                g.FillRectangle(br, _backButtonRect);

            using (var pen = new Pen(Color.Orange, 2))
                g.DrawRectangle(pen, _backButtonRect);

            string backText = "[ESC] BACK";
            SizeF backTextSize = g.MeasureString(backText, _bodyFont);
            float backTextX = backBtnX + (backBtnW - backTextSize.Width) / 2;
            float backTextY = backBtnY + (backBtnH - backTextSize.Height) / 2;
            g.DrawString(backText, _bodyFont, Brushes.White, backTextX, backTextY);
        }

        public override void HandleClick(System.Drawing.Point p)
        {
            // Mode selection screen
            if (_showModeSelection && _results.Count == 0 && !_testRunning)
            {
                if (_startButtonRect.Contains(p))
                {
                    _testMode = TestMode.Statistical;
                    _showModeSelection = false;
                    StartTest();
                }
                else if (_visualTestButtonRect.Contains(p))
                {
                    _testMode = TestMode.Visual;
                    _showModeSelection = false;
                    StartTest();
                }
                else if (_backButtonRect.Contains(p))
                {
                    Game.Instance.Scenes.Pop();
                }
            }
            // Test mode: start/rerun buttons
            else if (!_testRunning && _results.Count == 0)
            {
                // Start button clicked
                if (_startButtonRect.Contains(p))
                {
                    StartTest();
                }
                // Back button clicked
                else if (_backButtonRect.Contains(p))
                {
                    Game.Instance.Scenes.Pop();
                }
            }
            else if (_results.Count > 0)
            {
                // Rerun button clicked
                if (_rerunButtonRect.Contains(p))
                {
                    RerunTest();
                }
                // Back button clicked
                else if (_backButtonRect.Contains(p))
                {
                    Game.Instance.Scenes.Pop();
                }
            }
        }

        private static readonly string[] _allLevelIds = {
            "dino", "storm1", "sky", "blockade", "wano", "warlord1",
            "harbor", "coral", "tundra", "storm2", "warlord2",
            "dive_gate", "sunken_gate", "kelp", "boiling_vent", "abyss", "centipede_final"
        };

        private static readonly string[] _allLevelNames = {
            "1. Dinosaur Island", "2. Storm Belt", "3. Sky Island", "4. Marine Blockade",
            "5. Blade Nation", "6. Warlord: Sudo", "7. Harbor Town", "8. Coral Reef",
            "9. Tundra Peak", "10. Tempest Strait", "11. Warlord: Vanta", "12. Dive Gate",
            "13. Sunken Gate", "14. Kelp Maze", "15. Vent Ruins", "16. Abyss", "17. Centipede Boss"
        };

        private void StartTest()
        {
            _results.Clear();
            _enhancedResults.Clear();
            LevelAutoTestManager.AllResults.Clear();
            _currentDisplayIndex = 0;
            _displayTimer = 0f;
            _currentTestLevelIndex = 0;
            _onScreenLog.Clear();

            _testLevelIds   = _allLevelIds;
            _testLevelNames = _allLevelNames;

            if (_testMode == TestMode.Visual)
            {
                // ── VISUAL MODE: push the real game scene driven by bot input ──
                // _testRunning stays false; the BotPlayLevelScene handles everything.
                LaunchNextVisualLevel();
            }
            else
            {
                // ── STATISTICAL MODE: fast blocking simulation ─────────────────
                _testRunning = true;
                AddLog("🤖 AUTO-TEST BOT: Starting Statistical mode test...");
                AddLog("");
            }
        }

        /// <summary>
        /// Pushes the next BotPlayLevelScene.  When it finishes it calls back
        /// here to either push the next one or show results.
        /// </summary>
        private void LaunchNextVisualLevel()
        {
            if (_currentTestLevelIndex >= _testLevelIds.Length)
            {
                // All levels done — build result list and show summary
                _testRunning = false;
                _results = _enhancedResults.Select(e => new LevelAutoTestResult
                {
                    LevelId       = e.LevelId,
                    LevelName     = e.LevelName,
                    IsBeatable    = e.IsBeatable,
                    TimeToComplete = e.TimeToComplete,
                    ItemsCollected = e.ItemsCollected,
                    EnemiesDefeated = e.EnemiesDefeated,
                    FailureReason = e.FailureReason,
                    BotData       = e.BotData
                }).ToList();
                return;
            }

            string id   = _testLevelIds[_currentTestLevelIndex];
            string name = _testLevelNames[_currentTestLevelIndex];
            int    idx  = _currentTestLevelIndex;   // capture for lambda

            // Create the real game scene for this level
            Scene inner = LevelSceneFactory.Create(id, name);

            // Wrap it in BotPlayLevelScene so the bot drives the input
            var wrapper = new BotPlayLevelScene(inner, name, (beaten, elapsed) =>
            {
                // Record result
                var result = new EnhancedLevelTestResult
                {
                    LevelId         = id,
                    LevelName       = name,
                    IsBeatable      = beaten,
                    TimeToComplete  = elapsed,
                    FailureReason   = beaten ? "" : "Timeout or insufficient progress",
                    BotData         = new AutoTestBot()   // placeholder — real data is in the scene
                };
                _enhancedResults.Add(result);

                string status = beaten ? "✅ BEATABLE" : "❌ NOT BEATABLE";
                AddLog($"[{idx + 1}/{_testLevelIds.Length}] {name}: {status}  ({elapsed:F1}s)");

                // Advance and launch next
                _currentTestLevelIndex++;
                LaunchNextVisualLevel();
            });

            Game.Instance.Scenes.Push(wrapper);
        }

        private void RerunTest()
        {
            _results.Clear();
            _enhancedResults.Clear();
            LevelAutoTestManager.AllResults.Clear();
            _currentDisplayIndex = 0;
            _displayTimer = 0f;
            _currentTestLevelIndex = 0;
            _onScreenLog.Clear();

            _testLevelIds   = _allLevelIds;
            _testLevelNames = _allLevelNames;

            if (_testMode == TestMode.Visual)
            {
                AddLog("🤖 AUTO-TEST BOT: Rerunning Visual mode test...");
                LaunchNextVisualLevel();
            }
            else
            {
                _testRunning = true;
                AddLog("🤖 AUTO-TEST BOT: Rerunning Statistical mode test...");
                AddLog("");
            }
        }
    }
}
