// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 7: Gameplay Programmer
// Feature: Gameplay Expansion Systems Pack
// Purpose: Implements Phase 3 combat/mobility systems for skins, weapons,
//          finishers, shield upgrades, throwables, boosts, slow time,
//          invulnerability tuning, double damage, and knockback resistance.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Team 7 Feature 1: Character skins runtime state.
    /// </summary>
    public static class CharacterSkinsSystem
    {
        private static readonly Dictionary<string, string> _equippedByCharacter =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Miss Friday"] = "default_skin",
                ["Orca"] = "default_skin",
                ["Swan"] = "default_skin",
            };

        /// <summary>Sets equipped skin for character if owned.</summary>
        /// <remarks>PHASE 3 - Team 7: Character Skins</remarks>
        public static bool Equip(string characterName, string skinId)
        {
            if (string.IsNullOrWhiteSpace(characterName) || string.IsNullOrWhiteSpace(skinId)) return false;
            var owned = CosmeticInventorySystem.GetOwned();
            if (!owned.Contains(skinId)) return false;
            _equippedByCharacter[characterName] = skinId;
            return true;
        }

        /// <summary>Gets equipped skin id for character.</summary>
        /// <remarks>PHASE 3 - Team 7: Character Skins</remarks>
        public static string GetEquipped(string characterName)
        {
            return _equippedByCharacter.TryGetValue(characterName ?? string.Empty, out string skin) ? skin : "default_skin";
        }
    }

    /// <summary>
    /// Team 7 Feature 2: Weapon system and weapon stats.
    /// </summary>
    public static class WeaponSystem
    {
        /// <summary>Simple weapon descriptor.</summary>
        public sealed class Weapon
        {
            /// <summary>Weapon id.</summary>
            public string Id { get; set; }
            /// <summary>Display name.</summary>
            public string Name { get; set; }
            /// <summary>Base damage.</summary>
            public int Damage { get; set; }
            /// <summary>Attack speed scalar.</summary>
            public float Speed { get; set; }
        }

        private static readonly List<Weapon> _weapons = new List<Weapon>
        {
            new Weapon { Id = "blade_basic", Name = "Tide Blade", Damage = 18, Speed = 1.0f },
            new Weapon { Id = "blade_rapid", Name = "Storm Needle", Damage = 12, Speed = 1.4f },
            new Weapon { Id = "hammer_heavy", Name = "Anchor Hammer", Damage = 30, Speed = 0.7f },
        };

        /// <summary>Returns all weapon definitions.</summary>
        /// <remarks>PHASE 3 - Team 7: Weapon System</remarks>
        public static IReadOnlyList<Weapon> GetAll() => _weapons;

        /// <summary>Returns weapon by id.</summary>
        /// <remarks>PHASE 3 - Team 7: Weapon System</remarks>
        public static Weapon Get(string id) => _weapons.FirstOrDefault(w => w.Id.Equals(id ?? string.Empty, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Team 7 Feature 3: Combo finisher moves resolver.
    /// </summary>
    public static class ComboFinisherMovesSystem
    {
        /// <summary>Returns finisher name based on combo count.</summary>
        /// <remarks>PHASE 3 - Team 7: Combo Finisher Moves</remarks>
        public static string ResolveFinisher(int combo)
        {
            if (combo >= 25) return "Tempest Breaker";
            if (combo >= 15) return "Cyclone Arc";
            if (combo >= 8) return "Rising Slash";
            return "No finisher";
        }
    }

    /// <summary>
    /// Team 7 Feature 4: Shield mechanics advanced.
    /// </summary>
    public static class ShieldMechanicsAdvancedSystem
    {
        /// <summary>Current shield durability value.</summary>
        public static int Durability { get; private set; } = 100;

        /// <summary>Consumes shield durability for blocked damage.</summary>
        /// <remarks>PHASE 3 - Team 7: Shield Mechanics Advanced</remarks>
        public static int BlockDamage(int incoming)
        {
            int absorbed = Math.Min(incoming, Durability);
            Durability = Math.Max(0, Durability - absorbed);
            return Math.Max(0, incoming - absorbed);
        }

        /// <summary>Refills shield durability.</summary>
        /// <remarks>PHASE 3 - Team 7: Shield Mechanics Advanced</remarks>
        public static void Refill() => Durability = 100;
    }

    /// <summary>
    /// Team 7 Feature 5: Bomb throwable model.
    /// </summary>
    public static class BombThrowableSystem
    {
        /// <summary>Returns explosion damage from charge level.</summary>
        /// <remarks>PHASE 3 - Team 7: Bomb Throwable</remarks>
        public static int DamageFromCharge(float secondsHeld)
        {
            float s = Math.Max(0f, Math.Min(2.5f, secondsHeld));
            return 20 + (int)(s * 18f);
        }
    }

    /// <summary>
    /// Team 7 Feature 6: Jump boost pads service.
    /// </summary>
    public static class JumpBoostPadsSystem
    {
        /// <summary>Returns vertical velocity after hitting boost pad.</summary>
        /// <remarks>PHASE 3 - Team 7: Jump Boost Pads</remarks>
        public static float ApplyBoost(float currentVy, float padStrength = 12f)
        {
            return -Math.Max(6f, padStrength) + currentVy * 0.1f;
        }
    }

    /// <summary>
    /// Team 7 Feature 7: Time slow power-up service.
    /// </summary>
    public static class TimeSlowPowerUpSystem
    {
        /// <summary>Returns timescale while time-slow is active.</summary>
        /// <remarks>PHASE 3 - Team 7: Time Slow Power-Up</remarks>
        public static float GetTimeScale(bool active) => active ? 0.55f : 1.0f;
    }

    /// <summary>
    /// Team 7 Feature 8: Advanced invulnerability frames.
    /// </summary>
    public static class InvulnerabilityFramesAdvancedSystem
    {
        /// <summary>Returns i-frame duration from difficulty factor.</summary>
        /// <remarks>PHASE 3 - Team 7: Invulnerability Frames Advanced</remarks>
        public static float GetDuration(float difficultyScale)
        {
            return Math.Max(0.2f, 0.75f / Math.Max(0.6f, difficultyScale));
        }
    }

    /// <summary>
    /// Team 7 Feature 9: Double damage modifier.
    /// </summary>
    public static class DoubleDamageModifierSystem
    {
        /// <summary>Applies optional double damage modifier.</summary>
        /// <remarks>PHASE 3 - Team 7: Double Damage Modifier</remarks>
        public static int Apply(int baseDamage, bool enabled)
        {
            return enabled ? baseDamage * 2 : baseDamage;
        }
    }

    /// <summary>
    /// Team 7 Feature 10: Knockback resistance modifier.
    /// </summary>
    public static class KnockbackResistanceSystem
    {
        /// <summary>Returns adjusted knockback after resistance factor.</summary>
        /// <remarks>PHASE 3 - Team 7: Knockback Resistance</remarks>
        public static float Apply(float knockback, float resistance)
        {
            float r = Math.Max(0f, Math.Min(0.9f, resistance));
            return knockback * (1f - r);
        }
    }
}
