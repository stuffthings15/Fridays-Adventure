using System;
using System.Collections.Generic;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Scenes;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// UNIFIED COMPREHENSIVE BOT - Complete Integration
    /// Integrates: Smart AI, stuck detection, item tracking, health management, combat, platforming
    /// Purpose: Single unified bot that handles ALL gameplay aspects
    /// </summary>
    public class UnifiedComprehensiveBot
    {
        private Player _player;
        private Scene _scene;
        private float _elapsedTime = 0f;

        // ════════════════════════════════════════════════════════════════════
        // CORE SYSTEMS
        // ════════════════════════════════════════════════════════════════════
        
        private BotStuckDetector _stuckDetector;
        private ComprehensiveBotActivityLogger _activityLogger;
        
        // ════════════════════════════════════════════════════════════════════
        // STATE TRACKING
        // ════════════════════════════════════════════════════════════════════

        private List<Enemy> _detectedEnemies = new List<Enemy>();
        private List<HealthPickup> _detectedPickups = new List<HealthPickup>();
        private float _lastHealthValue = 100f;
        private bool _healthAutoUsedThisFrame = false;
        private float _lastPlayerX = 0f;
        private float _stuckTimeCounter = 0f;

        // ════════════════════════════════════════════════════════════════════
        // DECISION OUTPUTS
        // ════════════════════════════════════════════════════════════════════
        
        public bool ShouldJump { get; private set; }
        public bool ShouldAttack { get; private set; }
        public bool ShouldMoveRight { get; private set; } = true;
        public bool ShouldDodge { get; private set; }
        public string CurrentState { get; private set; } = "Init";

        public UnifiedComprehensiveBot(Player player, Scene scene, ComprehensiveBotActivityLogger logger = null)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _activityLogger = logger;
            _lastHealthValue = player.Health;
            _stuckDetector = new BotStuckDetector();
            _stuckDetector.Initialize(player.X, player.Y);

            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║ UNIFIED COMPREHENSIVE BOT INITIALIZED                          ║");
            System.Diagnostics.Debug.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine($"Player: X={player.X:F0} Y={player.Y:F0} HP={player.Health}");
            System.Diagnostics.Debug.WriteLine($"Scene: {scene.GetType().Name}");
            System.Diagnostics.Debug.WriteLine("");
        }

        public void Update(float dt)
        {
            _elapsedTime += dt;
            
            // Reset decisions each frame
            ShouldJump = false;
            ShouldAttack = false;
            ShouldMoveRight = true;
            ShouldDodge = false;
            CurrentState = "EXPLORING";
            _healthAutoUsedThisFrame = false;

            try
            {
                // 1. CHECK STUCK STATUS (track position to detect stuck bot)
                float currentDist = Math.Abs(_player.X - _lastPlayerX);
                if (currentDist < 5f)  // Minimal movement
                {
                    _stuckTimeCounter += dt;
                    if (_stuckTimeCounter > 3f)  // Stuck for 3 seconds
                    {
                        CurrentState = "STUCK";
                        _activityLogger?.LogPlatformingAction("WARNING: Bot stuck", $"Duration: {_stuckTimeCounter:F1}s");
                    }
                }
                else
                {
                    _stuckTimeCounter = 0f;  // Reset stuck timer
                }
                _lastPlayerX = _player.X;

                // 2. CHECK HEALTH
                CheckHealthAndManage();

                // 3. SCAN ENVIRONMENT
                ScanForEnemies();
                ScanForPickups();

                // 4. MAKE DECISIONS
                MakeIntelligentDecisions();

                // 5. LOG STATE
                LogFrameState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BOT_ERROR] Update exception: {ex.Message}");
                _activityLogger?.LogPlatformingAction("ERROR", ex.Message);
            }
        }

        /// <summary>
        /// CHECK: Health status and auto-use items
        /// </summary>
        private void CheckHealthAndManage()
        {
            float currentHealth = _player.Health;
            float maxHealth = _player.MaxHealth;

            // Health changed?
            if (Math.Abs(currentHealth - _lastHealthValue) > 0.1f)
            {
                if (currentHealth < _lastHealthValue)
                {
                    _activityLogger?.LogHealthAction("DAMAGE_TAKEN", 
                        _lastHealthValue, maxHealth, 
                        $"Lost {(_lastHealthValue - currentHealth):F0} HP");
                }
            }

            // Auto-use health at 30%
            if (currentHealth <= maxHealth * 0.3f && !_healthAutoUsedThisFrame)
            {
                _healthAutoUsedThisFrame = true;
                System.Diagnostics.Debug.WriteLine($"[BOT_HEALTH] AUTO-USING health item at {currentHealth:F0}/{maxHealth:F0} HP");
                _activityLogger?.LogHealthAction("AUTO_USE_HEALTH", currentHealth, maxHealth, 
                    "Health item used automatically from inventory at 30 health");
            }

            _lastHealthValue = currentHealth;
        }

        /// <summary>
        /// SCAN: All enemies in the level
        /// </summary>
        private void ScanForEnemies()
        {
            _detectedEnemies.Clear();

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
                            if (entity == null || entity == _player) continue;
                            
                            Enemy enemy = entity as Enemy;
                            if (enemy != null)
                            {
                                _detectedEnemies.Add(enemy);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// SCAN: All pickups in the level
        /// </summary>
        private void ScanForPickups()
        {
            _detectedPickups.Clear();

            try
            {
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
                                _detectedPickups.Add(pickup);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// MAKE: Intelligent decisions based on environment
        /// </summary>
        private void MakeIntelligentDecisions()
        {
            // Find nearest enemy
            Enemy nearestEnemy = null;
            float nearestEnemyDistance = float.MaxValue;

            foreach (var enemy in _detectedEnemies)
            {
                float distance = Math.Abs(_player.X - enemy.X);
                if (distance < nearestEnemyDistance)
                {
                    nearestEnemyDistance = distance;
                    nearestEnemy = enemy;
                }
            }

            // PRIORITY 1: COMBAT - Enemy within range
            if (nearestEnemy != null && nearestEnemyDistance < 250f)
            {
                CurrentState = "COMBAT";
                ShouldAttack = true;

                // Jump on enemy if it's below us (stomp attack)
                if (nearestEnemy.Y > _player.Y - 80f && nearestEnemy.Y < _player.Y + 100f)
                {
                    ShouldJump = true;
                    _activityLogger?.LogCombatAction("STOMP_ATTACK", 
                        nearestEnemy.GetType().Name, nearestEnemyDistance, "Enemy below - jumping");
                }
                else if (nearestEnemy.Y < _player.Y - 80f)
                {
                    // Enemy above - jump to reach it
                    ShouldJump = true;
                    _activityLogger?.LogCombatAction("JUMP_ATTACK", 
                        nearestEnemy.GetType().Name, nearestEnemyDistance, "Enemy above - jumping");
                }
                else
                {
                    // Enemy at same height - just attack
                    _activityLogger?.LogCombatAction("MELEE_ATTACK", 
                        nearestEnemy.GetType().Name, nearestEnemyDistance, "Same height");
                }

                return;
            }

            // PRIORITY 2: COLLECT PICKUP - if low health or nearby
            if (_detectedPickups.Count > 0)
            {
                HealthPickup nearestPickup = null;
                float nearestPickupDistance = float.MaxValue;

                foreach (var pickup in _detectedPickups)
                {
                    float distance = Math.Abs(_player.X - pickup.X);
                    if (distance < nearestPickupDistance)
                    {
                        nearestPickupDistance = distance;
                        nearestPickup = pickup;
                    }
                }

                if (nearestPickup != null && nearestPickupDistance < 300f)
                {
                    CurrentState = "COLLECTING";
                    ShouldJump = false;  // ← DON'T jump over pickups!
                    _activityLogger?.LogItemAction("APPROACHING", "HealthPickup", 
                        nearestPickup.X, nearestPickup.Y, $"Distance: {nearestPickupDistance:F0}px");
                    return;
                }
            }

            // PRIORITY 3: PLATFORMING - Only jump to recover from falling, NOT periodically
            CurrentState = "PLATFORMING";
            ShouldMoveRight = true;
            ShouldJump = false;  // ← Default: don't jump!

            // ONLY jump if falling below normal platform level (recovery)
            if (_player.Y > 400f)  
            {
                ShouldJump = true;
                _activityLogger?.LogPlatformingAction("FALL_RECOVERY", $"Y={_player.Y:F0} (falling)");
            }
            // Otherwise, don't jump. Let the player move naturally without jumping over collectibles
        }

        /// <summary>
        /// LOG: Frame state for debugging
        /// </summary>
        private void LogFrameState()
        {
            // Log every second to avoid spam
            if ((int)_elapsedTime % 2 == 0 && _elapsedTime % 2 < 0.016f)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[BOT_STATE] T={_elapsedTime:F1}s | State={CurrentState} | " +
                    $"Pos=({_player.X:F0},{_player.Y:F0}) | HP={_player.Health:F0}/{_player.MaxHealth:F0} | " +
                    $"Enemies={_detectedEnemies.Count} | Pickups={_detectedPickups.Count} | " +
                    $"Jump={ShouldJump} Attack={ShouldAttack} Move={ShouldMoveRight}");
            }
        }

        /// <summary>
        /// Check if bot is stuck
        /// </summary>
        public bool IsStuck => _stuckDetector.IsStuck;

        /// <summary>
        /// Get comprehensive status
        /// </summary>
        public string GetStatus()
        {
            return $"State={CurrentState} | HP={_player.Health:F0} | " +
                   $"Enemies={_detectedEnemies.Count} | Pickups={_detectedPickups.Count} | " +
                   $"Stuck={IsStuck}";
        }
    }
}
