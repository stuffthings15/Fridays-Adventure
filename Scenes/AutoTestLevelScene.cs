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

        public override void OnEnter()
        {
            _titleFont = new Font("Courier New", 18, FontStyle.Bold);
            _headFont = new Font("Courier New", 12, FontStyle.Bold);
            _bodyFont = new Font("Courier New", 10);
            _smallFont = new Font("Courier New", 8);
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
        }

        public override void Update(float dt)
        {
            if (_testRunning)
            {
                return;
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
            g.DrawString("  • Saved to: Logs/bot-tests/", _smallFont, Brushes.Cyan, 50, y); y += 20;

            // Draw buttons
            int btnY = H - 80;
            g.DrawString("[ENTER] Start Test", _bodyFont, Brushes.LimeGreen, 40, btnY);
            g.DrawString("[ESC] Back", _bodyFont, Brushes.Orange, 40, btnY + 30);
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

            // Draw navigation buttons
            int btnY = H - 50;
            g.DrawString("[LEFT/RIGHT] Navigate | [ENTER] Rerun | [ESC] Back", _smallFont, Brushes.Cyan, 40, btnY);
        }

        public override void HandleClick(System.Drawing.Point p)
        {
            if (!_testRunning && _results.Count == 0)
            {
                // Start button area
                if (p.X >= 40 && p.X <= 250 && p.Y >= _canvasHeight - 80 && p.Y <= _canvasHeight - 50)
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
            }
            else if (p.X >= 40 && p.X <= 250 && p.Y >= _canvasHeight - 80 && p.Y <= _canvasHeight - 50 && _results.Count > 0)
            {
                // Rerun test
                _testRunning = true;
                _results.Clear();
                _currentDisplayIndex = 0;
                _displayTimer = 0f;

                Console.WriteLine("\n🤖 AUTO-TEST BOT: Rerunning level beatability test...\n");
                LevelAutoTestManager.RunAllTests();
                _results = LevelAutoTestManager.AllResults;
                _testRunning = false;
            }

            // Back button - ESC also works
            if (p.X >= 40 && p.X <= 200 && p.Y >= _canvasHeight - 40 && p.Y <= _canvasHeight - 10)
            {
                Game.Instance.Scenes.Pop();
            }
        }
    }
}
