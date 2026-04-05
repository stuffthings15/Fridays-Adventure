using System;
using System.Collections.Generic;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Scenes;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// DIAGNOSTIC BOT - Actually shows what the bot sees and why it makes decisions
    /// Outputs VISIBLE debug information every frame
    /// </summary>
    public class DiagnosticBot
    {
        private Player _player;
        private Scene _scene;
        private float _elapsedTime = 0f;
        private int _frameCount = 0;

        // Detection results
        private List<Enemy> _enemies = new List<Enemy>();
        private List<HealthPickup> _pickups = new List<HealthPickup>();
        private Enemy _nearestEnemy = null;
        private float _nearestEnemyDistance = float.MaxValue;
        private HealthPickup _nearestPickup = null;
        private float _nearestPickupDistance = float.MaxValue;
        
        // Gap detection
        private bool _isFalling = false;
        private float _platformHeightEstimate = 300f;

        // Decisions
        public bool ShouldJump { get; private set; }
        public bool ShouldAttack { get; private set; }
        public bool ShouldMoveRight { get; private set; } = true;

        public DiagnosticBot(Player player, Scene scene)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _platformHeightEstimate = player.Y;

            PrintHeader();
        }

        private void PrintHeader()
        {
            Console.WriteLine("\n");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║ DIAGNOSTIC BOT - REAL-TIME DETECTION & DECISION LOGGING       ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
        }

        public void Update(float dt)
        {
            _elapsedTime += dt;
            _frameCount++;

            // Reset
            _nearestEnemy = null;
            _nearestEnemyDistance = float.MaxValue;
            _nearestPickup = null;
            _nearestPickupDistance = float.MaxValue;
            ShouldJump = false;
            ShouldAttack = false;
            ShouldMoveRight = true;

            // Detect everything
            DetectEnemies();
            DetectPickups();
            DetectGaps();

            // Make decision
            MakeDecision();

            // Log every frame (to actually see what's happening)
            if (_frameCount % 10 == 0)  // Every ~167ms
            {
                PrintDiagnostics();
            }
        }

        private void DetectEnemies()
        {
            _enemies.Clear();

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
                                _enemies.Add(enemy);
                                float dist = Math.Abs(_player.X - enemy.X);
                                if (dist < _nearestEnemyDistance)
                                {
                                    _nearestEnemyDistance = dist;
                                    _nearestEnemy = enemy;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Enemy detection failed: {ex.Message}");
                Console.ResetColor();
            }
        }

        private void DetectPickups()
        {
            _pickups.Clear();

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
                                _pickups.Add(pickup);
                                float dist = Math.Abs(_player.X - pickup.X);
                                if (dist < _nearestPickupDistance)
                                {
                                    _nearestPickupDistance = dist;
                                    _nearestPickup = pickup;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Pickup detection failed: {ex.Message}");
                Console.ResetColor();
            }
        }

        private void DetectGaps()
        {
            // Detect if player is falling
            _isFalling = _player.Y > _platformHeightEstimate + 50f;

            // Update platform height estimate (moving average)
            if (!_isFalling)
            {
                _platformHeightEstimate = _player.Y;
            }
        }

        private void MakeDecision()
        {
            // Priority 1: Enemy combat
            if (_nearestEnemy != null && _nearestEnemyDistance < 250f)
            {
                ShouldAttack = true;
                if (_nearestEnemy.Y > _player.Y - 80f)
                {
                    ShouldJump = true;  // Jump on enemy
                }
                return;
            }

            // Priority 2: Collect pickup
            if (_nearestPickup != null && _nearestPickupDistance < 300f)
            {
                // Don't jump on pickups, just move to them
                return;
            }

            // Priority 3: Falling - recover with jump
            if (_isFalling)
            {
                ShouldJump = true;
            }
        }

        private void PrintDiagnostics()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[FRAME {_frameCount}] T={_elapsedTime:F2}s");
            Console.ResetColor();

            // Player position
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"PLAYER: X={_player.X:F0} Y={_player.Y:F0} HP={_player.Health:F0}/{_player.MaxHealth:F0}");
            Console.ResetColor();

            // Enemies
            if (_enemies.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ENEMIES DETECTED: {_enemies.Count}");
                if (_nearestEnemy != null)
                {
                    Console.WriteLine($"  ├─ NEAREST: {_nearestEnemy.GetType().Name} at X={_nearestEnemy.X:F0} Y={_nearestEnemy.Y:F0} (distance={_nearestEnemyDistance:F0}px)");
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"ENEMIES: None detected");
                Console.ResetColor();
            }

            // Pickups
            if (_pickups.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"PICKUPS DETECTED: {_pickups.Count}");
                if (_nearestPickup != null)
                {
                    Console.WriteLine($"  ├─ NEAREST: HealthPickup at X={_nearestPickup.X:F0} Y={_nearestPickup.Y:F0} (distance={_nearestPickupDistance:F0}px)");
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine($"PICKUPS: None detected");
                Console.ResetColor();
            }

            // Gap detection
            Console.ForegroundColor = _isFalling ? ConsoleColor.Yellow : ConsoleColor.Green;
            Console.WriteLine($"GAP DETECTION: PlatformHeight={_platformHeightEstimate:F0} | Falling={_isFalling}");
            Console.ResetColor();

            // Decisions
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"DECISIONS: Jump={ShouldJump} | Attack={ShouldAttack} | Move={ShouldMoveRight}");
            if (ShouldJump && _isFalling)
            {
                Console.WriteLine("  └─ REASON: Falling - attempting recovery");
            }
            else if (ShouldJump && _nearestEnemy != null)
            {
                Console.WriteLine($"  └─ REASON: Combat - jumping on {_nearestEnemy.GetType().Name}");
            }
            else if (ShouldAttack)
            {
                Console.WriteLine($"  └─ REASON: Combat - attacking {_nearestEnemy.GetType().Name}");
            }
            else
            {
                Console.WriteLine("  └─ REASON: Normal movement");
            }
            Console.ResetColor();

            Console.WriteLine("─────────────────────────────────────────────────────────────");
        }

        public string GetStatus()
        {
            return $"Enemies={_enemies.Count} | Pickups={_pickups.Count} | Falling={_isFalling} | Jump={ShouldJump} | Attack={ShouldAttack}";
        }
    }
}
