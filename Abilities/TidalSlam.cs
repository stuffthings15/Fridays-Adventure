namespace Fridays_Adventure.Abilities
{
    /// <summary>
    /// Orca's signature move — a ground-pound shockwave that damages every
    /// enemy inside a radius around the caster.  The scene is responsible for
    /// querying SlamHitbox and applying the damage; OnUse is kept minimal so
    /// the cooldown pattern stays consistent with IceWall / FlashFreeze.
    /// </summary>
    public sealed class TidalSlam : Ability
    {
        public float Radius     { get; } = 120f;
        public int   SlamDamage { get; } = 30;

        public TidalSlam() : base("Tidal Slam", 5f) { }

        protected override void OnUse(Entities.Character caster) { }
    }
}
