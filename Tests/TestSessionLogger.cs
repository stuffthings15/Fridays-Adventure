using System;
using System.IO;
using System.Text;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// PHASE 2 - Team 10: Engine Programmer
    /// Feature: Test Session File Logger
    /// Purpose: Saves all test debug information to timestamped log files for QA review
    /// 
    /// Creates logs in: Logs/TestSessions/[TIMESTAMP]/
    /// Each test gets its own detailed log file
    /// </summary>
    public class TestSessionLogger
    {
        private string _sessionId;
        private string _logDirectory;
        private string _mainLogPath;
        private StreamWriter _mainLogWriter;
        private bool _isInitialized = false;

        public TestSessionLogger()
        {
            InitializeSession();
        }

        /// <summary>
        /// Initialize a new test session with timestamped directory
        /// </summary>
        public void InitializeSession()
        {
            // Use absolute path based on BaseDirectory so logs work in F5 debugging
            // (working directory may differ from executable location)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Create timestamped session folder with absolute path
            _sessionId = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _logDirectory = Path.Combine(baseDir, "Logs", "TestSessions", _sessionId);

            try
            {
                // Create directories if they don't exist
                Directory.CreateDirectory(_logDirectory);

                // Create main session log file
                _mainLogPath = Path.Combine(_logDirectory, "SESSION_LOG.txt");
                _mainLogWriter = new StreamWriter(_mainLogPath, false, Encoding.UTF8);
                _mainLogWriter.AutoFlush = true;

                _isInitialized = true;

                // Write session header
                WriteLine("═══════════════════════════════════════════════════════════════");
                WriteLine("FRIDAY'S ADVENTURE - AUTOMATED TEST SESSION LOG");
                WriteLine("═══════════════════════════════════════════════════════════════");
                WriteLine($"Session ID: {_sessionId}");
                WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                WriteLine($"Working Directory: {Environment.CurrentDirectory}");
                WriteLine($"Base Directory: {baseDir}");
                WriteLine($"Log Directory: {_logDirectory}");
                WriteLine("───────────────────────────────────────────────────────────────");
                WriteLine("");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize test logger: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Write a line to the session log
        /// </summary>
        public void WriteLine(string message)
        {
            if (!_isInitialized) return;
            
            try
            {
                _mainLogWriter?.WriteLine(message);
                // Also output to debug window
                System.Diagnostics.Debug.WriteLine($"[LOG] {message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log write error: {ex.Message}");
            }
        }

        /// <summary>
        /// Write blank line
        /// </summary>
        public void WriteBlankLine()
        {
            if (!_isInitialized) return;
            _mainLogWriter?.WriteLine("");
        }

        /// <summary>
        /// Create a level-specific log file
        /// </summary>
        public string CreateLevelLogFile(string levelId, string levelName)
        {
            if (!_isInitialized) return null;
            
            try
            {
                string levelLogPath = Path.Combine(_logDirectory, $"LEVEL_{levelId}.txt");
                
                using (var writer = new StreamWriter(levelLogPath, false, Encoding.UTF8))
                {
                    writer.WriteLine("═══════════════════════════════════════════════════════════════");
                    writer.WriteLine($"LEVEL TEST LOG: {levelName}");
                    writer.WriteLine("═══════════════════════════════════════════════════════════════");
                    writer.WriteLine($"Level ID: {levelId}");
                    writer.WriteLine($"Test Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine("───────────────────────────────────────────────────────────────");
                    writer.WriteLine("");
                }
                
                return levelLogPath;
            }
            catch (Exception ex)
            {
                WriteLine($"ERROR creating level log: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Log test result for a level
        /// </summary>
        public void LogLevelResult(string levelId, LevelAutoTestResult result)
        {
            if (!_isInitialized) return;
            
            try
            {
                string levelLogPath = Path.Combine(_logDirectory, $"LEVEL_{levelId}.txt");
                
                using (var writer = new StreamWriter(levelLogPath, true, Encoding.UTF8))
                {
                    writer.WriteLine($"[TEST RESULT]");
                    writer.WriteLine($"  Status: {(result.IsBeatable ? "✅ BEATABLE" : "❌ NOT BEATABLE")}");
                    writer.WriteLine($"  Time to Complete: {result.TimeToComplete:F1}s");
                    writer.WriteLine($"  Distance Traveled: {result.BotData.DistanceTraveled:F0}px");
                    writer.WriteLine($"  Items Collected: {result.ItemsCollected}");
                    writer.WriteLine($"  Enemies Defeated: {result.EnemiesDefeated}");
                    
                    if (!string.IsNullOrEmpty(result.FailureReason))
                    {
                        writer.WriteLine($"  Failure Reason: {result.FailureReason}");
                    }
                    
                    writer.WriteLine($"  Test Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine("───────────────────────────────────────────────────────────────");
                    writer.WriteLine("");
                }
            }
            catch (Exception ex)
            {
                WriteLine($"ERROR logging level result: {ex.Message}");
            }
        }

        /// <summary>
        /// Log a test frame with detailed diagnostic info
        /// </summary>
        public void LogTestFrame(string levelId, int frameNumber, float time, AutoTestBot bot)
        {
            if (!_isInitialized) return;
            
            try
            {
                string levelLogPath = Path.Combine(_logDirectory, $"LEVEL_{levelId}.txt");
                
                // Only log every 10th frame to avoid huge files
                if (frameNumber % 10 != 0) return;
                
                using (var writer = new StreamWriter(levelLogPath, true, Encoding.UTF8))
                {
                    writer.WriteLine($"[FRAME {frameNumber:000}] T={time:F2}s");
                    writer.WriteLine($"  Bot Position: X={bot.BotX:F0} Y={bot.BotY:F0}");
                    writer.WriteLine($"  State: {bot.State}");
                    writer.WriteLine($"  Actions: Jump={bot.ShouldJump} | Attack={bot.ShouldAttack} | Move={bot.MoveRight}");
                    writer.WriteLine($"  Progress: Distance={bot.DistanceTraveled:F0}px | Items={bot.ItemsCollected} | Enemies={bot.EnemiesDefeated}");
                    writer.WriteLine("");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR logging frame: {ex.Message}");
            }
        }

        /// <summary>
        /// Log test summary
        /// </summary>
        public void LogTestSummary(int totalTests, int beatableCount, int unbeatableCount)
        {
            if (!_isInitialized) return;
            
            try
            {
                WriteLine("");
                WriteLine("═══════════════════════════════════════════════════════════════");
                WriteLine("TEST SESSION SUMMARY");
                WriteLine("═══════════════════════════════════════════════════════════════");
                WriteLine($"Total Levels Tested: {totalTests}");
                WriteLine($"✅ Beatable: {beatableCount}");
                WriteLine($"❌ Not Beatable: {unbeatableCount}");
                WriteLine($"Success Rate: {(beatableCount * 100f / totalTests):F1}%");
                WriteLine($"Session Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                WriteLine("═══════════════════════════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR logging summary: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a detailed results CSV file for spreadsheet analysis
        /// </summary>
        public void CreateResultsCSV(System.Collections.Generic.List<LevelAutoTestResult> results)
        {
            if (!_isInitialized) return;
            
            try
            {
                string csvPath = Path.Combine(_logDirectory, "TEST_RESULTS.csv");
                
                using (var writer = new StreamWriter(csvPath, false, Encoding.UTF8))
                {
                    // Write CSV header
                    writer.WriteLine("Level ID,Level Name,Beatable,Time (s),Distance (px),Items,Enemies,Failure Reason");
                    
                    // Write each result
                    foreach (var result in results)
                    {
                        string beatable = result.IsBeatable ? "YES" : "NO";
                        string reason = result.FailureReason ?? "N/A";
                        
                        writer.WriteLine($"\"{result.LevelId}\",\"{result.LevelName}\",{beatable}," +
                            $"{result.TimeToComplete:F1},{result.BotData.DistanceTraveled:F0}," +
                            $"{result.ItemsCollected},{result.EnemiesDefeated},\"{reason}\"");
                    }
                }
                
                WriteLine($"✓ Results CSV created: TEST_RESULTS.csv");
            }
            catch (Exception ex)
            {
                WriteLine($"ERROR creating results CSV: {ex.Message}");
            }
        }

        /// <summary>
        /// Finish the test session and close log file
        /// </summary>
        public void FinishSession()
        {
            if (!_isInitialized) return;
            
            try
            {
                WriteLine("");
                WriteLine("═══════════════════════════════════════════════════════════════");
                WriteLine("SESSION LOG CLOSED");
                WriteLine("═══════════════════════════════════════════════════════════════");
                
                _mainLogWriter?.Close();
                _mainLogWriter?.Dispose();
                
                System.Diagnostics.Debug.WriteLine($"Test logs saved to: {_logDirectory}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR closing session: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the log directory path for this session
        /// </summary>
        public string GetLogDirectory() => _logDirectory;

        /// <summary>
        /// Get the session ID
        /// </summary>
        public string GetSessionId() => _sessionId;
    }
}
