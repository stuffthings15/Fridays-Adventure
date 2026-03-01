using System;
using System.Collections.Generic;
using Fridays_Adventure.Abilities;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Hazards;

namespace Fridays_Adventure.Rules
{
    public static class IceSystem
    {
        // Call every frame — updates walls and applies fire interaction
        public static void Update(List<IceWallInstance> walls,
                                  IEnumerable<Hazard>   hazards,
                                  Player                player,
                                  float                 dt)
        {
            var fires = new List<FireSource>();
            foreach (var hz in hazards)
                if (hz is FireSource f) fires.Add(f);

            // Melt walls near fire
            for (int i = walls.Count - 1; i >= 0; i--)
            {
                bool nearFire = false;
                foreach (var f in fires)
                    if (f.IsNear(walls[i].X + walls[i].Width  * 0.5f,
                                 walls[i].Y + walls[i].Height * 0.5f))
                    { nearFire = true; break; }

                walls[i].Update(dt, nearFire);
                if (!walls[i].IsAlive) walls.RemoveAt(i);
            }

            // Drain ice reserve near fire and raise melt risk
            foreach (var f in fires)
            {
                if (f.IsNear(player.CenterX, player.CenterY))
                {
                    player.MeltRisk    = Math.Min(1f, player.MeltRisk + dt * 0.25f);
                    player.IceReserve  = Math.Max(0, player.IceReserve - (int)(18 * dt));
                }
            }

            // Passive melt-risk recovery when away from fire
            if (!player.HasEffect(StatusEffect.Burning))
                player.MeltRisk = Math.Max(0f, player.MeltRisk - dt * 0.08f);
        }
    }
}
