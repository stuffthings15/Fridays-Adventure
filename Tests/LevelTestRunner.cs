using System;
using System.Collections.Generic;
using Fridays_Adventure.Tests;

namespace Fridays_Adventure
{
    /// <summary>
    /// Test Runner for Level Beatability Verification
    /// Run this from Main() to verify all levels are beatable
    /// </summary>
    public static class LevelTestRunner
    {
        /// <summary>
        /// Execute all level beatability tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.Clear();
            Console.WriteLine("\n");
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                                                            ║");
            Console.WriteLine("║        FRIDAYS ADVENTURE - LEVEL BEATABILITY TEST          ║");
            Console.WriteLine("║                                                            ║");
            Console.WriteLine("║        Testing all 17 levels for completability           ║");
            Console.WriteLine("║                                                            ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine("\n");

            var results = LevelBeatabilityTest.RunBeatabilityTest();
            
            Console.WriteLine("\nPress any key to exit test...");
            Console.ReadKey();
        }

        /// <summary>
        /// Get test results summary for specific level
        /// </summary>
        public static string GetLevelTestStatus(string levelId)
        {
            var result = LevelBeatabilityTest.RunBeatabilityTest();
            var levelResult = result.Find(r => r.LevelId == levelId);
            
            if (levelResult == null)
                return "Level not found";
            
            return levelResult.IsBeatableResult ? "✅ BEATABLE" : "❌ HAS ISSUES";
        }
    }
}
