namespace Fridays_Adventure.Abilities
{
    /// <summary>
    /// Swan's evasive dash — launches her in the facing direction and applies
    /// a brief Dodging window (contact damage is resolved in the scene).
    /// </summary>
    public sealed class WingDash : Ability
    {
        public float DashSpeed     { get; } = 620f;
        public float DashDuration  { get; } = 0.22f;
        public int   ContactDamage { get; } = 18;

        public WingDash() : base("Wing Dash", 4f) { }

        protected override void OnUse(Entities.Character caster)
        {
            // Grant invincibility frames for the dash window
            caster.ApplyEffect(Entities.StatusEffect.Dodging, DashDuration);
            caster.VelocityX = caster.FacingRight ? DashSpeed : -DashSpeed;
        }
    }
}
