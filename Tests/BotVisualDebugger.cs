using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// PHASE 2 - Team 10: Engine Programmer
    /// Feature: Bot Visual Debugger and Analysis System
    /// Purpose: Renders bot visually during testing, detects stuck states, and generates detailed logs
    /// </summary>
    public class BotVisualDebugger
    {
        /// <summary>
        /// Tracks bot position history for stuck detection
        /// </summary>
        private struct PositionSample
        {
            public float X;
            public float Y;
            public float Time;
        }

        /// <summary>
        /// Detailed action log entry
        /// </summary>
        public struct BotActionLog
        {
            public float Time { get; set; }
            public string Action { get; set; }
            public float BotX { get; set; }
            public float BotY { get; set; }
            public string State { get; set; }
            public string Details { get; set; }
        }

        private AutoTestBot _bot;
        private string _currentLevelName;
        private List<PositionSample> _positionHistory = new List<PositionSample>();
        private List<BotActionLog> _actionLog = new List<BotActionLog>();
        private float _lastLogTime = 0f;
        private const float LOG_INTERVAL = 0.1f;  // Log every 0.1 seconds

        // Item and enemy tracking
        private ItemAndEnemyAnalyzer _itemEnemyAnalyzer = new ItemAndEnemyAnalyzer();

        // Stuck detection constants
        private const float STUCK_THRESHOLD = 50f;  // pixels
        private const float STUCK_CHECK_TIME = 3f;   // seconds

        private bool _isStuck = false;
        private float _stuckStartTime = 0f;

        /// <summary>
        /// Initialize visual debugger for a level
        /// </summary>
        public void StartLevelDebug(AutoTestBot bot, string levelName)
        {
            _bot = bot;
            _currentLevelName = levelName;
            _positionHistory.Clear();
            _actionLog.Clear();
            _lastLogTime = 0f;
            _isStuck = false;
            _stuckStartTime = 0f;

            LogAction($"═══ LEVEL START: {levelName} ═══", "INIT");
            LogAction($"Bot initialized at position ({bot.BotX:F0}, {bot.BotY:F0})", "INIT");
        }

        /// <summary>
        /// Update visual debugger each frame — records position history and checks for stuck.
        /// NOTE: The bot's Update() must be called separately by the caller before this.
        /// </summary>
        public void Update(float dt)
        {
            if (_bot == null) return;

            // Sample current bot position for stuck detection
            _positionHistory.Add(new PositionSample
            {
                X = _bot.BotX,
                Y = _bot.BotY,
                Time = _bot.TimeInLevel
            });

            // Periodic state logging based on real bot time
            if (_bot.TimeInLevel - _lastLogTime >= LOG_INTERVAL)
            {
                LogCurrentState();
                _lastLogTime = _bot.TimeInLevel;
            }

            CheckForStuck();
        }

        /// <summary>
        /// Check if bot is stuck in one place
        /// </summary>
        private void CheckForStuck()
        {
            if (_positionHistory.Count < (int)(STUCK_CHECK_TIME / LOG_INTERVAL))
                return;

            // Get oldest sample from stuck check window
            var oldSample = _positionHistory[_positionHistory.Count - (int)(STUCK_CHECK_TIME / LOG_INTERVAL)];
            var currentSample = _positionHistory[_positionHistory.Count - 1];

            float distance = Math.Abs(currentSample.X - oldSample.X) + Math.Abs(currentSample.Y - oldSample.Y);

            if (distance < STUCK_THRESHOLD)
            {
                if (!_isStuck)
                {
                    _isStuck = true;
                    _stuckStartTime = currentSample.Time;
                    LogAction("⚠️ BOT STUCK DETECTED - Not moving!", "STUCK");
                }
            }
            else
            {
                if (_isStuck)
                {
                    LogAction("✅ Bot resumed movement", "UNSTUCK");
                    _isStuck = false;
                }
            }
        }

        /// <summary>
        /// Log current bot state
        /// </summary>
        private void LogCurrentState()
        {
            string stateStr = _bot.State.ToString();
            string details = $"Dist: {_bot.DistanceTraveled:F0}px | Items: {_bot.ItemsCollected} | Enemies: {_bot.EnemiesDefeated}";

            if (_bot.ShouldJump)
                LogAction("JUMP", stateStr, details);
            if (_bot.ShouldAttack)
                LogAction("ATTACK", stateStr, details);
            if (_bot.State == AutoTestBot.BotState.WonLevel)
                LogAction("🎉 LEVEL WON!", stateStr, details);
            if (_bot.State == AutoTestBot.BotState.Failed)
                LogAction("💀 LEVEL FAILED!", stateStr, details);
        }

        /// <summary>
        /// Log an action with full details
        /// </summary>
        public void LogAction(string action, string state, string details = "")
        {
            LogAction(action, state, _bot?.BotX ?? 0, _bot?.BotY ?? 0, details);
        }

        /// <summary>
        /// Overload for simple logging
        /// </summary>
        public void LogAction(string action, string details = "")
        {
            _actionLog.Add(new BotActionLog
            {
                Time = _bot?.TimeInLevel ?? 0f,
                Action = action,
                BotX = _bot?.BotX ?? 0f,
                BotY = _bot?.BotY ?? 0f,
                State = _bot?.State.ToString() ?? "Unknown",
                Details = details
            });
        }

        /// <summary>
        /// Internal log with all parameters
        /// </summary>
        private void LogAction(string action, string state, float botX, float botY, string details = "")
        {
            _actionLog.Add(new BotActionLog
            {
                Time = _bot?.TimeInLevel ?? 0f,
                Action = action,
                BotX = botX,
                BotY = botY,
                State = state,
                Details = details
            });
        }

        /// <summary>
        /// Get item and enemy analyzer
        /// </summary>
        public ItemAndEnemyAnalyzer GetAnalyzer() => _itemEnemyAnalyzer;

        /// <summary>
        /// Log an item encounter
        /// </summary>
        public void LogItemEncounter(float x, float y, string itemType, bool collected, string reason = "")
        {
            _itemEnemyAnalyzer.LogItemEncounter(x, y, itemType, _bot?.TimeInLevel ?? 0f, collected, reason);
            if (collected)
                LogAction($"💎 ITEM COLLECTED: {itemType}", "ITEM_COLLECT");
            else
                LogAction($"⚠️ ITEM NOT COLLECTED: {itemType} @ ({x:F0}, {y:F0})", "ITEM_FAIL");
        }

        /// <summary>
        /// Log an enemy encounter
        /// </summary>
        public void LogEnemyEncounter(float x, float y, string enemyType, bool defeated, string reason = "")
        {
            _itemEnemyAnalyzer.LogEnemyEncounter(x, y, enemyType, _bot?.TimeInLevel ?? 0f, defeated, reason);
            if (defeated)
                LogAction($"⚔️ ENEMY DEFEATED: {enemyType}", "ENEMY_DEFEAT");
            else
                LogAction($"⚠️ ENEMY NOT DEFEATED: {enemyType} @ ({x:F0}, {y:F0})", "ENEMY_FAIL");
        }

        /// <summary>
        /// Set total items available in level
        /// </summary>
        public void SetTotalItemsAvailable(int count) => _itemEnemyAnalyzer.SetTotalItemsAvailable(count);

        /// <summary>
        /// Set total enemies available in level
        /// </summary>
        public void SetTotalEnemiesAvailable(int count) => _itemEnemyAnalyzer.SetTotalEnemiesAvailable(count);

        /// <summary>
        /// Get item and enemy analysis report
        /// </summary>
        public string GetItemEnemyAnalysisReport() => _itemEnemyAnalyzer.GenerateComprehensiveReport();

        /// <summary>
        /// Get summary of analysis
        /// </summary>
        public string GetAnalysisSummary()
        {
            float itemCollect = _itemEnemyAnalyzer.GetItemCollectibilityPercentage();
            float enemyDefeat = _itemEnemyAnalyzer.GetEnemyDefeatPercentage();

            return $"Items: {itemCollect:F0}% collected | Enemies: {enemyDefeat:F0}% defeated";
        }

        /// <summary>
        /// Get all action logs
        /// </summary>
        public List<BotActionLog> GetActionLog() => new List<BotActionLog>(_actionLog);

        /// <summary>
        /// Check if bot ever got stuck
        /// </summary>
        public bool WasStuckAtAnyPoint() => _actionLog.Any(log => log.Action.Contains("STUCK"));

        /// <summary>
        /// Get stuck duration in seconds
        /// </summary>
        public float GetStuckDuration()
        {
            var stuckLogs = _actionLog.Where(log => log.Action.Contains("STUCK")).ToList();
            if (stuckLogs.Count == 0) return 0f;
            return _bot?.TimeInLevel ?? 0f - stuckLogs.First().Time;
        }

        /// <summary>
        /// Generate detailed analysis report
        /// </summary>
        public string GenerateAnalysisReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"\n═══════════════════════════════════════════════════════");
            report.AppendLine($"DETAILED VISUAL TEST REPORT - {_currentLevelName}");
            report.AppendLine($"═══════════════════════════════════════════════════════\n");

            report.AppendLine($"Test Duration: {_bot?.TimeInLevel:F2}s");
            report.AppendLine($"Final Position: ({_bot?.BotX:F0}, {_bot?.BotY:F0})");
            report.AppendLine($"Total Distance: {_bot?.DistanceTraveled:F0}px");
            report.AppendLine($"Items Collected: {_bot?.ItemsCollected}");
            report.AppendLine($"Enemies Defeated: {_bot?.EnemiesDefeated}");
            report.AppendLine($"Final State: {_bot?.State}\n");

            if (WasStuckAtAnyPoint())
            {
                report.AppendLine($"⚠️ BOT GOT STUCK: Yes");
                report.AppendLine($"   Stuck Duration: {GetStuckDuration():F2}s\n");
            }
            else
            {
                report.AppendLine($"✅ BOT GOT STUCK: No\n");
            }

            report.AppendLine("ACTION TIMELINE:");
            report.AppendLine("─────────────────────────────────────────────────────");
            foreach (var log in _actionLog.Take(20))  // Show first 20 actions
            {
                report.AppendLine($"[{log.Time:F2}s] {log.Action,-20} | State: {log.State,-12} | Pos: ({log.BotX:F0}, {log.BotY:F0}) | {log.Details}");
            }

            if (_actionLog.Count > 20)
                report.AppendLine($"\n... and {_actionLog.Count - 20} more actions");

            report.AppendLine($"\n═══════════════════════════════════════════════════════\n");

            return report.ToString();
        }

        /// <summary>
        /// Draw the bot sprite at an explicit screen position (used by the scrolling viewport).
        /// The caller computes the screen X/Y; this method just renders the sprite.
        /// </summary>
        public void DrawBotVisualAt(Graphics g, int screenX, int screenY, float scale = 1f)
        {
            if (_bot == null) return;

            int botW = (int)(20 * scale);
            int botH = (int)(20 * scale);

            // Body colour reflects current bot state
            Color bodyColor = _bot.State == AutoTestBot.BotState.Failed  ? Color.OrangeRed  :
                              _bot.State == AutoTestBot.BotState.WonLevel ? Color.LimeGreen  :
                              _isStuck                                    ? Color.Orange      :
                                                                            Color.DeepSkyBlue;

            // Body
            using (var br = new SolidBrush(bodyColor))
                g.FillRectangle(br, screenX, screenY, botW, botH);

            // White outline
            using (var pen = new Pen(Color.White, 1))
                g.DrawRectangle(pen, screenX, screenY, botW, botH);

            // Eyes — two white dots, positioned based on facing direction
            int eyeY = screenY + (int)(3 * scale);
            int eye1X = screenX + (int)((_bot.MoveRight ? 10 : 4) * scale);
            int eye2X = screenX + (int)((_bot.MoveRight ? 14 : 8) * scale);
            int eyeSize = Math.Max(2, (int)(3 * scale));
            g.FillEllipse(Brushes.White, eye1X, eyeY, eyeSize, eyeSize);
            g.FillEllipse(Brushes.White, eye2X, eyeY, eyeSize, eyeSize);

            // Jump indicator — yellow arc above bot when jumping
            if (_bot.ShouldJump)
            {
                using (var pen = new Pen(Color.Yellow, 2))
                    g.DrawArc(pen, screenX - 2, screenY - 8, botW + 4, 10, 0, -180);
            }

            // Attack indicator — red slash when attacking
            if (_bot.ShouldAttack)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    int dir = _bot.MoveRight ? 1 : -1;
                    g.DrawLine(pen, screenX + botW / 2, screenY + botH / 2,
                                    screenX + botW / 2 + dir * (int)(14 * scale),
                                    screenY + (int)(4 * scale));
                }
            }

            // Stuck pulse border
            if (_isStuck)
            {
                using (var pen = new Pen(Color.OrangeRed, 2))
                    g.DrawRectangle(pen, screenX - 3, screenY - 3, botW + 6, botH + 6);
            }
        }

        /// <summary>
        /// Draw bot on screen for visual debugging (world-space origin version — legacy)
        /// </summary>
        public void DrawBotVisual(Graphics g, float screenOriginX, float screenOriginY, float scale = 1f)
        {
            if (_bot == null) return;

            // Clamp X to a visible range relative to the origin so the bot doesn't fly off-screen
            float maxVisible = 300f;
            float relX = Math.Min(_bot.BotX * scale, maxVisible);
            int sx = (int)(screenOriginX + relX);
            int sy = (int)(screenOriginY);

            DrawBotVisualAt(g, sx, sy, scale);
        }

        /// <summary>
        /// Draw debug info on screen
        /// </summary>
        public void DrawDebugInfo(Graphics g, int screenWidth, int screenHeight)
        {
            if (_bot == null) return;

            using (var font = new Font("Courier New", 9))
            using (var brush = Brushes.White)
            {
                int y = screenHeight - 120;
                g.DrawString($"🤖 BOT POS: ({_bot.BotX:F0}, {_bot.BotY:F0})", font, brush, 10, y);
                y += 18;
                g.DrawString($"📊 STATE: {_bot.State} | TIME: {_bot.TimeInLevel:F1}s", font, brush, 10, y);
                y += 18;
                g.DrawString($"📈 DIST: {_bot.DistanceTraveled:F0}px | ITEMS: {_bot.ItemsCollected} | ENEMIES: {_bot.EnemiesDefeated}", font, brush, 10, y);
                y += 18;

                if (_isStuck)
                {
                    using (var redBrush = new SolidBrush(Color.OrangeRed))
                        g.DrawString($"⚠️ STUCK FOR {_bot.TimeInLevel - _stuckStartTime:F1}s", font, redBrush, 10, y);
                }
                else
                {
                    g.DrawString($"✅ Moving freely", font, brush, 10, y);
                }
            }
        }
    }
}
