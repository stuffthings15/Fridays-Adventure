namespace Fridays_Adventure.Abilities
{
    public abstract class Ability
    {
        public string Name        { get; protected set; }
        public float  MaxCooldown { get; protected set; }
        public float  Cooldown    { get; private set; }
        public bool   IsReady     => Cooldown <= 0;
        public float  Progress    => MaxCooldown > 0 ? 1f - (Cooldown / MaxCooldown) : 1f;
        protected bool BypassSuppression { get; set; }

        protected Ability(string name, float cooldown)
        {
            Name        = name;
            MaxCooldown = cooldown;
        }

        public bool TryUse(Entities.Character caster)
        {
            if (!IsReady) return false;
            if (!BypassSuppression && caster.HasEffect(Entities.StatusEffect.Suppressed)) return false;
            OnUse(caster);
            Cooldown = MaxCooldown;
            return true;
        }

        protected abstract void OnUse(Entities.Character caster);

        public void TickCooldown(float dt)
        {
            if (Cooldown > 0) Cooldown -= dt;
        }

        public void ResetCooldown() => Cooldown = 0;
    }
}
