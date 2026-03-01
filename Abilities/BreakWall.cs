namespace Fridays_Adventure.Abilities
{
    public sealed class BreakWall : Ability
    {
        public float Range { get; } = 70f;
        public int   ShockwaveDamage { get; } = 8;

        public BreakWall() : base("Break Wall", 2.5f)
        {
            BypassSuppression = true;
        }

        protected override void OnUse(Entities.Character caster) { }
    }
}
