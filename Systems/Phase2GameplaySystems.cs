// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 7: Gameplay Programmer
// Feature: Gameplay Systems Pack
// Purpose: Implements Phase 2 movement/combat gameplay mechanics and helpers.
// ────────────────────────────────────────────────────────────────────────────

using System;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Wall-slide movement helper.
    /// </summary>
    /// <remarks>PHASE 2 - Team 7: Wall Slide Mechanic</remarks>
    public static class WallSlideMechanicSystem
    {
        /// <summary>Returns adjusted vertical speed while sliding on a wall.</summary>
        /// <remarks>PHASE 2 - Team 7: Wall Slide Mechanic</remarks>
        public static float Apply(float currentVy, float maxSlideSpeed = 2.8f)
        {
            return Math.Min(currentVy, Math.Max(0.5f, maxSlideSpeed));
        }
    }

    /// <summary>
    /// Air-dash velocity resolver.
    /// </summary>
    /// <remarks>PHASE 2 - Team 7: Air Dash</remarks>
    public static class AirDashSystem
    {
        /// <summary>Returns horizontal impulse from dash direction.</summary>
        /// <remarks>PHASE 2 - Team 7: Air Dash</remarks>
        public static float DashVelocity(int direction)
        {
            int dir = direction < 0 ? -1 : 1;
            return dir * 12f;
        }
    }

    /// <summary>
    /// Shield power-up model.
    /// </summary>
    /// <remarks>PHASE 2 - Team 7: Shield Power-Up</remarks>
    public static class ShieldPowerUpSystem
    {
        /// <summary>Current active shield points.</summary>
        public static int Points { get; private set; }

        /// <summary>Activates shield with a point budget.</summary>
        /// <remarks>PHASE 2 - Team 7: Shield Power-Up</remarks>
        public static void Activate(int points = 40)
        {
            Points = Math.Max(0, points);
        }

        /// <summary>Absorbs incoming damage and returns overflow damage.</summary>
        /// <remarks>PHASE 2 - Team 7: Shield Power-Up</remarks>
        public static int Absorb(int incoming)
        {
            int block = Math.Min(Math.Max(0, incoming), Points);
            Points -= block;
            return Math.Max(0, incoming - block);
        }
    }

    /// <summary>
    /// Rope-swing momentum helper.
    /// </summary>
    /// <remarks>PHASE 2 - Team 7: Rope Swing Mechanic</remarks>
    public static class RopeSwingMechanicSystem
    {
        /// <summary>Returns tangential velocity from swing angle and rope length.</summary>
        /// <remarks>PHASE 2 - Team 7: Rope Swing Mechanic</remarks>
        public static float TangentialVelocity(float angleRadians, float ropeLength)
        {
            float len = Math.Max(20f, ropeLength);
            return (float)(Math.Sin(angleRadians) * len * 0.06f);
        }
    }

    /// <summary>
    /// Magnetic platform pull-force helper.
    /// </summary>
    /// <remarks>PHASE 2 - Team 7: Magnetic Platforms</remarks>
    public static class MagneticPlatformsSystem
    {
        /// <summary>Returns magnetic pull force from distance to platform center.</summary>
        /// <remarks>PHASE 2 - Team 7: Magnetic Platforms</remarks>
        public static float PullForce(float distance)
        {
            float d = Math.Max(1f, distance);
            return Math.Max(0f, 14f - d * 0.08f);
        }
    }

    /// <summary>
    /// Spike-ball enemy damage helper.
    /// </summary>
    /// <remarks>PHASE 2 - Team 7: Spike Ball Enemy</remarks>
    public static class SpikeBallEnemySystem
    {
        /// <summary>Returns contact damage based on enemy speed.</summary>
        /// <remarks>PHASE 2 - Team 7: Spike Ball Enemy</remarks>
        public static int ContactDamage(float speed)
        {
            return 8 + (int)Math.Round(Math.Max(0f, speed) * 1.4f);
        }
    }

    /// <summary>
    /// Conveyor belt velocity modifier.
    /// </summary>
    /// <remarks>PHASE 2 - Team 7: Conveyor Belt Sequence</remarks>
    public static class ConveyorBeltSequenceSystem
    {
        /// <summary>Applies conveyor speed to player horizontal velocity.</summary>
        /// <remarks>PHASE 2 - Team 7: Conveyor Belt Sequence</remarks>
        public static float Apply(float currentVx, float beltSpeed)
        {
            return currentVx + beltSpeed * 0.35f;
        }
    }

    /// <summary>
    /// Portal mechanic transform helper.
    /// </summary>
    /// <remarks>PHASE 2 - Team 7: Portal Mechanic</remarks>
    public static class PortalMechanicSystem
    {
        /// <summary>Maps an input angle to output angle after portal traversal.</summary>
        /// <remarks>PHASE 2 - Team 7: Portal Mechanic</remarks>
        public static float TransformAngle(float angleRadians, float portalRotation)
        {
            return angleRadians + portalRotation;
        }
    }

    /// <summary>
    /// Slippery surface friction model.
    /// </summary>
    /// <remarks>PHASE 2 - Team 7: Slippery Surface</remarks>
    public static class SlipperySurfaceSystem
    {
        /// <summary>Returns velocity after one friction step on slippery terrain.</summary>
        /// <remarks>PHASE 2 - Team 7: Slippery Surface</remarks>
        public static float ApplyFriction(float vx, float friction = 0.94f)
        {
            float f = Math.Max(0.7f, Math.Min(0.99f, friction));
            return vx * f;
        }
    }

    /// <summary>
    /// Rocket launcher power-up damage model.
    /// </summary>
    /// <remarks>PHASE 2 - Team 7: Rocket Launcher Power-Up</remarks>
    public static class RocketLauncherPowerUpSystem
    {
        /// <summary>Returns explosion damage from charge seconds.</summary>
        /// <remarks>PHASE 2 - Team 7: Rocket Launcher Power-Up</remarks>
        public static int Damage(float chargeSeconds)
        {
            float c = Math.Max(0f, Math.Min(3f, chargeSeconds));
            return 35 + (int)Math.Round(c * 18f);
        }
    }
}
