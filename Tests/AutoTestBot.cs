using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// PHASE 2 - Team 10: Engine Programmer
    /// Feature: Automated Level Beatability Test Bot
    /// Purpose: AI-driven bot that tests each level by simulating player behavior
    /// </summary>
    public class AutoTestBot
    {
        /// <summary>
        /// Bot state during level testing
        /// </summary>
        public enum BotState
        {
            Idle,
            Running,
            Jumping,
            Collecting,
            Fighting,
            WonLevel,
            Failed
        }

        /// <summary>
        /// Current state of the bot
        /// </summary>
        public BotState State { get; set; } = BotState.Idle;

        /// <summary>
        /// Has this level been completed successfully
        /// </summary>
        public bool LevelCompleted { get; set; } = false;

        /// <summary>
        /// Time spent in this level
        /// </summary>
        public float TimeInLevel { get; set; } = 0f;

        /// <summary>
        /// Maximum time allowed per level (60 seconds)
        /// </summary>
        private const float MAX_TIME_PER_LEVEL = 60f;

        /// <summary>
        /// Bot's X position
        /// </summary>
        public float BotX { get; set; } = 0f;

        /// <summary>
        /// Bot's Y position
        /// </summary>
        public float BotY { get; set; } = 0f;

        /// <summary>
        /// Items collected in this level
        /// </summary>
        public int ItemsCollected { get; set; } = 0;

        /// <summary>
        /// Enemies defeated in this level
        /// </summary>
        public int EnemiesDefeated { get; set; } = 0;

        /// <summary>
        /// Distance traveled (for progress tracking)
        /// </summary>
        public float DistanceTraveled { get; set; } = 0f;

        /// <summary>
        /// Whether bot should jump
        /// </summary>
        public bool ShouldJump { get; set; } = false;

        /// <summary>
        /// Whether bot should move right
        /// </summary>
        public bool MoveRight { get; set; } = true;

        /// <summary>
        /// Whether bot should attack
        /// </summary>
        public bool ShouldAttack { get; set; } = false;

        /// <summary>
        /// Last gap width detected (for intelligent jumping)
        /// </summary>
        public float LastGapWidth { get; set; } = 0f;

        /// <summary>
        /// Stuck detection system
        /// </summary>
        public BotStuckDetector _stuckDetector = new BotStuckDetector();

        /// <summary>
        /// Initialize bot for a new level
        /// </summary>
        public void Initialize(float startX, float startY)
        {
            BotX = startX;
            BotY = startY;
            State = BotState.Running;
            LevelCompleted = false;
            TimeInLevel = 0f;
            ItemsCollected = 0;
            EnemiesDefeated = 0;
            DistanceTraveled = 0f;
            ShouldJump = false;
            MoveRight = true;
            ShouldAttack = false;

            // Initialize stuck detector
            _stuckDetector.Initialize(startX, startY);
        }

        /// <summary>
        /// Update bot behavior each frame
        /// </summary>
        public void Update(float dt, TestSessionLogger logger = null)
        {
            TimeInLevel += dt;

            // Check for stuck before timeout
            _stuckDetector.Update(dt, this, logger);

            // If stuck for more than 3 seconds, mark level as stuck
            if (_stuckDetector.IsStuck && _stuckDetector.CurrentStuckDuration > 3f)
            {
                // Give it another 10 seconds before declaring failure
                if (_stuckDetector.CurrentStuckDuration > 13f)
                {
                    State = BotState.Failed;
                    LevelCompleted = false;
                    logger?.WriteLine($"[BOT] Level failed - bot stuck for {_stuckDetector.CurrentStuckDuration:F1}s");
                    return;
                }
            }

            // Timeout - level is too hard or unbeatable
            if (TimeInLevel > MAX_TIME_PER_LEVEL)
            {
                State = BotState.Failed;
                LevelCompleted = false;
                logger?.WriteLine($"[BOT] Level timeout after {TimeInLevel:F1}s");
                return;
            }

            // Simulate AI behavior
            UpdateAI(dt);
        }

        /// <summary>
        /// Core AI logic for the bot
        /// </summary>
        private void UpdateAI(float dt)
        {
            switch (State)
            {
                case BotState.Running:
                    SimulateMovement(dt);
                    SimulateJumping(dt);
                    SimulateAttacking(dt);
                    break;

                case BotState.WonLevel:
                    LevelCompleted = true;
                    break;

                case BotState.Failed:
                    LevelCompleted = false;
                    break;
            }
        }

        /// <summary>
        /// Simulate forward movement
        /// </summary>
        private void SimulateMovement(float dt)
        {
            // Move right at steady pace
            float moveSpeed = 150f; // pixels per second
            DistanceTraveled += moveSpeed * dt;
            BotX += moveSpeed * dt;
            MoveRight = true;
        }

        /// <summary>
        /// Simulate intelligent jumping
        /// </summary>
        private void SimulateJumping(float dt)
        {
            // Jump frequently to avoid obstacles and traverse gaps
            // Bot should jump every 0.5-1.5 seconds for platforming
            ShouldJump = (TimeInLevel % 1.2f) < 0.1f;
        }

        /// <summary>
        /// Simulate attacking enemies
        /// </summary>
        private void SimulateAttacking(float dt)
        {
            // Attack when enemies are nearby (simulated)
            // In real scenario, this would check for nearby enemies
            ShouldAttack = (TimeInLevel % 3f) < 0.2f;
        }

        /// <summary>
        /// Register item collection
        /// </summary>
        public void CollectItem()
        {
            ItemsCollected++;
        }

        /// <summary>
        /// Register enemy defeat
        /// </summary>
        public void DefeatEnemy()
        {
            EnemiesDefeated++;
        }

        /// <summary>
        /// Called when exit flag is reached
        /// </summary>
        public void ReachedExit()
        {
            State = BotState.WonLevel;
            LevelCompleted = true;
        }

        /// <summary>
        /// Get summary of bot performance
        /// </summary>
        public string GetSummary()
        {
            return $"Time: {TimeInLevel:F1}s | Distance: {DistanceTraveled:F0}px | Items: {ItemsCollected} | Enemies: {EnemiesDefeated} | Completed: {(LevelCompleted ? "✅" : "❌")}";
        }

        /// <summary>
        /// Get stuck detection report
        /// </summary>
        public string GetStuckReport()
        {
            return _stuckDetector.GetStuckReport();
        }

        /// <summary>
        /// Check if bot got stuck at any point
        /// </summary>
        public bool DidBotGetStuck()
        {
            return _stuckDetector.AllStuckEvents.Count > 0;
        }
    }

    /// <summary>
    /// Level auto-test results tracking
    /// </summary>
    public class LevelAutoTestResult
    {
        public string LevelId { get; set; }
        public string LevelName { get; set; }
        public bool IsBeatable { get; set; }
        public float TimeToComplete { get; set; }
        public int ItemsCollected { get; set; }
        public int EnemiesDefeated { get; set; }
        public string FailureReason { get; set; }
        public AutoTestBot BotData { get; set; }
    }

    /// <summary>
    /// Manages automated testing of all levels
    /// </summary>
    public class LevelAutoTestManager
    {
        /// <summary>
        /// Allocates a new console window for this process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        public static List<LevelAutoTestResult> AllResults { get; set; } = new List<LevelAutoTestResult>();

        // ── Test Session Logger ──────────────────────────────────────────
        private static TestSessionLogger _logger;

        /// <summary>
        /// Test a single level with the bot (used for live progress)
        /// </summary>
        public static LevelAutoTestResult TestLevelSingle(string levelId, string levelName, TestSessionLogger logger = null)
        {
            var bot = new AutoTestBot();
            bot.Initialize(100f, 300f); // Start position

            var result = new LevelAutoTestResult
            {
                LevelId = levelId,
                LevelName = levelName,
                BotData = bot
            };

            try
            {
                // Simulate bot running through level for 60 seconds
                for (float time = 0f; time < 60f; time += 0.016f) // ~60 FPS
                {
                    bot.Update(0.016f, logger);

                    // Simulate random events
                    SimulateLevelEvents(bot, time, levelId);

                    // Check if bot reached exit
                    if (bot.State == AutoTestBot.BotState.WonLevel)
                    {
                        result.IsBeatable = true;
                        result.TimeToComplete = bot.TimeInLevel;
                        result.ItemsCollected = bot.ItemsCollected;
                        result.EnemiesDefeated = bot.EnemiesDefeated;
                        break;
                    }

                    // Check timeout
                    if (bot.TimeInLevel >= 60f)
                    {
                        result.IsBeatable = false;
                        result.FailureReason = "Timeout - Level took too long";
                        break;
                    }
                }

                // If bot made no progress, level is likely unbeatable
                if (bot.DistanceTraveled < 50f)
                {
                    result.IsBeatable = false;
                    result.FailureReason = "Bot made insufficient progress";
                }

                // Check if bot got stuck
                if (bot.DidBotGetStuck())
                {
                    result.FailureReason = "Bot got stuck during level";
                    logger?.WriteLine(bot.GetStuckReport());
                }
            }
            catch (Exception ex)
            {
                result.IsBeatable = false;
                result.FailureReason = $"Exception: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Run automated tests on all 17 levels (blocking)
        /// </summary>
        public static void RunAllTests()
        {
            // Initialize file logger
            _logger = new TestSessionLogger();

            // Allocate console window for WinExe application
            // Fixes "The handle is invalid" IOException when calling Console.Clear()
            try
            {
                AllocConsole();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to allocate console: {ex.Message}");
            }

            Console.Clear();
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                                                            ║");
            Console.WriteLine("║     FRIDAYS ADVENTURE - AUTOMATED LEVEL BEATABILITY TEST   ║");
            Console.WriteLine("║                                                            ║");
            Console.WriteLine("║        AI Bot Testing All 17 Levels                        ║");
            Console.WriteLine("║                                                            ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

            _logger.WriteLine("Starting automated test run of all 17 levels...");
            _logger.WriteBlankLine();

            // Test each level
            string[] levelIds = {
                "dino", "storm1", "sky", "blockade", "wano", "warlord1",
                "harbor", "coral", "tundra", "storm2", "warlord2",
                "dive_gate", "sunken_gate", "kelp", "boiling_vent", "abyss", "centipede_final"
            };

            string[] levelNames = {
                "1. Dinosaur Island", "2. Storm Belt", "3. Sky Island", "4. Marine Blockade",
                "5. Blade Nation", "6. Warlord: Sudo", "7. Harbor Town", "8. Coral Reef",
                "9. Tundra Peak", "10. Tempest Strait", "11. Warlord: Vanta", "12. Dive Gate",
                "13. Sunken Gate", "14. Kelp Maze", "15. Vent Ruins", "16. Abyss", "17. Centipede Boss"
            };

            AllResults.Clear();

            for (int i = 0; i < levelIds.Length; i++)
            {
                string testMessage = $"[{i + 1}/{levelIds.Length}] Testing: {levelNames[i]}...";
                Console.WriteLine(testMessage);
                _logger.WriteLine(testMessage);

                var result = TestLevel(levelIds[i], levelNames[i]);
                AllResults.Add(result);

                string status = result.IsBeatable ? "✅ BEATABLE" : "❌ NOT BEATABLE";
                Console.WriteLine($"        Status: {status}");
                _logger.WriteLine($"Status: {status}");

                Console.WriteLine($"        {result.BotData.GetSummary()}");
                _logger.WriteLine(result.BotData.GetSummary());

                if (!string.IsNullOrEmpty(result.FailureReason))
                {
                    Console.WriteLine($"        Issue: {result.FailureReason}");
                    _logger.WriteLine($"Issue: {result.FailureReason}");
                }
                Console.WriteLine();
                _logger.WriteBlankLine();

                // Log individual level result
                _logger.LogLevelResult(levelIds[i], result);
            }

            PrintTestSummary();

            // Create CSV for analysis
            _logger.CreateResultsCSV(AllResults);

            // Close logger
            _logger.FinishSession();

            Console.WriteLine($"\n📁 Test logs saved to: {_logger.GetLogDirectory()}");
        }

        /// <summary>
        /// Test a single level with the bot
        /// </summary>
        private static LevelAutoTestResult TestLevel(string levelId, string levelName)
        {
            var bot = new AutoTestBot();
            bot.Initialize(100f, 300f); // Start position

            var result = new LevelAutoTestResult
            {
                LevelId = levelId,
                LevelName = levelName,
                BotData = bot
            };

            try
            {
                // Simulate bot running through level for 60 seconds
                for (float time = 0f; time < 60f; time += 0.016f) // ~60 FPS
                {
                    bot.Update(0.016f, _logger);

                    // Simulate random events
                    SimulateLevelEvents(bot, time, levelId);

                    // Check if bot reached exit
                    if (bot.State == AutoTestBot.BotState.WonLevel)
                    {
                        result.IsBeatable = true;
                        result.TimeToComplete = bot.TimeInLevel;
                        result.ItemsCollected = bot.ItemsCollected;
                        result.EnemiesDefeated = bot.EnemiesDefeated;
                        break;
                    }

                    // Check timeout
                    if (bot.TimeInLevel >= 60f)
                    {
                        result.IsBeatable = false;
                        result.FailureReason = "Timeout - Level took too long";
                        break;
                    }
                }

                // If bot made no progress, level is likely unbeatable
                if (bot.DistanceTraveled < 50f)
                {
                    result.IsBeatable = false;
                    result.FailureReason = "Bot made insufficient progress";
                }

                // Check if bot got stuck
                if (bot.DidBotGetStuck())
                {
                    result.FailureReason = $"Bot got stuck during level ({bot._stuckDetector.AllStuckEvents.Count} stuck events)";
                    _logger?.WriteLine(bot.GetStuckReport());
                }
            }
            catch (Exception ex)
            {
                result.IsBeatable = false;
                result.FailureReason = $"Exception: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Simulate level events (items and enemies only).
        /// NOTE: This statistical simulation does NOT auto-complete levels.
        /// Real beatability data comes from Visual Mode (key 2), which runs
        /// the actual game scenes with the real bot player.
        /// </summary>
        private static void SimulateLevelEvents(AutoTestBot bot, float time, string levelId)
        {
            // Item collection roughly every 5 s
            if ((time % 5f) < 0.1f && time > 0)
                bot.CollectItem();

            // Enemy defeat roughly every 8 s
            if ((time % 8f) < 0.1f && time > 0)
                bot.DefeatEnemy();

            // ── NO auto-complete here ────────────────────────────────────────
            // The old code called bot.ReachedExit() whenever distance > 2000px,
            // which always triggered after ~30 s regardless of actual level layout.
            // That was a lie: every level appeared beatable without any real test.
            // Statistical mode is a rough SIMULATION ESTIMATE only.
            // Use Visual Mode (key 2) for a real beatability verdict.
        }

        /// <summary>
        /// Print summary of all test results.
        /// </summary>
        private static void PrintTestSummary()
        {
            int beatableCount   = AllResults.Count(r => r.IsBeatable);
            int unbeatableCount = AllResults.Count(r => !r.IsBeatable);

            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine("TEST SUMMARY - STATISTICAL SIMULATION (ESTIMATE ONLY)");
            Console.WriteLine("⚠  This mode simulates distance/item events only.");
            Console.WriteLine("⚠  Use Visual Mode (key 2 in-game) for real results.");
            Console.WriteLine("════════════════════════════════════════════════════════════\n");

            Console.WriteLine($"✅ Beatable (simulation): {beatableCount}/{AllResults.Count}");
            Console.WriteLine($"❌ Not beatable (simulation): {unbeatableCount}/{AllResults.Count}\n");

            // Log summary
            _logger.LogTestSummary(AllResults.Count, beatableCount, unbeatableCount);

            if (unbeatableCount > 0)
            {
                Console.WriteLine("LEVELS NEEDING ATTENTION:");
                Console.WriteLine("─────────────────────────────────────────────────────────");
                _logger.WriteLine("LEVELS NEEDING ATTENTION:");

                foreach (var result in AllResults.Where(r => !r.IsBeatable))
                {
                    Console.WriteLine($"\n{result.LevelName} ({result.LevelId})");
                    _logger.WriteLine($"\n{result.LevelName} ({result.LevelId})");

                    Console.WriteLine($"  ❌ {result.FailureReason}");
                    _logger.WriteLine($"  ❌ {result.FailureReason}");

                    Console.WriteLine($"  Distance traveled: {result.BotData.DistanceTraveled:F0}px");
                    _logger.WriteLine($"  Distance traveled: {result.BotData.DistanceTraveled:F0}px");

                    Console.WriteLine($"  Time spent: {result.BotData.TimeInLevel:F1}s");
                    _logger.WriteLine($"  Time spent: {result.BotData.TimeInLevel:F1}s");
                }
                Console.WriteLine("\n════════════════════════════════════════════════════════════");
                Console.WriteLine("⚠️  ACTION REQUIRED: Fix identified levels");
                Console.WriteLine("════════════════════════════════════════════════════════════");
            }
            else
            {
                Console.WriteLine("════════════════════════════════════════════════════════════");
                Console.WriteLine("✅ ALL LEVELS ARE BEATABLE - READY FOR RELEASE");
                Console.WriteLine("════════════════════════════════════════════════════════════");
            }

            // Show statistics
            float averageTime = AllResults.Where(r => r.IsBeatable).Average(r => r.TimeToComplete);
            Console.WriteLine($"\nAverage completion time: {averageTime:F1}s");
            _logger.WriteLine($"\nAverage completion time: {averageTime:F1}s");
        }

        /// <summary>
        /// Get results for a specific level
        /// </summary>
        public static LevelAutoTestResult GetResultForLevel(string levelId)
        {
            return AllResults.FirstOrDefault(r => r.LevelId == levelId);
        }
    }
}
