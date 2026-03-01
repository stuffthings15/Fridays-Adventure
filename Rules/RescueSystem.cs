using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;

namespace Fridays_Adventure.Rules
{
    public static class RescueSystem
    {
        // Time the player has to mash before auto-rescue triggers
        public static float GetMashWindow(int crewBonds)
        {
            if (crewBonds >= 6) return 1.0f; // Finn very close — fast rescue
            if (crewBonds >= 3) return 1.8f; // Some bond — moderate
            return 99f;                       // No bond — player must escape alone
        }

        // Returns true if a crewmate automatically rescues the player
        public static bool AutoRescueAvailable(int crewBonds) => crewBonds >= 3;

        // Called when rescue succeeds — boost bond and apply rescue
        public static void ApplyRescue(Player player, ref float sinkTimer)
        {
            player.RemoveEffect(StatusEffect.Sinking);
            player.VelocityY = -280f;
            sinkTimer        = 0f;
            Game.Instance.CrewBonds = System.Math.Min(10, Game.Instance.CrewBonds + 1);
            Game.Instance.Audio.BeepJump();
        }
    }
}
