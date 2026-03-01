namespace Fridays_Adventure.Abilities
{
    public sealed class FlashFreeze : Ability
    {
        public float Range { get; } = 130f;
        public FlashFreeze() : base("Flash Freeze", 6f) { }
        protected override void OnUse(Entities.Character caster) { }
    }
}
