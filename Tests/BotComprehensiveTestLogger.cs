using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fridays_Adventure.Tests
{
    // ────────────────────────────────────────────
    // PHASE 2 - Team 10: Engine Programmer
    // Feature: Comprehensive Bot Test Logger
    // Purpose: Captures all bot events, diagnoses failures, and generates actionable
    //          reports showing what went wrong and why the bot didn't complete the game.
    // ────────────────────────────────────────────

    /// <summary>
    /// Comprehensive diagnostic system that:
    /// - Captures all bot input, abilities, pickups, enemies, scene transitions
    /// - Identifies failures (stuck states, unresponsive systems, timeout)
    /// - Auto-detects issues (missing pickup collection, CardRoulette hang, etc.)
    /// - Generates detailed reports with actionable fixes
    /// - Logs to file + console for analysis
    /// </summary>
    public sealed class BotComprehensiveTestLogger
    {
        private readonly List<BotEvent> _events = new List<BotEvent>();
        private readonly string _levelName;
        private readonly string _levelId;
        private float _elapsedTime = 0f;

        // ── Event types ────────────────────────────────────────────────
        public class BotEvent
        {
            public float Time;
            public string Category;        // INPUT, ABILITY, PICKUP, ENEMY, SCENE, STATE, HEALTH
            public string EventType;       // Specific event
            public string Details;         // Additional info
            public bool IsFailure;         // Marks error states
        }

        public BotComprehensiveTestLogger(string levelId, string levelName)
        {
            _levelId = levelId;
            _levelName = levelName;
        }

        public void Update(float dt) => _elapsedTime += dt;

        // ── Event recording ────────────────────────────────────────────

        public void LogAttack(bool success, string reason = "")
        {
            _events.Add(new BotEvent
            {
                Time = _elapsedTime,
                Category = "ABILITY",
                EventType = success ? "ATTACK_FIRED" : "ATTACK_BLOCKED",
                Details = reason,
                IsFailure = !success
            });
        }

        public void LogJump(bool success, string reason = "")
        {
            _events.Add(new BotEvent
            {
                Time = _elapsedTime,
                Category = "ABILITY",
                EventType = success ? "JUMP" : "JUMP_FAILED",
                Details = reason,
                IsFailure = !success
            });
        }

        public void LogFrostBall(bool success, string reason = "")
        {
            _events.Add(new BotEvent
            {
                Time = _elapsedTime,
                Category = "ABILITY",
                EventType = success ? "FROST_BALL" : "FROST_BALL_BLOCKED",
                Details = reason,
                IsFailure = !success
            });
        }

        public void LogPickupCollected(string pickupType, float value)
        {
            _events.Add(new BotEvent
            {
                Time = _elapsedTime,
                Category = "PICKUP",
                EventType = pickupType.ToUpper(),
                Details = $"Value: {value}",
                IsFailure = false
            });
        }

        public void LogPickupMissed(string pickupType, string reason = "")
        {
            _events.Add(new BotEvent
            {
                Time = _elapsedTime,
                Category = "PICKUP",
                EventType = $"{pickupType}_MISSED",
                Details = reason,
                IsFailure = true
            });
        }

        public void LogEnemyDefeated(string enemyType)
        {
            _events.Add(new BotEvent
            {
                Time = _elapsedTime,
                Category = "ENEMY",
                EventType = "DEFEATED",
                Details = enemyType,
                IsFailure = false
            });
        }

        public void LogSceneTransition(string fromScene, string toScene)
        {
            _events.Add(new BotEvent
            {
                Time = _elapsedTime,
                Category = "SCENE",
                EventType = "TRANSITION",
                Details = $"{fromScene} → {toScene}",
                IsFailure = false
            });
        }

        public void LogCardRouletteEnter()
        {
            _events.Add(new BotEvent
            {
                Time = _elapsedTime,
                Category = "MINIGAME",
                EventType = "CARD_ROULETTE_START",
                Details = "",
                IsFailure = false
            });
        }

        public void LogCardRouletteSelect(int cardNumber)
        {
            _events.Add(new BotEvent
            {
                Time = _elapsedTime,
                Category = "MINIGAME",
                EventType = "CARD_SELECTED",
                Details = $"Card {cardNumber}",
                IsFailure = false
            });
        }

        public void LogCardRouletteComplete()
        {
            _events.Add(new BotEvent
            {
                Time = _elapsedTime,
                Category = "MINIGAME",
                EventType = "CARD_ROULETTE_COMPLETE",
                Details = "",
                IsFailure = false
            });
        }

        public void LogHealthStatus(int current, int max)
        {
            _events.Add(new BotEvent
            {
                Time = _elapsedTime,
                Category = "HEALTH",
                EventType = "STATUS",
                Details = $"{current}/{max}",
                IsFailure = current < max / 2  // Flag if hurt
            });
        }

        public void LogTimeout()
        {
            _events.Add(new BotEvent
            {
                Time = _elapsedTime,
                Category = "STATE",
                EventType = "TIMEOUT",
                Details = "Level timeout reached",
                IsFailure = true
            });
        }

        public void LogLevelComplete()
        {
            _events.Add(new BotEvent
            {
                Time = _elapsedTime,
                Category = "STATE",
                EventType = "LEVEL_COMPLETE",
                Details = "",
                IsFailure = false
            });
        }

        // ── Report generation ─────────────────────────────────────────

        public string GenerateComprehensiveReport(bool levelCompleted)
        {
            var report = new StringBuilder();

            report.AppendLine("\n╔════════════════════════════════════════════════════════════╗");
            report.AppendLine("║         COMPREHENSIVE BOT TEST DIAGNOSTIC REPORT            ║");
            report.AppendLine("╚════════════════════════════════════════════════════════════╝\n");

            // Summary
            report.AppendLine($"LEVEL: {_levelName} ({_levelId})");
            report.AppendLine($"COMPLETION: {(levelCompleted ? "✅ PASSED" : "❌ FAILED")}");
            report.AppendLine($"TIME SPENT: {_elapsedTime:F1}s");
            report.AppendLine($"TOTAL EVENTS: {_events.Count}\n");

            // Event statistics
            var attackEvents = _events.Where(e => e.Category == "ABILITY" && e.EventType.Contains("ATTACK")).ToList();
            var jumpEvents = _events.Where(e => e.Category == "ABILITY" && e.EventType.Contains("JUMP")).ToList();
            var pickupEvents = _events.Where(e => e.Category == "PICKUP").ToList();
            var enemyEvents = _events.Where(e => e.Category == "ENEMY").ToList();
            var minigameEvents = _events.Where(e => e.Category == "MINIGAME").ToList();
            var failureEvents = _events.Where(e => e.IsFailure).ToList();

            report.AppendLine("STATISTICS:");
            report.AppendLine($"  Attacks: {attackEvents.Count(e => e.EventType == "ATTACK_FIRED")} fired, {attackEvents.Count(e => e.EventType == "ATTACK_BLOCKED")} blocked");
            report.AppendLine($"  Jumps: {jumpEvents.Count}");
            report.AppendLine($"  Pickups Collected: {pickupEvents.Count(e => !e.EventType.EndsWith("_MISSED"))}");
            report.AppendLine($"  Pickups Missed: {pickupEvents.Count(e => e.EventType.EndsWith("_MISSED"))}");
            report.AppendLine($"  Enemies Defeated: {enemyEvents.Count}");
            report.AppendLine($"  Minigames: {minigameEvents.Count}");
            report.AppendLine($"  FAILURES: {failureEvents.Count}\n");

            // Issues detected
            report.AppendLine("ISSUES DETECTED:");
            report.AppendLine("─────────────────────────────────────────────────────────────");

            if (attackEvents.Count(e => e.EventType == "ATTACK_BLOCKED") > attackEvents.Count(e => e.EventType == "ATTACK_FIRED") * 2)
                report.AppendLine("⚠️  CRITICAL: Attack ability blocked too frequently - check cooldown");

            if (pickupEvents.Count(e => e.EventType.EndsWith("_MISSED")) > 0)
                report.AppendLine($"⚠️  CONCERN: {pickupEvents.Count(e => e.EventType.EndsWith("_MISSED"))} pickups missed - collision detection issue?");

            if (minigameEvents.Count == 0 && !levelCompleted)
                report.AppendLine("⚠️  ISSUE: No minigame interactions detected - CardRoulette or result screens not responding");

            var cardSelectEvents = minigameEvents.Where(e => e.EventType == "CARD_SELECTED").ToList();
            if (cardSelectEvents.Count == 0 && minigameEvents.Any(e => e.EventType == "CARD_ROULETTE_START"))
                report.AppendLine("⚠️  CRITICAL: CardRoulette started but no cards selected - input not working!");

            var blockingEvents = _events.Where(e => e.Category == "MINIGAME" && !e.EventType.EndsWith("_COMPLETE")).ToList();
            if (blockingEvents.Count > 0 && _elapsedTime > 30f)
                report.AppendLine($"⚠️  CRITICAL: Bot stuck in minigame for {_elapsedTime - blockingEvents.First().Time:F1}s");

            if (attackEvents.Count == 0)
                report.AppendLine("⚠️  CRITICAL: No attacks fired at all - attack system may be offline");

            if (!levelCompleted && _elapsedTime >= 90f)
                report.AppendLine("⚠️  FAILURE: Level timeout - bot either stuck or level too difficult");

            report.AppendLine();

            // Detailed timeline
            report.AppendLine("DETAILED TIMELINE:");
            report.AppendLine("─────────────────────────────────────────────────────────────");
            foreach (var evt in _events)
            {
                string failureMarker = evt.IsFailure ? "❌" : "  ";
                string line = $"{failureMarker} [{evt.Time:F2}s] {evt.Category,-12} {evt.EventType,-20} {evt.Details}";
                report.AppendLine(line);
            }

            report.AppendLine("\nAUTOMATIC RECOMMENDATIONS:");
            report.AppendLine("─────────────────────────────────────────────────────────────");

            if (attackEvents.Count(e => e.EventType == "ATTACK_BLOCKED") > 5)
                report.AppendLine("→ Check Character.TryAttack() cooldown - may be too restrictive");

            if (pickupEvents.Count(e => e.EventType.EndsWith("_MISSED")) > 2)
                report.AppendLine("→ Verify Hitbox collision detection in UpdateBerries() and UpdateHealthPickups()");

            if (cardSelectEvents.Count == 0 && minigameEvents.Any())
                report.AppendLine("→ CardRoulette input handler broken - verify InteractPressed check in CardRouletteScene.Update()");

            if (enemyEvents.Count == 0)
                report.AppendLine("→ No enemies defeated - check attack hit detection or enemy spawning");

            report.AppendLine("\n" + new string('═', 60));

            return report.ToString();
        }

        /// <summary>
        /// Save report to file for later analysis.
        /// </summary>
        public void SaveReport(bool levelCompleted)
        {
            string reportText = GenerateComprehensiveReport(levelCompleted);
            
            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "bot-tests");
                Directory.CreateDirectory(logDir);

                string fileName = $"bot-test-{_levelId}-{DateTime.Now:yyyy-MM-dd-HHmmss}.txt";
                string filePath = Path.Combine(logDir, fileName);

                File.WriteAllText(filePath, reportText);
                Console.WriteLine($"📝 Report saved: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to save report: {ex.Message}");
            }
        }
    }
}
