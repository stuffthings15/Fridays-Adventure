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

        public override void OnEnter()
        {
            _titleFont = new Font("Courier New", 18, FontStyle.Bold);
            _headFont = new Font("Courier New", 12, FontStyle.Bold);
            _bodyFont = new Font("Courier New", 10);
            _smallFont = new Font("Courier New", 8);
            _buttonFont = new Font("Courier New", 12, FontStyle.Bold);
            _testRunning = false;
            _results.Clear();
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
                return;
            }

            var input = Game.Instance.Input;

            // Handle keyboard input
            if (input.InteractPressed)
            {
                // Enter key pressed
                if (_results.Count == 0)
                {
                    // Start the test
                    StartTest();
                }
                else
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
                // Show loading message
                g.DrawString("🤖 Running smart bot tests with ability usage...", _headFont, Brushes.Cyan, 50, _canvasHeight / 2 - 40);
                g.DrawString("Generating detailed logs for analysis... Please wait.", _bodyFont, Brushes.LimeGreen, 50, _canvasHeight / 2 + 20);
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
            if (!_testRunning && _results.Count == 0)
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

        private void StartTest()
        {
            _testRunning = true;
            _results.Clear();
            _currentDisplayIndex = 0;
            _displayTimer = 0f;

            Console.WriteLine("\n🤖 AUTO-TEST BOT: Starting level beatability test...\n");
            LevelAutoTestManager.RunAllTests();
            _results = LevelAutoTestManager.AllResults;
            _testRunning = false;
        }

        private void RerunTest()
        {
            _testRunning = true;
            _results.Clear();
            _currentDisplayIndex = 0;
            _displayTimer = 0f;

            Console.WriteLine("\n🤖 AUTO-TEST BOT: Rerunning level beatability test...\n");
            LevelAutoTestManager.RunAllTests();
            _results = LevelAutoTestManager.AllResults;
            _testRunning = false;
        }
    }
}
