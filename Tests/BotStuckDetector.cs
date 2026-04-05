using System;
using System.Collections.Generic;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// PHASE 2 - Team 10: Engine Programmer
    /// Feature: Bot Stuck Detection & Analysis
    /// Purpose: Detects when bot gets stuck and logs detailed diagnostic data
    /// </summary>
    public class BotStuckDetector
    {
        // ════════════════════════════════════════════════════════════════════
        // STUCK DETECTION PARAMETERS
        // ════════════════════════════════════════════════════════════════════
        
        private const float NO_PROGRESS_THRESHOLD = 5f;  // pixels per second minimum
        private const float STUCK_TIME_THRESHOLD = 3f;   // seconds before marking stuck
        private const float CHECK_INTERVAL = 0.1f;       // check every 0.1 seconds
        
        // ════════════════════════════════════════════════════════════════════
        // TRACKING STATE
        // ════════════════════════════════════════════════════════════════════
        
        private float _lastX = 0f;
        private float _lastY = 0f;
        private float _timeSinceLastMove = 0f;
        private float _checkTimer = 0f;
        private bool _isCurrentlyStuck = false;
        
        private List<StuckEvent> _stuckEvents = new List<StuckEvent>();
        
        public class StuckEvent
        {
            public float TimeStuck { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float DurationSeconds { get; set; }
            public AutoTestBot.BotState State { get; set; }
            public string Reason { get; set; }
        }

        // ════════════════════════════════════════════════════════════════════
        // PUBLIC INTERFACE
        // ════════════════════════════════════════════════════════════════════
        
        public bool IsStuck => _isCurrentlyStuck;
        public float CurrentStuckDuration => _timeSinceLastMove;
        public List<StuckEvent> AllStuckEvents => _stuckEvents;

        public void Initialize(float startX, float startY)
        {
            _lastX = startX;
            _lastY = startY;
            _timeSinceLastMove = 0f;
            _checkTimer = 0f;
            _isCurrentlyStuck = false;
            _stuckEvents.Clear();
        }

        /// <summary>
        /// Update stuck detection - call every frame
        /// </summary>
        public void Update(float dt, AutoTestBot bot, TestSessionLogger logger)
        {
            _checkTimer += dt;
            
            // Check every 0.1 seconds to avoid excessive checks
            if (_checkTimer < CHECK_INTERVAL)
                return;
            
            _checkTimer = 0f;

            // Calculate distance moved since last check
            float dx = bot.BotX - _lastX;
            float dy = bot.BotY - _lastY;
            float distanceMoved = (float)Math.Sqrt(dx * dx + dy * dy);
            float speedPixelsPerSec = distanceMoved / CHECK_INTERVAL;

            // If bot moved significantly, reset stuck timer
            if (speedPixelsPerSec > NO_PROGRESS_THRESHOLD)
            {
                _timeSinceLastMove = 0f;
                _lastX = bot.BotX;
                _lastY = bot.BotY;
                
                // If we were stuck, we're not anymore
                if (_isCurrentlyStuck)
                {
                    logger?.WriteLine($"[BOT] ✓ Bot unstuck - resumed movement at X={bot.BotX:F0}");
                    _isCurrentlyStuck = false;
                }
            }
            else
            {
                // Bot is not moving
                _timeSinceLastMove += CHECK_INTERVAL;

                // If we just became stuck, log it
                if (_timeSinceLastMove >= STUCK_TIME_THRESHOLD && !_isCurrentlyStuck)
                {
                    _isCurrentlyStuck = true;
                    LogStuckEvent(bot, logger);
                }

                // Every 10 seconds while stuck, log status
                if (_isCurrentlyStuck && (int)(_timeSinceLastMove * 10) % 100 == 0)
                {
                    logger?.WriteLine($"[BOT_STUCK] Still stuck for {_timeSinceLastMove:F1}s at X={bot.BotX:F0} Y={bot.BotY:F0} State={bot.State}");
                }
            }
        }

        /// <summary>
        /// Log a stuck event with full diagnostics
        /// </summary>
        private void LogStuckEvent(AutoTestBot bot, TestSessionLogger logger)
        {
            var stuckEvent = new StuckEvent
            {
                TimeStuck = bot.TimeInLevel,
                X = bot.BotX,
                Y = bot.BotY,
                State = bot.State,
                Reason = DetermineStuckReason(bot)
            };

            _stuckEvents.Add(stuckEvent);

            logger?.WriteLine("");
            logger?.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            logger?.WriteLine("║ BOT STUCK DETECTED!                                           ║");
            logger?.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            logger?.WriteLine($"Time in Level: {bot.TimeInLevel:F1}s");
            logger?.WriteLine($"Position: X={bot.BotX:F0} Y={bot.BotY:F0}");
            logger?.WriteLine($"State: {bot.State}");
            logger?.WriteLine($"Distance Traveled: {bot.DistanceTraveled:F0}px");
            logger?.WriteLine($"Items Collected: {bot.ItemsCollected}");
            logger?.WriteLine($"Enemies Defeated: {bot.EnemiesDefeated}");
            logger?.WriteLine($"Likely Reason: {stuckEvent.Reason}");
            logger?.WriteLine("───────────────────────────────────────────────────────────────");
            logger?.WriteLine("");
        }

        /// <summary>
        /// Determine why the bot is stuck
        /// </summary>
        private string DetermineStuckReason(AutoTestBot bot)
        {
            if (bot.State == AutoTestBot.BotState.Failed)
                return "Bot reached failed state";

            if (bot.DistanceTraveled < 100f)
                return "No initial progress - stuck at level start";

            if (bot.BotX < 100f && bot.DistanceTraveled > 1000f)
                return "Bounced back or level wraps - unexpected position";

            if (bot.BotY > 600f)
                return "Fell below level (Y > 600) - likely fell into pit";

            if (bot.BotY < 0f)
                return "Position above top of level - physics error?";

            return "Unknown - not moving but no obvious reason";
        }

        /// <summary>
        /// Get diagnostic report of all stuck events
        /// </summary>
        public string GetStuckReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("");
            report.AppendLine("═══════════════════════════════════════════════════════════════");
            report.AppendLine("BOT STUCK EVENTS REPORT");
            report.AppendLine("═══════════════════════════════════════════════════════════════");
            
            if (_stuckEvents.Count == 0)
            {
                report.AppendLine("✓ No stuck events detected");
            }
            else
            {
                report.AppendLine($"Total stuck events: {_stuckEvents.Count}");
                report.AppendLine("");
                
                for (int i = 0; i < _stuckEvents.Count; i++)
                {
                    var evt = _stuckEvents[i];
                    report.AppendLine($"Event #{i + 1}:");
                    report.AppendLine($"  Time: {evt.TimeStuck:F1}s");
                    report.AppendLine($"  Position: ({evt.X:F0}, {evt.Y:F0})");
                    report.AppendLine($"  State: {evt.State}");
                    report.AppendLine($"  Reason: {evt.Reason}");
                    report.AppendLine("");
                }
            }
            
            report.AppendLine("═══════════════════════════════════════════════════════════════");
            return report.ToString();
        }

        /// <summary>
        /// Check if level took too long (suggests stuck)
        /// </summary>
        public bool LevelTakingTooLong(float timeInLevel)
        {
            // If we've been in a level for more than 50 seconds and made less than 3000 pixels
            // progress, we're probably stuck
            return timeInLevel > 50f && _stuckEvents.Count > 0;
        }
    }
}
