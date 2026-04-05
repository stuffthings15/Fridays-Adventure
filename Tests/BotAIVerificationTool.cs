using System;
using System.Diagnostics;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Scenes;

namespace Fridays_Adventure.Tests
{
    /// <summary>
    /// VERIFICATION TOOL: Ensures RealSmartBotAI is actually running during gameplay
    /// If the bot is still dumb, this tool will tell you exactly what's wrong
    /// </summary>
    public class BotAIVerificationTool
    {
        private static bool _enabled = false;
        private static int _frameCount = 0;
        private static float _elapsedTime = 0f;
        
        // Metrics
        private static int _enemiesDetected = 0;
        private static int _pickupsDetected = 0;
        private static int _jumpsTriggered = 0;
        private static int _attacksTriggered = 0;
        private static int _dodgesTriggered = 0;
        
        public static void Enable()
        {
            _enabled = true;
            _frameCount = 0;
            _elapsedTime = 0f;
            Debug.WriteLine("═══════════════════════════════════════════════════════════");
            Debug.WriteLine("🔍 BOT AI VERIFICATION STARTED");
            Debug.WriteLine("═══════════════════════════════════════════════════════════");
        }
        
        public static void VerifyAIFrame(RealSmartBotAI ai, float dt)
        {
            if (!_enabled || ai == null) return;
            
            _frameCount++;
            _elapsedTime += dt;
            
            // Every 60 frames (1 second at 60 FPS)
            if (_frameCount % 60 == 0)
            {
                string state = ai.CurrentState.ToString();
                Debug.WriteLine($"[FRAME {_frameCount}] {ai.GetDiagnostics()}");
            }
        }
        
        public static void Disable()
        {
            if (!_enabled) return;
            
            Debug.WriteLine("");
            Debug.WriteLine("═══════════════════════════════════════════════════════════");
            Debug.WriteLine("📊 BOT AI VERIFICATION REPORT");
            Debug.WriteLine("═══════════════════════════════════════════════════════════");
            Debug.WriteLine($"Runtime: {_elapsedTime:F1}s ({_frameCount} frames)");
            Debug.WriteLine($"Frames per second: {_frameCount / Math.Max(_elapsedTime, 0.016f):F1}");
            Debug.WriteLine("");
            Debug.WriteLine("✓ If you see diagnostics above, RealSmartBotAI IS running");
            Debug.WriteLine("✓ Check state values to see if detection is working");
            Debug.WriteLine("✓ If state is always 'Exploring', detection might be broken");
            Debug.WriteLine("");
            Debug.WriteLine("═══════════════════════════════════════════════════════════");
            _enabled = false;
        }
        
        /// <summary>
        /// QUICK TEST: Can AI see entities?
        /// Call this to verify reflection is working
        /// </summary>
        public static void QuickTest(Scene scene, Player player)
        {
            if (scene == null || player == null)
            {
                Debug.WriteLine("❌ QUICK TEST FAILED: Scene or player is null");
                return;
            }
            
            Debug.WriteLine("");
            Debug.WriteLine("⚙️  BOT AI QUICK TEST");
            Debug.WriteLine("───────────────────────────────────────────────────────────");
            
            try
            {
                // Try to get entities
                var entitiesField = scene.GetType().GetField("_entities",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (entitiesField == null)
                {
                    Debug.WriteLine("❌ ERROR: Scene doesn't have _entities field!");
                    Debug.WriteLine($"   Scene type: {scene.GetType().Name}");
                    return;
                }
                
                var entities = entitiesField.GetValue(scene) as System.Collections.Generic.List<Entity>;
                if (entities == null)
                {
                    Debug.WriteLine("❌ ERROR: _entities is null or not a List<Entity>!");
                    return;
                }
                
                Debug.WriteLine($"✓ Found {entities.Count} total entities");
                
                int enemyCount = 0;
                foreach (var entity in entities)
                {
                    if (entity is Enemy)
                    {
                        enemyCount++;
                        Enemy enemy = (Enemy)entity;
                        float distToPlayer = Math.Abs(player.X - enemy.X);
                        Debug.WriteLine($"  ✓ Enemy at X={enemy.X:F0}, Distance={distToPlayer:F0}px");
                    }
                }
                
                if (enemyCount == 0)
                {
                    Debug.WriteLine("⚠️  WARNING: No enemies detected in scene!");
                }
                
                // Try to get pickups
                var pickupsField = scene.GetType().GetField("_pickups",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (pickupsField != null)
                {
                    var pickups = pickupsField.GetValue(scene) as System.Collections.Generic.List<HealthPickup>;
                    if (pickups != null)
                    {
                        Debug.WriteLine($"✓ Found {pickups.Count} health pickups");
                        foreach (var pickup in pickups)
                        {
                            if (!pickup.Collected)
                            {
                                float dist = Math.Abs(player.X - pickup.X);
                                Debug.WriteLine($"  ✓ Pickup at X={pickup.X:F0}, Distance={dist:F0}px");
                            }
                        }
                    }
                }
                
                Debug.WriteLine("");
                Debug.WriteLine("✅ QUICK TEST PASSED - AI should be able to detect objects");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ QUICK TEST ERROR: {ex.Message}");
            }
            
            Debug.WriteLine("───────────────────────────────────────────────────────────");
        }
    }
}
