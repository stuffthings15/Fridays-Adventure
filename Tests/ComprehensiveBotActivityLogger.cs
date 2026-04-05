using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// COMPREHENSIVE BOT ACTIVITY LOGGER
    /// Logs EVERYTHING the bot does for complete transparency and debugging
    /// </summary>
    public class ComprehensiveBotActivityLogger
    {
        private string _sessionDirectory;
        private string _levelLogPath;
        private StreamWriter _levelLogWriter;
        private bool _isInitialized = false;

        private List<string> _itemsLog = new List<string>();
        private List<string> _combatLog = new List<string>();
        private List<string> _healthLog = new List<string>();
        private List<string> _platformingLog = new List<string>();

        public void InitializeForLevel(string sessionDirectory, string levelId, string levelName)
        {
            try
            {
                _sessionDirectory = sessionDirectory;
                _levelLogPath = Path.Combine(_sessionDirectory, $"BOT_ACTIVITY_{levelId}.txt");

                using (var writer = new StreamWriter(_levelLogPath, false, Encoding.UTF8))
                {
                    writer.WriteLine("═══════════════════════════════════════════════════════════════");
                    writer.WriteLine($"COMPREHENSIVE BOT ACTIVITY LOG - {levelName}");
                    writer.WriteLine("═══════════════════════════════════════════════════════════════");
                    writer.WriteLine($"Level ID: {levelId}");
                    writer.WriteLine($"Test Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine("───────────────────────────────────────────────────────────────");
                    writer.WriteLine("");
                }

                _isInitialized = true;
                _itemsLog.Clear();
                _combatLog.Clear();
                _healthLog.Clear();
                _platformingLog.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR initializing bot activity logger: {ex.Message}");
            }
        }

        public void LogItemAction(string action, string itemType, float x, float y, string reason = "")
        {
            if (!_isInitialized) return;
            
            string log = $"[ITEM] {action}: {itemType} at X={x:F0} Y={y:F0}";
            if (!string.IsNullOrEmpty(reason))
                log += $" - {reason}";
            
            _itemsLog.Add(log);
            System.Diagnostics.Debug.WriteLine(log);
        }

        public void LogCombatAction(string action, string enemyType, float distance, string detail = "")
        {
            if (!_isInitialized) return;
            
            string log = $"[COMBAT] {action}: {enemyType} at {distance:F0}px";
            if (!string.IsNullOrEmpty(detail))
                log += $" - {detail}";
            
            _combatLog.Add(log);
            System.Diagnostics.Debug.WriteLine(log);
        }

        public void LogHealthAction(string action, float currentHealth, float maxHealth, string detail = "")
        {
            if (!_isInitialized) return;
            
            string log = $"[HEALTH] {action}: {currentHealth:F0}/{maxHealth:F0}";
            if (!string.IsNullOrEmpty(detail))
                log += $" - {detail}";
            
            _healthLog.Add(log);
            System.Diagnostics.Debug.WriteLine(log);
        }

        public void LogPlatformingAction(string action, string detail = "")
        {
            if (!_isInitialized) return;
            
            string log = $"[PLATFORMING] {action}";
            if (!string.IsNullOrEmpty(detail))
                log += $" - {detail}";
            
            _platformingLog.Add(log);
            System.Diagnostics.Debug.WriteLine(log);
        }

        public void FinalizeLevelLog(int itemsCollected, int itemsMissed, int enemiesDefeated, int damagesTaken)
        {
            if (!_isInitialized) return;

            try
            {
                using (var writer = new StreamWriter(_levelLogPath, true, Encoding.UTF8))
                {
                    writer.WriteLine("═══════════════════════════════════════════════════════════════");
                    writer.WriteLine("ITEM COLLECTION LOG");
                    writer.WriteLine("═══════════════════════════════════════════════════════════════");
                    foreach (var log in _itemsLog)
                        writer.WriteLine(log);
                    writer.WriteLine($"SUMMARY: {itemsCollected} collected, {itemsMissed} missed");
                    writer.WriteLine("");

                    writer.WriteLine("═══════════════════════════════════════════════════════════════");
                    writer.WriteLine("COMBAT LOG");
                    writer.WriteLine("═══════════════════════════════════════════════════════════════");
                    foreach (var log in _combatLog)
                        writer.WriteLine(log);
                    writer.WriteLine($"SUMMARY: {enemiesDefeated} enemies defeated, {damagesTaken} times damaged");
                    writer.WriteLine("");

                    writer.WriteLine("═══════════════════════════════════════════════════════════════");
                    writer.WriteLine("HEALTH MANAGEMENT LOG");
                    writer.WriteLine("═══════════════════════════════════════════════════════════════");
                    foreach (var log in _healthLog)
                        writer.WriteLine(log);
                    writer.WriteLine("");

                    writer.WriteLine("═══════════════════════════════════════════════════════════════");
                    writer.WriteLine("PLATFORMING LOG");
                    writer.WriteLine("═══════════════════════════════════════════════════════════════");
                    foreach (var log in _platformingLog)
                        writer.WriteLine(log);
                    writer.WriteLine("");

                    writer.WriteLine("═══════════════════════════════════════════════════════════════");
                    writer.WriteLine("Test Ended: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    writer.WriteLine("═══════════════════════════════════════════════════════════════");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR finalizing bot activity log: {ex.Message}");
            }
        }
    }
}
