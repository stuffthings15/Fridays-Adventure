using System;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Systems
{
    // ────────────────────────────────────────────────────────────────────────────
    // PHASE 2 - Team 1: Game Director
    // Feature: Difficulty Modifiers
    // Purpose: Runtime difficulty selection affecting enemy stats and gameplay
    // Status: IMPLEMENTATION
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// PHASE 2 - Team 1: Game Director
    /// Feature: Difficulty Modifiers System
    /// Implements: Hard Mode (2x enemy HP), Challenge Mode (1-hit KO), Normal Mode
    /// Allows runtime selection of difficulty affecting enemy stats and player survivability
    /// </summary>
    public static class DifficultyModifiers
    {
        /// <summary>
        /// Enumeration of available difficulty levels
        /// </summary>
        public enum Difficulty
        {
            Normal,      // Standard gameplay
            Hard,        // Enemies have 2x HP
            Challenge    // Player takes 1-hit KO
        }

        private static Difficulty _currentDifficulty = Difficulty.Normal;

        /// <summary>
        /// Gets or sets the current difficulty level
        /// </summary>
        public static Difficulty CurrentDifficulty
        {
            get => _currentDifficulty;
            set
            {
                _currentDifficulty = value;
                Game.Instance.Save?.SetInt("difficulty", (int)value);
            }
        }

        /// <summary>
        /// Initialize difficulty from saved config
        /// </summary>
        public static void Initialize()
        {
            if (Game.Instance.Save != null)
            {
                int savedDifficulty = Game.Instance.Save.GetInt("difficulty");
                if (Enum.IsDefined(typeof(Difficulty), savedDifficulty))
                    _currentDifficulty = (Difficulty)savedDifficulty;
            }
        }

        /// <summary>
        /// Get enemy health multiplier based on current difficulty
        /// </summary>
        public static float GetEnemyHealthMultiplier()
        {
            return _currentDifficulty switch
            {
                Difficulty.Hard => 2.0f,      // Enemies have 2x health
                Difficulty.Challenge => 1.0f, // Same health but player dies in 1 hit
                _ => 1.0f                      // Normal: standard health
            };
        }

        /// <summary>
        /// Get player damage multiplier to enemy based on difficulty
        /// </summary>
        public static float GetPlayerDamageMultiplier()
        {
            // In Challenge mode, one hit kills most enemies
            return _currentDifficulty switch
            {
                Difficulty.Challenge => 10.0f,  // Massive damage - one-shot kills
                _ => 1.0f                        // Normal and Hard: standard damage
            };
        }

        /// <summary>
        /// Get player max health based on difficulty
        /// </summary>
        public static int GetPlayerMaxHealth()
        {
            return _currentDifficulty switch
            {
                Difficulty.Challenge => 30,   // Low HP for 1-hit KO challenge
                _ => 100                       // Normal and Hard: standard health
            };
        }

        /// <summary>
        /// Get friendly difficulty name for UI display
        /// </summary>
        public static string GetDifficultyName(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Normal => "NORMAL",
                Difficulty.Hard => "HARD",
                Difficulty.Challenge => "CHALLENGE",
                _ => "UNKNOWN"
            };
        }

        /// <summary>
        /// Get difficulty description for UI tooltips
        /// </summary>
        public static string GetDifficultyDescription(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Normal => "Standard difficulty — Enemies have normal health",
                Difficulty.Hard => "Hard difficulty — Enemies have 2x health, stronger challenge",
                Difficulty.Challenge => "Challenge mode — One hit and you're down! Master the game",
                _ => "Unknown difficulty"
            };
        }
    }
}
