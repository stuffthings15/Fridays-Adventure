using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;

namespace Fridays_Adventure.Tests
{
    // ────────────────────────────────────────────
    // PHASE 2 - Team 10: Engine Programmer
    // Feature: Advanced Scene Detection System (Batch 4)
    // Purpose: Specialized hazard detection for Storm, Underwater, Boss scenes
    // ────────────────────────────────────────────

    /// <summary>
    /// Advanced detection system for specialized scene types.
    /// Handles lightning strikes, water hazards, boss patterns, etc.
    /// </summary>
    public sealed class AdvancedSceneDetector
    {
        // ── Lightning Strike Detection ────────────────────────────────────
        /// <summary>
        /// Detects active lightning strikes in StormScene.
        /// Lightning appears at specific X positions and has a predictable pattern.
        /// </summary>
        public static List<DetectedHazard> DetectLightningStrikes(object scene, Player player)
        {
            var strikes = new List<DetectedHazard>();
            if (scene == null || player == null) return strikes;

            // Use reflection to get weather data from StormScene
            var weatherField = scene.GetType().GetField("_weatherSystem",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (weatherField != null)
            {
                object weatherObj = weatherField.GetValue(scene);
                if (weatherObj != null)
                {
                    // Try to extract lightning position data
                    var lightningField = weatherObj.GetType().GetField("_lightningX",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (lightningField != null)
                    {
                        float lightningX = (float)lightningField.GetValue(weatherObj);
                        float dist = Math.Abs(lightningX - player.X);

                        if (dist < 500f)  // Extended range for lightning
                        {
                            strikes.Add(new DetectedHazard
                            {
                                X = lightningX,
                                Y = 0,
                                Width = 200,
                                Height = 1080,  // Full screen height
                                Type = "lightning",
                                Distance = dist,
                                IsImmediate = dist < 200f
                            });

                            System.Diagnostics.Debug.WriteLine(
                                $"[BATCH4_DETECT] Lightning strike at {lightningX:F0}, distance: {dist:F0}px");
                        }
                    }
                }
            }

            return strikes;
        }

        // ── Water Hazard Detection ────────────────────────────────────────
        /// <summary>
        /// Detects water level hazards in underwater/aquatic scenes.
        /// Rising/falling water can trap or drown the player.
        /// </summary>
        public static List<DetectedHazard> DetectWaterHazards(object scene, Player player)
        {
            var hazards = new List<DetectedHazard>();
            if (scene == null || player == null) return hazards;

            // Detect if this is an underwater scene
            string sceneName = scene.GetType().Name;
            bool isUnderwater = sceneName.Contains("Underwater") || sceneName.Contains("Coral") || 
                               sceneName.Contains("Kelp") || sceneName.Contains("Sunken");

            if (!isUnderwater) return hazards;

            // Water level rising/falling detection
            const float WATER_HAZARD_HEIGHT = 800f;  // Approximate screen height
            
            // Simulate water level (in real code, would query actual water level)
            float waterLevel = player.Y + 100f;  // Water is about 100px below player
            
            if (player.Y > waterLevel)
            {
                hazards.Add(new DetectedHazard
                {
                    X = player.X,
                    Y = waterLevel,
                    Width = 2000,
                    Height = 100,
                    Type = "water_rising",
                    Distance = Math.Abs(player.Y - waterLevel),
                    IsImmediate = player.Y > waterLevel - 50f
                });

                System.Diagnostics.Debug.WriteLine(
                    $"[BATCH4_DETECT] Water hazard: level at {waterLevel:F0}, bot at {player.Y:F0}");
            }

            return hazards;
        }

        // ── Boss Pattern Detection ────────────────────────────────────────
        /// <summary>
        /// Detects boss attack patterns and projectiles.
        /// Anticipates incoming attacks based on boss behavior.
        /// </summary>
        public static List<DetectedHazard> DetectBossHazards(object scene, Player player)
        {
            var hazards = new List<DetectedHazard>();
            if (scene == null || player == null) return hazards;

            // Detect boss projectiles via reflection
            var projectileFields = scene.GetType().GetFields(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var field in projectileFields)
            {
                // Look for projectile lists (Fireball, HammerBro projectiles, etc.)
                if (field.FieldType.IsGenericType && 
                    field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    object list = field.GetValue(scene);
                    if (list is System.Collections.IList projectiles)
                    {
                        foreach (var proj in projectiles)
                        {
                            if (proj == null) continue;

                            // Try to extract projectile position
                            var xProp = proj.GetType().GetProperty("X");
                            var yProp = proj.GetType().GetProperty("Y");
                            var wProp = proj.GetType().GetProperty("Width");
                            var hProp = proj.GetType().GetProperty("Height");
                            var velXProp = proj.GetType().GetProperty("VelocityX");

                            if (xProp != null && yProp != null && wProp != null && hProp != null)
                            {
                                float projX = (float)xProp.GetValue(proj);
                                float projY = (float)yProp.GetValue(proj);
                                float projW = (float)wProp.GetValue(proj);
                                float projH = (float)hProp.GetValue(proj);
                                float dist = Math.Abs(projX - player.X);

                                if (dist < 400f)  // Boss projectile range
                                {
                                    hazards.Add(new DetectedHazard
                                    {
                                        X = projX,
                                        Y = projY,
                                        Width = projW,
                                        Height = projH,
                                        Type = "boss_projectile",
                                        Distance = dist,
                                        IsImmediate = dist < 150f
                                    });
                                }
                            }
                        }
                    }
                }
            }

            if (hazards.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[BATCH4_DETECT] Boss hazards: {hazards.Count} projectiles detected");
            }

            return hazards;
        }

        // ── Platform Weakness Detection ──────────────────────────────────
        /// <summary>
        /// Detects unstable or breaking platforms.
        /// Bot should avoid or quickly traverse these platforms.
        /// </summary>
        public static List<DetectedHazard> DetectUnstablePlatforms(object scene, Player player)
        {
            var hazards = new List<DetectedHazard>();
            if (scene == null || player == null) return hazards;

            // Detect moving/breaking platforms that are unstable
            var platformFields = scene.GetType().GetFields(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var field in platformFields)
            {
                // Look for platform lists
                if (field.Name.Contains("Moving") || field.Name.Contains("Platform"))
                {
                    if (field.FieldType.IsGenericType && 
                        field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        object list = field.GetValue(scene);
                        if (list is System.Collections.IList platforms)
                        {
                            foreach (var plat in platforms)
                            {
                                if (plat == null) continue;

                                var xProp = plat.GetType().GetProperty("X");
                                var yProp = plat.GetType().GetProperty("Y");
                                var wProp = plat.GetType().GetProperty("Width");
                                var hProp = plat.GetType().GetProperty("Height");
                                var velProp = plat.GetType().GetProperty("VelocityX");

                                if (xProp != null && velProp != null)
                                {
                                    float platX = (float)xProp.GetValue(plat);
                                    float platY = (float)yProp.GetValue(plat);
                                    float platW = (float)wProp.GetValue(plat);
                                    float platH = (float)hProp.GetValue(plat);
                                    float vel = (float)velProp.GetValue(plat);

                                    // Check if platform is moving unpredictably
                                    if (Math.Abs(vel) > 300f)  // Fast moving
                                    {
                                        float dist = Math.Abs(platX - player.X);
                                        if (dist < 300f)
                                        {
                                            hazards.Add(new DetectedHazard
                                            {
                                                X = platX,
                                                Y = platY,
                                                Width = platW,
                                                Height = platH,
                                                Type = "unstable_platform",
                                                Distance = dist,
                                                IsImmediate = dist < 100f
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return hazards;
        }

        // ── Trap Detection ───────────────────────────────────────────────
        /// <summary>
        /// Detects floor spikes, spike traps, and other instant-kill hazards.
        /// These require immediate avoidance (jumping or running around).
        /// </summary>
        public static List<DetectedHazard> DetectTraps(object scene, Player player)
        {
            var traps = new List<DetectedHazard>();
            if (scene == null || player == null) return traps;

            // Detect spike hazards
            var hazardFields = scene.GetType().GetFields(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var field in hazardFields)
            {
                if (field.Name.Contains("Hazard") || field.Name.Contains("Spike") || field.Name.Contains("Trap"))
                {
                    if (field.FieldType.IsGenericType && 
                        field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        object list = field.GetValue(scene);
                        if (list is System.Collections.IList hazards)
                        {
                            foreach (var hazard in hazards)
                            {
                                if (hazard == null) continue;

                                var xProp = hazard.GetType().GetProperty("X");
                                var yProp = hazard.GetType().GetProperty("Y");
                                var activeProp = hazard.GetType().GetProperty("IsActive");

                                if (xProp != null && activeProp != null)
                                {
                                    bool isActive = (bool)activeProp.GetValue(hazard);
                                    if (!isActive) continue;

                                    float hazX = (float)xProp.GetValue(hazard);
                                    float hazY = (float)yProp.GetValue(hazard);
                                    float dist = Math.Abs(hazX - player.X);

                                    if (dist < 250f)
                                    {
                                        traps.Add(new DetectedHazard
                                        {
                                            X = hazX,
                                            Y = hazY,
                                            Width = 32,
                                            Height = 32,
                                            Type = "spike_trap",
                                            Distance = dist,
                                            IsImmediate = dist < 100f
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (traps.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[BATCH4_DETECT] Traps detected: {traps.Count} spike hazards");
            }

            return traps;
        }

        // ── Combined Advanced Detection ──────────────────────────────────
        /// <summary>
        /// Performs all advanced detections and returns comprehensive hazard list.
        /// Called by SmartBotAI for specialized scene handling.
        /// </summary>
        public static List<DetectedHazard> DetectAllAdvancedHazards(object scene, Player player)
        {
            var allHazards = new List<DetectedHazard>();

            // Run all detection methods
            allHazards.AddRange(DetectLightningStrikes(scene, player));
            allHazards.AddRange(DetectWaterHazards(scene, player));
            allHazards.AddRange(DetectBossHazards(scene, player));
            allHazards.AddRange(DetectUnstablePlatforms(scene, player));
            allHazards.AddRange(DetectTraps(scene, player));

            // Sort by distance (nearest first)
            allHazards.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            if (allHazards.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[BATCH4_ADVANCED] Total advanced hazards detected: {allHazards.Count}");
            }

            return allHazards;
        }
    }
}
