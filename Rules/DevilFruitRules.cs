using System.Collections.Generic;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Hazards;

namespace Fridays_Adventure.Rules
{
    // Centralises all Devil-Fruit rule checks so scenes call one method
    public static class DevilFruitRules
    {
        public static void Check(Character character, IEnumerable<Hazard> hazards, float dt)
        {
            bool inWater    = false;
            bool inSeaston  = false;

            foreach (var hz in hazards)
            {
                if (!hz.IsActive || !hz.Overlaps(character)) continue;
                hz.ApplyEffect(character, dt);
                if (hz.Type == HazardType.WaterPit)    inWater   = true;
                if (hz.Type == HazardType.SeaStoneZone) inSeaston = true;
            }

            // If no longer in hazard, lift the status
            if (!inWater   && character.HasEffect(StatusEffect.Sinking))
            {
                // Only remove if timer expired (handled by Character.TickEffects)
            }
            if (!inSeaston && character.HasEffect(StatusEffect.Suppressed))
            {
                // Suppression naturally expires between frames (0.2s refresh)
            }
        }

        public static void ApplySuppression(Character c, float dt)
        {
            c.ApplyEffect(StatusEffect.Suppressed, 0.2f);
            foreach (var a in c.Abilities)
                a.TickCooldown(-dt); // freeze cooldowns while suppressed
        }
    }
}
