namespace Playground.Script.Abilities.Effects
{
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;

    public class RegenerationEffect(int duration = 3, int stacks = 1, bool permanent = false)
        : EffectBase(effect: Effects.Regeneration,
            modifier: new CurrentHealthModifier(ModifierType.Additive, 80, ModifierPriorities.Buffs),
            duration,
            stacks,
            permanent)
    {
        public override void OnTick(ICharacter character)
        {
            base.OnTick(character);
            character.Health.Heal(Modifier.Value);
        }
    }
}
