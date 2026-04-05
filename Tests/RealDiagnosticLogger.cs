using System;
using System.IO;
using System.Text;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// REAL Diagnostic Logger - Actually writes to files and console
    /// </summary>
    public class RealDiagnosticLogger
    {
        private string _logPath;
        private StreamWriter _writer;
        private bool _initialized = false;

        public void Initialize(string levelName)
        {
            try
            {
                // Create Logs directory
                string logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Directory.CreateDirectory(logsDir);

                // Create timestamped session dir
                string sessionDir = Path.Combine(logsDir, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
                Directory.CreateDirectory(sessionDir);

                // Create level log file
                _logPath = Path.Combine(sessionDir, $"{levelName}_diagnostic.txt");
                _writer = new StreamWriter(_logPath, false, Encoding.UTF8);
                _writer.AutoFlush = true;

                _initialized = true;

                WriteLine("═══════════════════════════════════════════════════════════════");
                WriteLine($"DIAGNOSTIC LOG - {levelName}");
                WriteLine("═══════════════════════════════════════════════════════════════");
                WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                WriteLine($"Log file: {_logPath}");
                WriteLine("───────────────────────────────────────────────────────────────");
                WriteLine("");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Diagnostic log created: {_logPath}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Failed to create diagnostic log: {ex.Message}");
                Console.ResetColor();
            }
        }

        public void LogFrame(int frameNum, float time, float playerX, float playerY, float playerHP,
            int enemyCount, float nearestEnemyDist, int pickupCount, float nearestPickupDist,
            bool isFalling, bool shouldJump, bool shouldAttack)
        {
            if (!_initialized) return;

            try
            {
                _writer.WriteLine($"[FRAME {frameNum}] T={time:F2}s");
                _writer.WriteLine($"  Player: X={playerX:F0} Y={playerY:F0} HP={playerHP:F0}");
                _writer.WriteLine($"  Enemies: {enemyCount} (nearest: {nearestEnemyDist:F0}px away)");
                _writer.WriteLine($"  Pickups: {pickupCount} (nearest: {nearestPickupDist:F0}px away)");
                _writer.WriteLine($"  Falling: {isFalling}");
                _writer.WriteLine($"  Decisions: Jump={shouldJump} Attack={shouldAttack}");
                _writer.WriteLine("");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOG ERROR] {ex.Message}");
            }
        }

        public void LogDecision(string decision, string reason)
        {
            if (!_initialized) return;

            try
            {
                _writer.WriteLine($"[DECISION] {decision}");
                _writer.WriteLine($"  Reason: {reason}");
                _writer.WriteLine("");
            }
            catch { }
        }

        public void LogError(string error)
        {
            if (!_initialized) return;

            try
            {
                _writer.WriteLine($"[ERROR] {error}");
                _writer.WriteLine("");
            }
            catch { }
        }

        public void WriteLine(string line)
        {
            if (!_initialized) return;

            try
            {
                _writer?.WriteLine(line);
            }
            catch { }
        }

        public void Close()
        {
            if (!_initialized) return;

            try
            {
                _writer?.WriteLine("");
                _writer?.WriteLine("═══════════════════════════════════════════════════════════════");
                _writer?.WriteLine($"Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _writer?.WriteLine("═══════════════════════════════════════════════════════════════");
                _writer?.Flush();
                _writer?.Close();
                _writer?.Dispose();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Diagnostic log saved: {_logPath}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR closing log] {ex.Message}");
            }
        }

        public string GetLogPath() => _logPath;
    }
}
