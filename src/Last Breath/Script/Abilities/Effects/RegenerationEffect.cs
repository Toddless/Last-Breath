namespace LastBreath.Script.Abilities.Effects
{
    using Core.Enums;
    using Core.Modifiers;
    using LastBreath.Script.Helpers;
    using LastBreath.Script.Abilities;
    using Core.Interfaces.Entity;

    public class RegenerationEffect : EffectBase
    {
        public RegenerationEffect(int duration = 3, int stacks = 1, bool permanent = false) : base(effect: Effects.Regeneration,
                duration,
                stacks,
                permanent)
        {
            Modifier = new ModifierInstance(Parameter.Health, ModifierType.Flat, 80, this, ModifierPriorities.Buffs);
        }

        public override void OnTick(IEntity character)
        {
            base.OnTick(character);
            character.Health.Heal(Modifier.Value);
        }
    }
}
