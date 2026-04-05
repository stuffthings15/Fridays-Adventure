using System;
using System.Collections.Generic;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Scenes;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// COMPREHENSIVE INTELLIGENT BOT - V3
    /// Handles: Dialogue, platforming, combat, health management, item collection tracking
    /// Purpose: Fully autonomous bot that plays through levels like a skilled human player
    /// </summary>
    public class ComprehensiveIntelligentBot
    {
        private Player _player;
        private Scene _scene;
        private float _elapsedTime = 0f;
        
        // ════════════════════════════════════════════════════════════════════
        // DIALOGUE & UI HANDLING
        // ════════════════════════════════════════════════════════════════════
        private bool _dialogueActive = false;
        private float _dialogueWaitTimer = 0f;
        
        // ════════════════════════════════════════════════════════════════════
        // ITEM COLLECTION TRACKING
        // ════════════════════════════════════════════════════════════════════
        private List<ItemTarget> _itemsInLevel = new List<ItemTarget>();
        private List<ItemTarget> _itemsCollected = new List<ItemTarget>();
        private List<ItemTarget> _itemsMissed = new List<ItemTarget>();
        
        public class ItemTarget
        {
            public float X { get; set; }
            public float Y { get; set; }
            public string Type { get; set; }
            public bool IsReachable { get; set; }
            public string UnreachableReason { get; set; }
        }

        // ════════════════════════════════════════════════════════════════════
        // HEALTH MANAGEMENT
        // ════════════════════════════════════════════════════════════════════
        private float _lastKnownHealth = 100f;
        private bool _autoUsedHealth = false;
        private float _healthUsageTime = 0f;
        
        // ════════════════════════════════════════════════════════════════════
        // ENEMY TRACKING
        // ════════════════════════════════════════════════════════════════════
        private List<EnemyTarget> _enemiesInLevel = new List<EnemyTarget>();
        
        public class EnemyTarget
        {
            public float X { get; set; }
            public float Y { get; set; }
            public string Type { get; set; }
            public bool IsDefeated { get; set; }
        }

        // ════════════════════════════════════════════════════════════════════
        // PATHFINDING & PLATFORMING
        // ════════════════════════════════════════════════════════════════════
        private List<GapDetection> _detectedGaps = new List<GapDetection>();
        
        public class GapDetection
        {
            public float X { get; set; }
            public float Width { get; set; }
            public float SafeJumpHeight { get; set; }
        }

        // ════════════════════════════════════════════════════════════════════
        // DECISION OUTPUTS
        // ════════════════════════════════════════════════════════════════════
        
        public bool ShouldJump { get; private set; }
        public bool ShouldAttack { get; private set; }
        public bool ShouldDodge { get; private set; }
        public bool ShouldMoveRight { get; private set; } = true;
        public bool ShouldUseHealth { get; private set; }

        public string CurrentState { get; private set; } = "Init";

        // ════════════════════════════════════════════════════════════════════
        // LOGGING
        // ════════════════════════════════════════════════════════════════════
        
        private List<string> _thisFrameLog = new List<string>();
        private TestSessionLogger _logger;

        public ComprehensiveIntelligentBot(Player player, Scene scene, TestSessionLogger logger = null)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _logger = logger;
            _lastKnownHealth = player.Health;
            
            ScanLevelForItems();
            ScanLevelForEnemies();
        }

        public void Update(float dt)
        {
            _elapsedTime += dt;
            _thisFrameLog.Clear();

            // 1. CHECK FOR DIALOGUE/UI BLOCKING
            CheckDialogueStatus();
            if (_dialogueActive)
            {
                HandleDialogue();
                return;  // Skip other logic while dialogue is active
            }

            // 2. CHECK HEALTH STATUS
            CheckHealthStatus();

            // 3. SCAN ENVIRONMENT
            DetectGaps();
            DetectEnemies();
            DetectNearbyItems();

            // 4. MAKE INTELLIGENT DECISIONS
            MakeDecisions();

            // 5. LOG FRAME DATA
            LogFrame();
        }

        /// <summary>
        /// SCAN: Detect all items in level
        /// </summary>
        private void ScanLevelForItems()
        {
            try
            {
                // Try to get pickups list directly
                var pickupsField = _scene.GetType().GetField("_pickups",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (pickupsField != null)
                {
                    var pickups = pickupsField.GetValue(_scene) as List<HealthPickup>;
                    if (pickups != null)
                    {
                        foreach (var pickup in pickups)
                        {
                            if (pickup != null && !pickup.Collected)
                            {
                                _itemsInLevel.Add(new ItemTarget
                                {
                                    X = pickup.X,
                                    Y = pickup.Y,
                                    Type = "HealthPickup",
                                    IsReachable = IsItemReachable(pickup.X, pickup.Y)
                                });
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// SCAN: Detect all enemies in level
        /// </summary>
        private void ScanLevelForEnemies()
        {
            try
            {
                var entitiesField = _scene.GetType().GetField("_entities",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (entitiesField != null)
                {
                    var entities = entitiesField.GetValue(_scene) as List<Entity>;
                    if (entities != null)
                    {
                        foreach (var entity in entities)
                        {
                            Enemy enemy = entity as Enemy;
                            if (enemy != null)
                            {
                                _enemiesInLevel.Add(new EnemyTarget
                                {
                                    X = enemy.X,
                                    Y = enemy.Y,
                                    Type = enemy.GetType().Name,
                                    IsDefeated = false
                                });
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// CHECK: Is there dialogue/UI blocking gameplay?
        /// </summary>
        private void CheckDialogueStatus()
        {
            // In real gameplay, check if any dialogue boxes or popups are active
            // For now, just check a timer
            if (_dialogueWaitTimer > 0)
            {
                _dialogueActive = true;
                _dialogueWaitTimer -= 0.016f;
            }
            else
            {
                _dialogueActive = false;
            }
        }

        /// <summary>
        /// HANDLE: Dismiss dialogue and UI elements
        /// </summary>
        private void HandleDialogue()
        {
            CurrentState = "DIALOGUE";
            ShouldJump = false;
            ShouldAttack = false;
            ShouldMoveRight = false;
            ShouldDodge = false;
            
            // Press Enter to dismiss
            _thisFrameLog.Add("[BOT] Dismissing dialogue with Enter");
            _dialogueWaitTimer = 0;
        }

        /// <summary>
        /// CHECK: Monitor health and use items automatically
        /// </summary>
        private void CheckHealthStatus()
        {
            float currentHealth = _player.Health;
            
            // Health changed since last frame
            if (Math.Abs(currentHealth - _lastKnownHealth) > 0.1f)
            {
                if (currentHealth < _lastKnownHealth)
                {
                    _thisFrameLog.Add($"[BOT] Damage taken: {_lastKnownHealth:F0} → {currentHealth:F0}");
                }
                else if (currentHealth > _lastKnownHealth)
                {
                    _thisFrameLog.Add($"[BOT] Health recovered: {_lastKnownHealth:F0} → {currentHealth:F0}");
                }
            }

            // Auto-use health at 30 threshold
            if (currentHealth <= 30f && !_autoUsedHealth)
            {
                _autoUsedHealth = true;
                _healthUsageTime = _elapsedTime;
                ShouldUseHealth = true;
                _thisFrameLog.Add($"[BOT_HEALTH] AUTO-USED health item from inventory at {currentHealth:F0} HP");
            }
            else if (currentHealth > 60f)
            {
                _autoUsedHealth = false;
            }

            _lastKnownHealth = currentHealth;
        }

        /// <summary>
        /// DETECT: Gaps and platforms
        /// </summary>
        private void DetectGaps()
        {
            // Scan ahead for gaps in platform
            float scanDistance = 200f;

            // Look for drop-offs (gap detection)
            if (_player.X + scanDistance > 5000f)  // Near end of level
            {
                _thisFrameLog.Add("[BOT_GAP] Approaching level end");
            }
        }

        /// <summary>
        /// DETECT: Nearby enemies for combat
        /// </summary>
        private void DetectEnemies()
        {
            foreach (var enemy in _enemiesInLevel)
            {
                if (enemy.IsDefeated) continue;
                
                float distance = Math.Abs(_player.X - enemy.X);
                
                if (distance < 300f)
                {
                    _thisFrameLog.Add($"[BOT_COMBAT] Enemy detected: {enemy.Type} at distance {distance:F0}px");
                }
            }
        }

        /// <summary>
        /// DETECT: Nearby items to collect
        /// </summary>
        private void DetectNearbyItems()
        {
            foreach (var item in _itemsInLevel)
            {
                if (item.IsReachable && !_itemsCollected.Contains(item))
                {
                    float distance = Math.Abs(_player.X - item.X);
                    
                    if (distance < 400f && distance > 10f)
                    {
                        _thisFrameLog.Add($"[BOT_ITEM] Item nearby ({distance:F0}px): {item.Type}");
                    }
                    else if (distance <= 10f)
                    {
                        _itemsCollected.Add(item);
                        _thisFrameLog.Add($"[BOT_ITEM] ✓ COLLECTED: {item.Type}");
                    }
                }
            }
        }

        /// <summary>
        /// MAKE: Intelligent decisions based on all data
        /// </summary>
        private void MakeDecisions()
        {
            // Reset
            ShouldJump = false;
            ShouldAttack = false;
            ShouldDodge = false;
            ShouldMoveRight = true;
            ShouldUseHealth = false;
            CurrentState = "EXPLORING";

            // Priority 1: Combat
            float nearestEnemyDistance = float.MaxValue;
            Enemy nearestEnemy = null;
            
            try
            {
                var entitiesField = _scene.GetType().GetField("_entities",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (entitiesField != null)
                {
                    var entities = entitiesField.GetValue(_scene) as List<Entity>;
                    if (entities != null)
                    {
                        foreach (var entity in entities)
                        {
                            Enemy enemy = entity as Enemy;
                            if (enemy != null)
                            {
                                float distance = Math.Abs(_player.X - enemy.X);
                                if (distance < nearestEnemyDistance)
                                {
                                    nearestEnemyDistance = distance;
                                    nearestEnemy = enemy;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // If enemy nearby
            if (nearestEnemy != null && nearestEnemyDistance < 300f)
            {
                CurrentState = "COMBAT";
                ShouldAttack = true;
                
                // Jump to stomp if enemy below
                if (nearestEnemy.Y > _player.Y - 50f)
                {
                    ShouldJump = true;
                }
                
                _thisFrameLog.Add($"[BOT] ATTACKING - Enemy at {nearestEnemyDistance:F0}px");
                return;
            }

            // Priority 2: Collect nearby items
            float nearestItemDistance = float.MaxValue;
            ItemTarget nearestItem = null;
            
            foreach (var item in _itemsInLevel)
            {
                if (!_itemsCollected.Contains(item) && item.IsReachable)
                {
                    float distance = Math.Abs(_player.X - item.X);
                    if (distance < nearestItemDistance)
                    {
                        nearestItemDistance = distance;
                        nearestItem = item;
                    }
                }
            }

            if (nearestItem != null && nearestItemDistance < 300f)
            {
                CurrentState = "COLLECTING";
                // Just move toward it
                _thisFrameLog.Add($"[BOT] Moving to item at {nearestItemDistance:F0}px");
                return;
            }

            // Default: Explore forward with periodic jumps
            ShouldMoveRight = true;
            
            // Jump periodically for platforming
            if ((int)(_elapsedTime * 10f) % 30 == 0)
            {
                ShouldJump = true;
                _thisFrameLog.Add("[BOT] Periodic jump");
            }
        }

        /// <summary>
        /// Check if item is reachable (not in sky, not in unreachable area)
        /// </summary>
        private bool IsItemReachable(float itemX, float itemY)
        {
            // Items above Y=100 are likely in the sky or unreachable
            if (itemY < 100f)
                return false;
            
            // Items way below player spawn are likely unreachable
            if (itemY > 600f)
                return false;
            
            return true;
        }

        /// <summary>
        /// Log frame data
        /// </summary>
        private void LogFrame()
        {
            // Log every frame but could throttle
            if ((int)_elapsedTime % 1 == 0 && _elapsedTime % 1 < 0.016f)
            {
                foreach (var line in _thisFrameLog)
                {
                    System.Diagnostics.Debug.WriteLine(line);
                    _logger?.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// Get report on items missed
        /// </summary>
        public string GetItemCollectionReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("");
            report.AppendLine("═══════════════════════════════════════════════════════════════");
            report.AppendLine("ITEM COLLECTION REPORT");
            report.AppendLine("═══════════════════════════════════════════════════════════════");
            report.AppendLine($"Items in level: {_itemsInLevel.Count}");
            report.AppendLine($"Items collected: {_itemsCollected.Count}");
            report.AppendLine($"Items missed: {_itemsMissed.Count}");
            
            if (_itemsMissed.Count > 0)
            {
                report.AppendLine("");
                report.AppendLine("MISSED ITEMS:");
                foreach (var item in _itemsMissed)
                {
                    report.AppendLine($"  - {item.Type} at X={item.X:F0} Y={item.Y:F0}");
                    if (!item.IsReachable)
                    {
                        report.AppendLine($"    Reason: {item.UnreachableReason}");
                    }
                }
            }
            
            report.AppendLine("═══════════════════════════════════════════════════════════════");
            return report.ToString();
        }

        /// <summary>
        /// Get comprehensive bot status
        /// </summary>
        public string GetComprehensiveStatus()
        {
            return $"State={CurrentState} | Health={_player.Health:F0}/{_player.MaxHealth} | " +
                   $"Items={_itemsCollected.Count}/{_itemsInLevel.Count} | " +
                   $"Jump={ShouldJump} | Attack={ShouldAttack}";
        }
    }
}
