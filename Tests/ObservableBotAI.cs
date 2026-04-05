using System;
using System.Collections.Generic;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Scenes;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// OBSERVABLE SMART BOT AI - V2
    /// This version logs EVERYTHING so we can see exactly what's happening
    /// If it doesn't work, we can see exactly why in the logs
    /// </summary>
    public class ObservableBotAI
    {
        private Player _player;
        private Scene _scene;
        private float _elapsedTime = 0f;

        // ════════════════════════════════════════════════════════════════════
        // OBSERVABLE STATE - Easy to see what's happening
        // ════════════════════════════════════════════════════════════════════
        
        public List<Enemy> DetectedEnemies { get; private set; } = new List<Enemy>();
        public List<HealthPickup> DetectedPickups { get; private set; } = new List<HealthPickup>();
        
        public Enemy NearestEnemy { get; private set; }
        public HealthPickup NearestPickup { get; private set; }
        
        public float DistanceToNearestEnemy { get; private set; } = 999f;
        public float DistanceToNearestPickup { get; private set; } = 999f;

        // ════════════════════════════════════════════════════════════════════
        // DECISION OUTPUTS
        // ════════════════════════════════════════════════════════════════════
        
        public bool ShouldJump { get; private set; }
        public bool ShouldAttack { get; private set; }
        public bool ShouldDodge { get; private set; }
        public bool ShouldMoveRight { get; private set; } = true;

        public string CurrentState { get; private set; } = "Init";

        // ════════════════════════════════════════════════════════════════════
        // LOGGING
        // ════════════════════════════════════════════════════════════════════
        
        private List<string> _thisFrameLog = new List<string>();
        private bool _logNextFrame = false;
        
        public ObservableBotAI(Player player, Scene scene)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        }

        public void Update(float dt)
        {
            _elapsedTime += dt;
            _thisFrameLog.Clear();
            
            _thisFrameLog.Add($"[UPDATE] Time={_elapsedTime:F1}s | Player X={_player.X:F0} Y={_player.Y:F0} HP={_player.Health}");

            // 1. DETECT EVERYTHING
            DetectAllEntities();
            
            // 2. FIND NEAREST
            FindNearest();
            
            // 3. MAKE DECISIONS
            MakeDecisions();
            
            // 4. LOG IT
            LogFrame();
        }

        /// <summary>
        /// DETECT ALL ENTITIES - Very explicitly, with logging
        /// </summary>
        private void DetectAllEntities()
        {
            DetectedEnemies.Clear();
            DetectedPickups.Clear();
            int entitiesSearched = 0;

            try
            {
                // Try to get entities
                var entitiesField = _scene.GetType().GetField("_entities",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (entitiesField != null)
                {
                    var entities = entitiesField.GetValue(_scene) as List<Entity>;

                    if (entities != null)
                    {
                        entitiesSearched = entities.Count;
                        _thisFrameLog.Add($"  🔍 Searching {entities.Count} entities...");

                        int enemiesFound = 0;
                        foreach (var entity in entities)
                        {
                            if (entity == null || entity == _player) continue;

                            // CHECK IF ENEMY
                            Enemy enemy = entity as Enemy;
                            if (enemy != null)
                            {
                                float distance = Math.Abs(_player.X - enemy.X);
                                DetectedEnemies.Add(enemy);
                                enemiesFound++;
                                _thisFrameLog.Add($"    ✓ ENEMY #{enemiesFound}: X={enemy.X:F0} Distance={distance:F0}px Type={enemy.GetType().Name}");
                            }
                        }

                        if (enemiesFound == 0)
                        {
                            _thisFrameLog.Add($"    (No enemies found in {entities.Count} entities)");
                        }
                    }
                    else
                    {
                        _thisFrameLog.Add($"  ⚠️ _entities list is NULL!");
                    }
                }
                else
                {
                    _thisFrameLog.Add($"  ❌ ERROR: Scene {_scene.GetType().Name} has NO _entities field!");
                }

                // Try to get pickups
                var pickupsField = _scene.GetType().GetField("_pickups",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (pickupsField != null)
                {
                    var pickups = pickupsField.GetValue(_scene) as List<HealthPickup>;

                    if (pickups != null)
                    {
                        int pickupsFound = 0;
                        _thisFrameLog.Add($"  🔍 Found {pickups.Count} health pickups");

                        foreach (var pickup in pickups)
                        {
                            if (pickup != null && !pickup.Collected)
                            {
                                float distance = Math.Abs(_player.X - pickup.X);

                                // Only consider pickups if player needs health
                                if (_player.Health < _player.MaxHealth * 0.8f)
                                {
                                    DetectedPickups.Add(pickup);
                                    pickupsFound++;
                                    _thisFrameLog.Add($"    ✓ PICKUP #{pickupsFound}: X={pickup.X:F0} Distance={distance:F0}px");
                                }
                            }
                        }

                        if (pickupsFound == 0 && _player.Health < _player.MaxHealth * 0.8f)
                        {
                            _thisFrameLog.Add($"    (No available pickups for player health={_player.Health})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _thisFrameLog.Add($"  💥 EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Find the closest enemy and pickup
        /// </summary>
        private void FindNearest()
        {
            NearestEnemy = null;
            NearestPickup = null;
            DistanceToNearestEnemy = 999f;
            DistanceToNearestPickup = 999f;

            // Find nearest enemy
            foreach (var enemy in DetectedEnemies)
            {
                float distance = Math.Abs(_player.X - enemy.X);
                if (distance < DistanceToNearestEnemy)
                {
                    DistanceToNearestEnemy = distance;
                    NearestEnemy = enemy;
                }
            }

            // Find nearest pickup
            foreach (var pickup in DetectedPickups)
            {
                float distance = Math.Abs(_player.X - pickup.X);
                if (distance < DistanceToNearestPickup)
                {
                    DistanceToNearestPickup = distance;
                    NearestPickup = pickup;
                }
            }

            _thisFrameLog.Add($"  Nearest: Enemy={DistanceToNearestEnemy:F0}px | Pickup={DistanceToNearestPickup:F0}px");
        }

        /// <summary>
        /// MAKE INTELLIGENT DECISIONS based on what we detected
        /// </summary>
        private void MakeDecisions()
        {
            // Reset all actions
            ShouldJump = false;
            ShouldAttack = false;
            ShouldDodge = false;
            ShouldMoveRight = true;
            CurrentState = "None";

            // ── PRIORITY 1: CRITICAL HEALTH ──────────────────────────────
            if (_player.Health < _player.MaxHealth * 0.3f && NearestPickup != null)
            {
                CurrentState = "CRITICAL_HEALTH";
                ShouldMoveRight = true;
                _thisFrameLog.Add($"  → PRIORITY 1: CRITICAL HEALTH - Moving toward pickup ({DistanceToNearestPickup:F0}px away)");
                return;
            }

            // ── PRIORITY 2: ENEMY IN MELEE RANGE (< 150px) ──────────────
            if (NearestEnemy != null && DistanceToNearestEnemy < 150f)
            {
                CurrentState = "COMBAT_MELEE";
                
                // If enemy is below us, jump on it (stomp)
                if (NearestEnemy.Y > _player.Y - 50f)
                {
                    ShouldJump = true;
                    _thisFrameLog.Add($"  → PRIORITY 2: MELEE COMBAT - Jumping on enemy (below us)");
                }
                else
                {
                    ShouldJump = true;
                    _thisFrameLog.Add($"  → PRIORITY 2: MELEE COMBAT - Jumping (enemy above)");
                }
                
                // Always attack in melee
                ShouldAttack = true;
                _thisFrameLog.Add($"     └─ ATTACKING enemy at {DistanceToNearestEnemy:F0}px");
                return;
            }

            // ── PRIORITY 3: DISTANT ENEMY (150-400px) ───────────────────
            if (NearestEnemy != null && DistanceToNearestEnemy < 400f)
            {
                CurrentState = "COMBAT_RANGED";
                ShouldAttack = true;
                _thisFrameLog.Add($"  → PRIORITY 3: RANGED COMBAT - Attacking distant enemy ({DistanceToNearestEnemy:F0}px)");
                return;
            }

            // ── PRIORITY 4: NEARBY PICKUP (not critical health) ─────────
            if (NearestPickup != null && DistanceToNearestPickup < 300f)
            {
                CurrentState = "COLLECT_PICKUP";
                _thisFrameLog.Add($"  → PRIORITY 4: COLLECT - Moving toward pickup ({DistanceToNearestPickup:F0}px)");
                return;
            }

            // ── DEFAULT: EXPLORE & PLATFORMING ──────────────────────────
            CurrentState = "EXPLORE";
            ShouldMoveRight = true;
            
            // Periodic jumping for platforming (every 2-3 seconds)
            if ((int)(_elapsedTime * 10f) % 25 == 0)
            {
                ShouldJump = true;
                _thisFrameLog.Add($"  → DEFAULT: EXPLORE - Periodic jump for platforming");
            }
        }

        /// <summary>
        /// Log this frame's decisions
        /// </summary>
        private void LogFrame()
        {
            // Log EVERY frame (can be throttled later if needed)
            foreach (var line in _thisFrameLog)
            {
                System.Diagnostics.Debug.WriteLine(line);
            }

            // Always log the final decision
            System.Diagnostics.Debug.WriteLine($"  ➜ ACTION: Jump={ShouldJump} | Attack={ShouldAttack} | Move={ShouldMoveRight} | State={CurrentState}");
        }

        public string GetDebugInfo()
        {
            return $"State={CurrentState} | Enemies={DetectedEnemies.Count} | Pickups={DetectedPickups.Count} | " +
                   $"NearestEnemy={DistanceToNearestEnemy:F0}px | Jump={ShouldJump} | Attack={ShouldAttack}";
        }
    }
}
