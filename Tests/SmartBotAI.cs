using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Fridays_Adventure.Tests
{
    // ────────────────────────────────────────────
    // PHASE 2 - Team 10: Engine Programmer
    // Feature: Smart Bot AI System
    // Purpose: Intelligent bot that detects and responds to:
    //          • Hazards (lightning, spikes, obstacles)
    //          • Enemies (avoidance or combat)
    //          • Health status (uses items when needed)
    //          • Pickups (currency, health items)
    //          • Level geometry (gaps, walls, platforms)
    // ────────────────────────────────────────────

    /// <summary>
    /// Represents a detected hazard in the level.
    /// </summary>
    public class DetectedHazard
    {
        public float X, Y;                  // Position
        public float Width, Height;         // Size
        public string Type;                 // "lightning", "spike", "projectile", "obstacle"
        public float Distance;              // Distance from bot
        public bool IsImmediate;            // Within 150px (critical)
    }

    /// <summary>
    /// Represents a detected enemy.
    /// </summary>
    public class DetectedEnemy
    {
        public float X, Y;                  // Position
        public float Width, Height;         // Hitbox
        public string Type;                 // Enemy type
        public float Distance;              // Distance from bot
        public bool IsAggressive;           // Moving toward bot
        public int Health;                  // Enemy health
    }

    /// <summary>
    /// Represents a detected pickup.
    /// </summary>
    public class DetectedPickup
    {
        public float X, Y;                  // Position
        public string Type;                 // "berry", "health", "currency", "powerup"
        public float Value;                 // Pickup value
        public float Distance;              // Distance from bot
    }

    /// <summary>
    /// Core intelligent bot AI system.
    /// Makes decisions based on:
    /// - Health status
    /// - Detected hazards
    /// - Detected enemies
    /// - Detected pickups
    /// - Level geometry awareness
    /// </summary>
    public sealed class SmartBotAI
    {
        // ── State ────────────────────────────────────────────────────────
        private float _elapsedTime = 0f;
        private int _currentHealth = 100;
        private int _maxHealth = 100;
        private bool _isHurt = false;
        private bool _needsHealing = false;

        // ── Detection ────────────────────────────────────────────────────
        private List<DetectedHazard> _hazards = new List<DetectedHazard>();
        private List<DetectedEnemy> _enemies = new List<DetectedEnemy>();
        private List<DetectedPickup> _pickups = new List<DetectedPickup>();

        // ── Timers ───────────────────────────────────────────────────────
        private float _lastHazardAvoidTime = -1f;
        private float _lastEnemyEngageTime = -1f;
        private float _lastHealthCheckTime = -1f;

        // ── Configuration ────────────────────────────────────────────────
        private const float HAZARD_DETECTION_RANGE = 300f;      // Look ahead for hazards
        private const float ENEMY_DETECTION_RANGE = 400f;       // Look for enemies
        private const float PICKUP_DETECTION_RANGE = 250f;      // Look for items
        private const float HEALTH_PANIC_THRESHOLD = 0.3f;      // Use items at 30% health
        private const float HAZARD_AVOIDANCE_DELAY = 2.0f;      // Wait before jumping again
        private const float ENEMY_ENGAGEMENT_DELAY = 1.0f;      // Attack frequency limit

        // ── AI Decision State ────────────────────────────────────────────
        public enum BotBehavior
        {
            Idle,
            SprintNormal,
            AvoidHazard,
            EngageEnemy,
            SeekHealth,
            SeekPickup,
            Panic
        }

        public BotBehavior CurrentBehavior { get; private set; } = BotBehavior.SprintNormal;

        // ── Output decisions ────────────────────────────────────────────
        public bool ShouldMoveRight { get; private set; } = true;
        public bool ShouldJump { get; private set; } = false;
        public bool ShouldAttack { get; private set; } = false;
        public bool ShouldDodge { get; private set; } = false;
        public float TargetX { get; private set; } = 0f;         // For pathfinding
        public float TargetY { get; private set; } = 0f;

        // ── Diagnostics ──────────────────────────────────────────────────
        public List<DetectedHazard> DetectedHazards => new List<DetectedHazard>(_hazards);
        public List<DetectedEnemy> DetectedEnemies => new List<DetectedEnemy>(_enemies);
        public List<DetectedPickup> DetectedPickups => new List<DetectedPickup>(_pickups);

        // ── Public API ───────────────────────────────────────────────────

        /// <summary>
        /// Update bot AI each frame.
        /// </summary>
        public void Update(float dt, float botX, float botY, int currentHealth, int maxHealth)
        {
            _elapsedTime += dt;
            _currentHealth = currentHealth;
            _maxHealth = maxHealth;
            _isHurt = _currentHealth < _maxHealth;
            _needsHealing = _currentHealth <= (int)(_maxHealth * HEALTH_PANIC_THRESHOLD);

            // Clear previous detection
            _hazards.Clear();
            _enemies.Clear();
            _pickups.Clear();

            // BATCH 1: Core detection (you'll provide these via callbacks/interfaces)
            DetectHazards(botX, botY);
            DetectEnemies(botX, botY);
            DetectPickups(botX, botY);

            // BATCH 1: Make decisions based on detections
            MakeDecisions(botX, botY, dt);

            // Log current state
            LogBotState();
        }

        /// <summary>
        /// Set detected hazards for this frame.
        /// Called by the game scene to provide hazard information.
        /// </summary>
        public void SetDetectedHazards(List<DetectedHazard> hazards)
        {
            _hazards.Clear();
            if (hazards != null)
            {
                foreach (var h in hazards)
                {
                    // Calculate distance
                    h.Distance = Math.Abs(h.X - 0);  // Will be updated by game
                    _hazards.Add(h);
                }
            }
        }

        /// <summary>
        /// Set detected enemies for this frame.
        /// </summary>
        public void SetDetectedEnemies(List<DetectedEnemy> enemies)
        {
            _enemies.Clear();
            if (enemies != null)
            {
                foreach (var e in enemies)
                {
                    _enemies.Add(e);
                }
            }
        }

        /// <summary>
        /// Set detected pickups for this frame.
        /// </summary>
        public void SetDetectedPickups(List<DetectedPickup> pickups)
        {
            _pickups.Clear();
            if (pickups != null)
            {
                foreach (var p in pickups)
                {
                    _pickups.Add(p);
                }
            }
        }

        /// <summary>
        /// Report health change (when bot takes damage).
        /// </summary>
        public void OnHealthChanged(int newHealth)
        {
            _currentHealth = newHealth;
            _isHurt = true;
            System.Diagnostics.Debug.WriteLine($"[SMART_BOT] Health changed: {newHealth}/{_maxHealth}");
        }

        // ── Internal Decision Logic ──────────────────────────────────────

        private void DetectHazards(float botX, float botY)
        {
            // BATCH 1: Basic detection (already populated by game)
            // BATCH 4: Enhanced detection for specialized scenes
            System.Diagnostics.Debug.WriteLine($"[SMART_BOT] Scanning for hazards in range {HAZARD_DETECTION_RANGE}px");
        }

        private void DetectEnemies(float botX, float botY)
        {
            // BATCH 1: Placeholder - game will populate this
            System.Diagnostics.Debug.WriteLine($"[SMART_BOT] Scanning for enemies in range {ENEMY_DETECTION_RANGE}px");
        }

        private void DetectPickups(float botX, float botY)
        {
            // BATCH 1: Placeholder - game will populate this
            System.Diagnostics.Debug.WriteLine($"[SMART_BOT] Scanning for pickups in range {PICKUP_DETECTION_RANGE}px");
        }

        private void MakeDecisions(float botX, float botY, float dt)
        {
            // ── Priority 1: PANIC - HEALTH CRITICAL ──────────────────────
            if (_needsHealing && _pickups.Count > 0)
            {
                var nearestHealth = FindNearestPickup("health");
                if (nearestHealth != null)
                {
                    CurrentBehavior = BotBehavior.SeekHealth;
                    PathfindToPickup(nearestHealth, botX, botY);
                    ShouldAttack = false;
                    ShouldJump = false;
                    System.Diagnostics.Debug.WriteLine($"[SMART_BOT] PANIC MODE - Seeking health at ({nearestHealth.X:F0}, {nearestHealth.Y:F0})");
                    return;
                }
            }

            // ── Priority 2: HAZARD AVOIDANCE ────────────────────────────
            if (_hazards.Count > 0)
            {
                var immediateDanger = FindImmediateDanger();
                if (immediateDanger != null && _elapsedTime - _lastHazardAvoidTime >= HAZARD_AVOIDANCE_DELAY)
                {
                    CurrentBehavior = BotBehavior.AvoidHazard;
                    AvoidHazard(immediateDanger, botX, botY);
                    _lastHazardAvoidTime = _elapsedTime;
                    System.Diagnostics.Debug.WriteLine($"[SMART_BOT] HAZARD DETECTED - {immediateDanger.Type} at ({immediateDanger.X:F0}, {immediateDanger.Y:F0}), distance: {immediateDanger.Distance:F0}px");
                    return;
                }
            }

            // ── Priority 3: ENEMY ENGAGEMENT ─────────────────────────────
            if (_enemies.Count > 0 && _elapsedTime - _lastEnemyEngageTime >= ENEMY_ENGAGEMENT_DELAY)
            {
                var nearestEnemy = FindNearestEnemy();
                if (nearestEnemy != null && nearestEnemy.Distance < 250f)
                {
                    CurrentBehavior = BotBehavior.EngageEnemy;
                    EngageEnemy(nearestEnemy, botX, botY);
                    _lastEnemyEngageTime = _elapsedTime;
                    System.Diagnostics.Debug.WriteLine($"[SMART_BOT] ENEMY ENGAGED - {nearestEnemy.Type} at distance {nearestEnemy.Distance:F0}px");
                    return;
                }
            }

            // ── Priority 4: SEEK CURRENCY/ITEMS ─────────────────────────
            if (_pickups.Count > 0 && !_isHurt)
            {
                var nearestCurrency = FindNearestPickup("berry");
                if (nearestCurrency != null && nearestCurrency.Distance < 200f)
                {
                    CurrentBehavior = BotBehavior.SeekPickup;
                    PathfindToPickup(nearestCurrency, botX, botY);
                    System.Diagnostics.Debug.WriteLine($"[SMART_BOT] SEEKING PICKUP - {nearestCurrency.Type} at distance {nearestCurrency.Distance:F0}px");
                    return;
                }
            }

            // ── Default: SPRINT NORMALLY ─────────────────────────────────
            CurrentBehavior = BotBehavior.SprintNormal;
            ShouldMoveRight = true;
            ShouldJump = ShouldPeriodicJump();
            ShouldAttack = ShouldPeriodicAttack();
            ShouldDodge = false;
        }

        private void AvoidHazard(DetectedHazard hazard, float botX, float botY)
        {
            ShouldMoveRight = true;
            ShouldJump = true;                      // Jump to avoid
            ShouldAttack = false;
            ShouldDodge = hazard.Type == "lightning"; // Dodge lightning specifically

            // If hazard is above, jump higher; if below, keep moving
            TargetX = botX + 200f;  // Move forward quickly
        }

        private void EngageEnemy(DetectedEnemy enemy, float botX, float botY)
        {
            ShouldMoveRight = true;
            ShouldAttack = true;                    // Keep attacking

            // ── STOMP DETECTION: Jump on enemy head if enemy is below ─────
            // Classic platformer stomp: jump on enemy's head to defeat them
            if (enemy.Y > botY - 30f)  // Enemy head is roughly at same level or below
            {
                ShouldJump = true;      // Jump to stomp!
                System.Diagnostics.Debug.WriteLine($"[SMART_BOT] STOMP ATTACK - Jumping on {enemy.Type} head!");
            }
            else if (enemy.Y < botY - 50f)  // Enemy is above
            {
                ShouldJump = true;      // Jump to reach it
            }
            else
            {
                ShouldJump = false;     // Enemy is at level, don't jump
            }

            // Move toward enemy if far

            {
                TargetX = enemy.X;
            }
        }

        private void PathfindToPickup(DetectedPickup pickup, float botX, float botY)
        {
            // Simple pathfinding: move toward pickup
            ShouldMoveRight = pickup.X > botX;
            ShouldJump = (pickup.Y < botY - 100f);  // Jump if pickup is above
            ShouldAttack = false;
            TargetX = pickup.X;
            TargetY = pickup.Y;
        }

        private bool ShouldPeriodicJump()
        {
            // Jump every 0.55s for basic platforming
            return (_elapsedTime % 0.55f) < 0.1f;
        }

        private bool ShouldPeriodicAttack()
        {
            // Attack every 0.5s for continuous combat
            return (_elapsedTime % 0.5f) < 0.1f;
        }

        // ── Helper Methods ───────────────────────────────────────────────

        private DetectedHazard FindImmediateDanger()
        {
            // Find hazard within critical distance (150px)
            foreach (var h in _hazards)
            {
                if (h.Distance < 150f)
                {
                    return h;
                }
            }
            return null;
        }

        private DetectedEnemy FindNearestEnemy()
        {
            if (_enemies.Count == 0) return null;
            
            DetectedEnemy nearest = _enemies[0];
            foreach (var e in _enemies)
            {
                if (e.Distance < nearest.Distance)
                    nearest = e;
            }
            return nearest;
        }

        private DetectedPickup FindNearestPickup(string type = null)
        {
            if (_pickups.Count == 0) return null;

            DetectedPickup nearest = null;
            foreach (var p in _pickups)
            {
                if (type != null && p.Type != type) continue;
                if (nearest == null || p.Distance < nearest.Distance)
                    nearest = p;
            }
            return nearest;
        }

        private void LogBotState()
        {
            if (_elapsedTime % 5f < 0.016f)  // Log every 5 seconds
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SMART_BOT] Behavior: {CurrentBehavior} | Health: {_currentHealth}/{_maxHealth} | " +
                    $"Hazards: {_hazards.Count} | Enemies: {_enemies.Count} | Pickups: {_pickups.Count}");
            }
        }
    }
}
