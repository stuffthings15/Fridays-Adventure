// ────────────────────────────────────────────────────────────────────────────
// Abilities/FrostBall.cs
// Purpose: Frost Ball ability — blue fireball projectile available to all
//          characters on the X key with a 1-second cooldown.
// ────────────────────────────────────────────────────────────────────────────

namespace Fridays_Adventure.Abilities
{
    /// <summary>
    /// Frost Ball ability — shoots a blue ice projectile on X key press.
    /// Available to all playable characters with a 2-second cooldown.
    /// </summary>
    public sealed class FrostBall : Ability
    {
        public FrostBall() : base("Frost Ball", 2.0f) { }

        protected override void OnUse(Entities.Character caster) 
        {
            // Create and add Frost Ball projectile
            // This will be handled in the scene where the ability is used
        }
    }
}
