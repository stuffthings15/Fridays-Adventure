using System;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Systems
{
    public static class ThreatSystem
    {
        private const float DecayRate = 0.4f; // threat slowly falls over time

        public static void Tick(float dt)
        {
            Game.Instance.ThreatLevel = Math.Max(0,
                Game.Instance.ThreatLevel - DecayRate * dt);
        }

        public static void OnNodeTraversed()  => AddThreat(5f);
        public static void OnIslandCleared()  => AddThreat(-8f);
        public static void OnBossDefeated()   => AddThreat(-20f);
        public static void OnPlayerSpotted()  => AddThreat(12f);
        public static void OnStealthRoute()   => AddThreat(-5f);

        private static void AddThreat(float amount)
        {
            Game.Instance.ThreatLevel = Math.Max(0,
                Math.Min(100, Game.Instance.ThreatLevel + amount));
        }

        // Scales enemy HP by threat level
        public static float EnemyHpMultiplier()
        {
            float t = Game.Instance.ThreatLevel;
            if (t >= 80) return 1.6f;
            if (t >= 50) return 1.25f;
            return 1.0f;
        }

        // Returns true when threat is so high that extra patrols appear
        public static bool BlockadeActive() => Game.Instance.ThreatLevel >= 70;

        public static string ThreatLabel()
        {
            float t = Game.Instance.ThreatLevel;
            if (t >= 80) return "CRITICAL — Marines everywhere";
            if (t >= 60) return "HIGH — Pursuit squads deployed";
            if (t >= 40) return "ELEVATED — Patrols increased";
            if (t >= 20) return "MODERATE — Scouts reported";
            return "LOW — Seas are calm";
        }
    }

    public static class BountySystem
    {
        public static void Award(int amount)
        {
            Game.Instance.PlayerBounty += amount;
            // Every 2000 bounty triggers a threat spike
            int prev = Game.Instance.PlayerBounty - amount;
            if (Game.Instance.PlayerBounty / 2000 > prev / 2000)
                ThreatSystem.OnPlayerSpotted();
        }

        public static string Title()
        {
            int b = Game.Instance.PlayerBounty;
            if (b >= 15000) return "Legendary Pirate";
            if (b >= 8000)  return "Notorious Pirate";
            if (b >= 4000)  return "Wanted Pirate";
            if (b >= 1500)  return "Rising Pirate";
            if (b >= 500)   return "Small-Time Raider";
            return "Unknown Wanderer";
        }

        public static string Formatted() =>
            $"฿{Game.Instance.PlayerBounty:N0}  [{Title()}]";
    }
}
