// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 4: Lead Game Designer
// Feature: Design Systems Pack
// Purpose: Implements core Phase 2 design mechanics for movement/combat/risk.
// ────────────────────────────────────────────────────────────────────────────

using System;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Energy meter model for sprint/ability gating.
    /// </summary>
    /// <remarks>PHASE 2 - Team 4: Energy Meter System</remarks>
    public static class EnergyMeterSystem
    {
        /// <summary>Current energy value.</summary>
        /// <remarks>PHASE 2 - Team 4: Energy Meter System</remarks>
        public static float Energy { get; private set; } = 100f;

        /// <summary>Consumes energy and returns true if successful.</summary>
        /// <remarks>PHASE 2 - Team 4: Energy Meter System</remarks>
        public static bool Consume(float amount)
        {
            float a = Math.Max(0f, amount);
            if (Energy < a) return false;
            Energy -= a;
            return true;
        }

        /// <summary>Regenerates energy by delta time.</summary>
        /// <remarks>PHASE 2 - Team 4: Energy Meter System</remarks>
        public static void Regen(float dt, float ratePerSec = 10f)
        {
            Energy = Math.Min(100f, Energy + Math.Max(0f, dt) * Math.Max(0f, ratePerSec));
        }
    }

    /// <summary>
    /// Combo multiplier decay utility.
    /// </summary>
    /// <remarks>PHASE 2 - Team 4: Combo Multiplier Decay</remarks>
    public static class ComboMultiplierDecaySystem
    {
        /// <summary>Computes decayed combo multiplier over elapsed seconds.</summary>
        /// <remarks>PHASE 2 - Team 4: Combo Multiplier Decay</remarks>
        public static float Decay(float startMultiplier, float elapsedSeconds, float halfLifeSeconds = 3f)
        {
            float t = Math.Max(0f, elapsedSeconds);
            float hl = Math.Max(0.1f, halfLifeSeconds);
            float factor = (float)Math.Pow(0.5, t / hl);
            return Math.Max(1f, startMultiplier * factor);
        }
    }

    /// <summary>
    /// Momentum-based jumping adjustment helper.
    /// </summary>
    /// <remarks>PHASE 2 - Team 4: Momentum-Based Jumping</remarks>
    public static class MomentumJumpingSystem
    {
        /// <summary>Returns jump velocity based on horizontal speed ratio.</summary>
        /// <remarks>PHASE 2 - Team 4: Momentum-Based Jumping</remarks>
        public static float JumpVelocity(float baseJump, float speedRatio)
        {
            float s = Math.Max(0f, Math.Min(1.5f, speedRatio));
            return baseJump * (1f + s * 0.22f);
        }
    }

    /// <summary>
    /// Drift mechanic strength calculator.
    /// </summary>
    /// <remarks>PHASE 2 - Team 4: Drift Mechanic</remarks>
    public static class DriftMechanicSystem
    {
        /// <summary>Returns lateral drift correction from input and current velocity.</summary>
        /// <remarks>PHASE 2 - Team 4: Drift Mechanic</remarks>
        public static float Drift(float inputAxis, float currentVelocity)
        {
            float target = inputAxis * 8f;
            return (target - currentVelocity) * 0.18f;
        }
    }

    /// <summary>
    /// Power scaling for damage values.
    /// </summary>
    /// <remarks>PHASE 2 - Team 4: Power Scaling</remarks>
    public static class PowerScalingSystem
    {
        /// <summary>Applies progression scaling to base damage.</summary>
        /// <remarks>PHASE 2 - Team 4: Power Scaling</remarks>
        public static int ScaleDamage(int baseDamage, int level)
        {
            int l = Math.Max(1, level);
            return (int)Math.Round(baseDamage * (1.0 + (l - 1) * 0.08));
        }
    }

    /// <summary>
    /// Parry timing-window evaluator.
    /// </summary>
    /// <remarks>PHASE 2 - Team 4: Parry System</remarks>
    public static class ParrySystem
    {
        /// <summary>Returns true if timing is inside successful parry window.</summary>
        /// <remarks>PHASE 2 - Team 4: Parry System</remarks>
        public static bool IsParry(float secondsFromImpact, float window = 0.12f)
        {
            return Math.Abs(secondsFromImpact) <= Math.Max(0.03f, window);
        }
    }

    /// <summary>
    /// Grapple-hook range and cooldown model.
    /// </summary>
    /// <remarks>PHASE 2 - Team 4: Grapple Hook</remarks>
    public static class GrappleHookSystem
    {
        /// <summary>Maximum grapple range in world units.</summary>
        public const float MaxRange = 280f;

        /// <summary>Returns cooldown seconds from travel distance.</summary>
        /// <remarks>PHASE 2 - Team 4: Grapple Hook</remarks>
        public static float Cooldown(float distance)
        {
            float d = Math.Max(0f, Math.Min(MaxRange, distance));
            return 0.4f + d / MaxRange * 0.6f;
        }
    }

    /// <summary>
    /// Stamina system utility.
    /// </summary>
    /// <remarks>PHASE 2 - Team 4: Stamina System</remarks>
    public static class StaminaSystem
    {
        /// <summary>Returns post-action stamina value.</summary>
        /// <remarks>PHASE 2 - Team 4: Stamina System</remarks>
        public static float ApplyCost(float current, float cost)
        {
            return Math.Max(0f, current - Math.Max(0f, cost));
        }
    }

    /// <summary>
    /// Knockback multiplier helper.
    /// </summary>
    /// <remarks>PHASE 2 - Team 4: Knockback Multiplier</remarks>
    public static class KnockbackMultiplierSystem
    {
        /// <summary>Applies knockback multiplier and returns final force.</summary>
        /// <remarks>PHASE 2 - Team 4: Knockback Multiplier</remarks>
        public static float Apply(float baseForce, float multiplier)
        {
            return baseForce * Math.Max(0f, multiplier);
        }
    }

    /// <summary>
    /// Risk/reward scoring calculator.
    /// </summary>
    /// <remarks>PHASE 2 - Team 4: Risk/Reward Scoring</remarks>
    public static class RiskRewardScoringSystem
    {
        /// <summary>Returns adjusted score from base score and risk tier.</summary>
        /// <remarks>PHASE 2 - Team 4: Risk/Reward Scoring</remarks>
        public static int Score(int baseScore, int riskTier)
        {
            int r = Math.Max(0, Math.Min(3, riskTier));
            float mul = r == 0 ? 1.0f : r == 1 ? 1.2f : r == 2 ? 1.45f : 1.75f;
            return (int)Math.Round(baseScore * mul);
        }
    }
}
