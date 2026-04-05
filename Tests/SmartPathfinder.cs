using System;
using System.Collections.Generic;
using System.Drawing;

namespace Fridays_Adventure.Tests
{
    // ────────────────────────────────────────────
    // PHASE 2 - Team 10: Engine Programmer
    // Feature: Smart Pathfinding System (Batch 5)
    // Purpose: Intelligent pathfinding, platform prediction, gap detection
    // ────────────────────────────────────────────

    /// <summary>
    /// Advanced pathfinding for the smart bot.
    /// Predicts platforms, detects gaps, plans optimal routes.
    /// </summary>
    public sealed class SmartPathfinder
    {
        /// <summary>
        /// Detects upcoming gaps the bot will encounter.
        /// Used for jump planning and hazard avoidance.
        /// </summary>
        public static List<Rectangle> DetectUpcomingGaps(float botX, float botY, List<Rectangle> platforms, float lookAhead = 300f)
        {
            var gaps = new List<Rectangle>();

            // Scan ahead for missing platforms
            for (float checkX = botX + 50f; checkX < botX + lookAhead; checkX += 50f)
            {
                bool hasPlatform = false;

                foreach (var platform in platforms)
                {
                    if (platform.X <= checkX && checkX <= platform.X + platform.Width)
                    {
                        hasPlatform = true;
                        break;
                    }
                }

                if (!hasPlatform)
                {
                    gaps.Add(new Rectangle((int)checkX, (int)botY, 50, 500));
                }
            }

            if (gaps.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[BATCH5_PATHFIND] Detected {gaps.Count} gaps ahead");
            }

            return gaps;
        }

        /// <summary>
        /// Predicts the optimal landing platform for the next jump.
        /// </summary>
        public static Rectangle FindOptimalLandingPlatform(float botX, float botY, 
            List<Rectangle> platforms, float jumpRange = 200f)
        {
            Rectangle bestPlatform = new Rectangle(0, 0, 0, 0);
            float bestDistance = float.MaxValue;

            foreach (var platform in platforms)
            {
                // Check if platform is in jump range
                float platformX = platform.X + platform.Width / 2f;
                float distance = Math.Abs(platformX - botX);

                if (distance > 50f && distance < jumpRange && distance < bestDistance)
                {
                    // Prefer platforms directly ahead
                    if (platformX > botX)
                    {
                        bestPlatform = platform;
                        bestDistance = distance;
                    }
                }
            }

            if (bestPlatform.Width > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[BATCH5_PATHFIND] Optimal platform at {bestPlatform.X}, distance: {bestDistance:F0}px");
            }

            return bestPlatform;
        }

        /// <summary>
        /// Predicts if the bot will survive a platform crossing.
        /// Analyzes platform stability and safety.
        /// </summary>
        public static bool CanSafelyTraverse(Rectangle platform, float platformVelocity = 0f)
        {
            // Platform is safe if:
            // 1. Large enough to land on (> 30px width)
            // 2. Not moving too fast (< 500px/s)
            // 3. Actually under the bot's feet

            bool widthSafe = platform.Width > 30;
            bool speedSafe = Math.Abs(platformVelocity) < 500f;
            bool existsSafe = platform.Height > 0;

            return widthSafe && speedSafe && existsSafe;
        }

        /// <summary>
        /// Calculates optimal jump timing and height for obstacle clearance.
        /// </summary>
        public static float CalculateJumpHeight(float obstacleHeight, float obstacleDistance, float botSpeed = 150f)
        {
            // Jump height needed = obstacle height + safety margin
            // Timing = distance / speed

            float requiredHeight = obstacleHeight + 50f;  // 50px safety margin
            float jumpTiming = obstacleDistance / botSpeed;

            System.Diagnostics.Debug.WriteLine($"[BATCH5_PATHFIND] Jump height: {requiredHeight:F0}px, timing: {jumpTiming:F2}s");

            return requiredHeight;
        }

        /// <summary>
        /// Plans a route around detected obstacles and gaps.
        /// Returns waypoints the bot should aim for.
        /// </summary>
        public static List<PointF> PlanRoute(float botX, float botY, float targetX, 
            List<Rectangle> platforms, List<Rectangle> obstacles)
        {
            var waypoints = new List<PointF>();

            // Start point
            waypoints.Add(new PointF(botX, botY));

            // Simple pathfinding: walk straight, jump over gaps
            float currentX = botX;
            const float stepDistance = 100f;

            while (currentX < targetX)
            {
                currentX += stepDistance;

                bool hasGap = false;
                foreach (var obstacle in obstacles)
                {
                    if (obstacle.X <= currentX && currentX <= obstacle.X + obstacle.Width)
                    {
                        hasGap = true;
                        break;
                    }
                }

                if (hasGap)
                {
                    // Add jump waypoint
                    waypoints.Add(new PointF(currentX, botY - 100f));  // Up
                }

                waypoints.Add(new PointF(currentX, botY));  // Forward
            }

            // Target point
            waypoints.Add(new PointF(targetX, botY));

            System.Diagnostics.Debug.WriteLine($"[BATCH5_PATHFIND] Planned route with {waypoints.Count} waypoints");

            return waypoints;
        }

        /// <summary>
        /// Analyzes level terrain for optimal bot strategy.
        /// </summary>
        public static void AnalyzeTerrain(List<Rectangle> platforms, float levelWidth = 2800f)
        {
            if (platforms == null || platforms.Count == 0) return;

            // Calculate average platform size
            int totalWidth = 0;
            int totalGaps = 0;

            var sortedPlatforms = new List<Rectangle>(platforms);
            sortedPlatforms.Sort((a, b) => a.X.CompareTo(b.X));

            for (int i = 0; i < sortedPlatforms.Count - 1; i++)
            {
                int gapSize = sortedPlatforms[i + 1].X - (sortedPlatforms[i].X + sortedPlatforms[i].Width);
                if (gapSize > 0)
                {
                    totalGaps += gapSize;
                }
                totalWidth += sortedPlatforms[i].Width;
            }

            int avgGap = totalGaps / Math.Max(1, sortedPlatforms.Count - 1);
            int avgPlatform = totalWidth / Math.Max(1, sortedPlatforms.Count);

            System.Diagnostics.Debug.WriteLine(
                $"[BATCH5_TERRAIN] Platforms: {sortedPlatforms.Count}, Avg platform: {avgPlatform}px, Avg gap: {avgGap}px");
        }
    }
}
