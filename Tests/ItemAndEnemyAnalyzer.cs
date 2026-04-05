using System;
using System.Collections.Generic;
using System.Linq;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// PHASE 2 - Team 10: Engine Programmer
    /// Feature: Item and Enemy Analysis System
    /// Purpose: Tracks item collectibility and enemy encounters for detailed analysis
    /// </summary>
    public class ItemAndEnemyAnalyzer
    {
        /// <summary>
        /// Item encountered during testing
        /// </summary>
        public struct ItemEncounter
        {
            public float X { get; set; }
            public float Y { get; set; }
            public string ItemType { get; set; }
            public float TimeEncountered { get; set; }
            public bool WasCollected { get; set; }
            public string Reason { get; set; }  // Why it wasn't collected if applicable
        }

        /// <summary>
        /// Enemy encountered during testing
        /// </summary>
        public struct EnemyEncounter
        {
            public float X { get; set; }
            public float Y { get; set; }
            public string EnemyType { get; set; }
            public float TimeEncountered { get; set; }
            public bool WasDefeated { get; set; }
            public string Reason { get; set; }  // Why it wasn't defeated if applicable
        }

        private List<ItemEncounter> _itemsEncountered = new List<ItemEncounter>();
        private List<EnemyEncounter> _enemiesEncountered = new List<EnemyEncounter>();
        private int _totalItemsAvailable = 0;
        private int _totalEnemiesAvailable = 0;

        /// <summary>
        /// Log an item encounter
        /// </summary>
        public void LogItemEncounter(float x, float y, string itemType, float timeInLevel, bool collected, string reason = "")
        {
            _itemsEncountered.Add(new ItemEncounter
            {
                X = x,
                Y = y,
                ItemType = itemType,
                TimeEncountered = timeInLevel,
                WasCollected = collected,
                Reason = reason
            });
        }

        /// <summary>
        /// Log an enemy encounter
        /// </summary>
        public void LogEnemyEncounter(float x, float y, string enemyType, float timeInLevel, bool defeated, string reason = "")
        {
            _enemiesEncountered.Add(new EnemyEncounter
            {
                X = x,
                Y = y,
                EnemyType = enemyType,
                TimeEncountered = timeInLevel,
                WasDefeated = defeated,
                Reason = reason
            });
        }

        /// <summary>
        /// Set total items available in level (for comparison)
        /// </summary>
        public void SetTotalItemsAvailable(int count) => _totalItemsAvailable = count;

        /// <summary>
        /// Set total enemies available in level (for comparison)
        /// </summary>
        public void SetTotalEnemiesAvailable(int count) => _totalEnemiesAvailable = count;

        /// <summary>
        /// Get item collectibility report
        /// </summary>
        public string GenerateItemReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("\n" + new string('═', 60));
            report.AppendLine("ITEM COLLECTIBILITY ANALYSIS");
            report.AppendLine(new string('═', 60));

            int collectedCount = _itemsEncountered.Count(i => i.WasCollected);
            int totalEncountered = _itemsEncountered.Count;

            report.AppendLine($"\nItems Encountered: {totalEncountered}");
            report.AppendLine($"Items Collected: {collectedCount}");
            report.AppendLine($"Collectibility Rate: {(totalEncountered > 0 ? (collectedCount * 100f / totalEncountered) : 0):F1}%");
            report.AppendLine($"Total Available in Level: {_totalItemsAvailable}");
            report.AppendLine($"Items Not Found: {_totalItemsAvailable - totalEncountered}");

            if (_itemsEncountered.Count > 0)
            {
                report.AppendLine("\n" + new string('─', 60));
                report.AppendLine("ITEM DETAILS");
                report.AppendLine(new string('─', 60));

                // Group by type
                var groupedByType = _itemsEncountered.GroupBy(i => i.ItemType);
                foreach (var group in groupedByType)
                {
                    int groupCollected = group.Count(i => i.WasCollected);
                    report.AppendLine($"\n{group.Key}:");
                    report.AppendLine($"  Encountered: {group.Count()} | Collected: {groupCollected}");

                    foreach (var item in group)
                    {
                        string status = item.WasCollected ? "✅ COLLECTED" : "❌ NOT COLLECTED";
                        report.AppendLine($"    [{status}] Position: ({item.X:F0}, {item.Y:F0}) | Time: {item.TimeEncountered:F1}s");
                        if (!item.WasCollected && !string.IsNullOrEmpty(item.Reason))
                            report.AppendLine($"      Reason: {item.Reason}");
                    }
                }

                // Non-collectible items report
                var nonCollected = _itemsEncountered.Where(i => !i.WasCollected).ToList();
                if (nonCollected.Count > 0)
                {
                    report.AppendLine("\n" + new string('─', 60));
                    report.AppendLine("ISSUES & RECOMMENDATIONS");
                    report.AppendLine(new string('─', 60));

                    foreach (var item in nonCollected)
                    {
                        report.AppendLine($"\n⚠️ {item.ItemType} at ({item.X:F0}, {item.Y:F0}) NOT COLLECTIBLE");
                        report.AppendLine($"   Encountered at: {item.TimeEncountered:F1}s");

                        if (!string.IsNullOrEmpty(item.Reason))
                        {
                            report.AppendLine($"   Issue: {item.Reason}");
                        }

                        // Suggest fixes based on common issues
                        report.AppendLine($"\n   SUGGESTED FIXES:");
                        report.AppendLine($"   1. Verify item collision box at position ({item.X:F0}, {item.Y:F0})");
                        report.AppendLine($"   2. Check if item is behind obstacles or off-screen");
                        report.AppendLine($"   3. Ensure item collection trigger is enabled");
                        report.AppendLine($"   4. Test manual collection at this location");
                    }
                }
            }

            report.AppendLine("\n" + new string('═', 60) + "\n");
            return report.ToString();
        }

        /// <summary>
        /// Get enemy defeat report
        /// </summary>
        public string GenerateEnemyReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("\n" + new string('═', 60));
            report.AppendLine("ENEMY DEFEAT ANALYSIS");
            report.AppendLine(new string('═', 60));

            int defeatedCount = _enemiesEncountered.Count(e => e.WasDefeated);
            int totalEncountered = _enemiesEncountered.Count;

            report.AppendLine($"\nEnemies Encountered: {totalEncountered}");
            report.AppendLine($"Enemies Defeated: {defeatedCount}");
            report.AppendLine($"Defeat Rate: {(totalEncountered > 0 ? (defeatedCount * 100f / totalEncountered) : 0):F1}%");
            report.AppendLine($"Total Available in Level: {_totalEnemiesAvailable}");
            report.AppendLine($"Enemies Not Found: {_totalEnemiesAvailable - totalEncountered}");

            if (_enemiesEncountered.Count > 0)
            {
                report.AppendLine("\n" + new string('─', 60));
                report.AppendLine("ENEMY DETAILS");
                report.AppendLine(new string('─', 60));

                // Group by type
                var groupedByType = _enemiesEncountered.GroupBy(e => e.EnemyType);
                foreach (var group in groupedByType)
                {
                    int groupDefeated = group.Count(e => e.WasDefeated);
                    report.AppendLine($"\n{group.Key}:");
                    report.AppendLine($"  Encountered: {group.Count()} | Defeated: {groupDefeated}");

                    foreach (var enemy in group)
                    {
                        string status = enemy.WasDefeated ? "✅ DEFEATED" : "❌ NOT DEFEATED";
                        report.AppendLine($"    [{status}] Position: ({enemy.X:F0}, {enemy.Y:F0}) | Time: {enemy.TimeEncountered:F1}s");
                        if (!enemy.WasDefeated && !string.IsNullOrEmpty(enemy.Reason))
                            report.AppendLine($"      Reason: {enemy.Reason}");
                    }
                }

                // Undefeated enemies report
                var undefeated = _enemiesEncountered.Where(e => !e.WasDefeated).ToList();
                if (undefeated.Count > 0)
                {
                    report.AppendLine("\n" + new string('─', 60));
                    report.AppendLine("COMBAT ISSUES & RECOMMENDATIONS");
                    report.AppendLine(new string('─', 60));

                    foreach (var enemy in undefeated)
                    {
                        report.AppendLine($"\n⚠️ {enemy.EnemyType} at ({enemy.X:F0}, {enemy.Y:F0}) NOT DEFEATED");
                        report.AppendLine($"   Encountered at: {enemy.TimeEncountered:F1}s");

                        if (!string.IsNullOrEmpty(enemy.Reason))
                        {
                            report.AppendLine($"   Issue: {enemy.Reason}");
                        }

                        report.AppendLine($"\n   SUGGESTED FIXES:");
                        report.AppendLine($"   1. Verify enemy AI behavior at ({enemy.X:F0}, {enemy.Y:F0})");
                        report.AppendLine($"   2. Check if enemy has proper hitbox setup");
                        report.AppendLine($"   3. Verify bot attack ability reaches this enemy");
                        report.AppendLine($"   4. Test combat mechanics manually at this location");
                        report.AppendLine($"   5. Check if enemy has invulnerability frames");
                    }
                }
            }

            report.AppendLine("\n" + new string('═', 60) + "\n");
            return report.ToString();
        }

        /// <summary>
        /// Get comprehensive analysis report
        /// </summary>
        public string GenerateComprehensiveReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("\n╔" + new string('═', 58) + "╗");
            report.AppendLine("║" + new string(' ', 10) + "COMPREHENSIVE ITEM & ENEMY ANALYSIS" + new string(' ', 14) + "║");
            report.AppendLine("╚" + new string('═', 58) + "╝");

            report.Append(GenerateItemReport());
            report.Append(GenerateEnemyReport());

            // Summary statistics
            report.AppendLine(new string('═', 60));
            report.AppendLine("SUMMARY STATISTICS");
            report.AppendLine(new string('═', 60));
            report.AppendLine($"Total Items Found: {_itemsEncountered.Count} / {_totalItemsAvailable}");
            report.AppendLine($"Total Enemies Found: {_enemiesEncountered.Count} / {_totalEnemiesAvailable}");
            report.AppendLine($"Total Items Collected: {_itemsEncountered.Count(i => i.WasCollected)}");
            report.AppendLine($"Total Enemies Defeated: {_enemiesEncountered.Count(e => e.WasDefeated)}");
            report.AppendLine(new string('═', 60));

            return report.ToString();
        }

        /// <summary>
        /// Get all item encounters
        /// </summary>
        public List<ItemEncounter> GetAllItems() => new List<ItemEncounter>(_itemsEncountered);

        /// <summary>
        /// Get all enemy encounters
        /// </summary>
        public List<EnemyEncounter> GetAllEnemies() => new List<EnemyEncounter>(_enemiesEncountered);

        /// <summary>
        /// Get collectibility percentage
        /// </summary>
        public float GetItemCollectibilityPercentage()
        {
            if (_itemsEncountered.Count == 0) return 0f;
            return (_itemsEncountered.Count(i => i.WasCollected) * 100f) / _itemsEncountered.Count;
        }

        /// <summary>
        /// Get enemy defeat percentage
        /// </summary>
        public float GetEnemyDefeatPercentage()
        {
            if (_enemiesEncountered.Count == 0) return 0f;
            return (_enemiesEncountered.Count(e => e.WasDefeated) * 100f) / _enemiesEncountered.Count;
        }
    }
}
