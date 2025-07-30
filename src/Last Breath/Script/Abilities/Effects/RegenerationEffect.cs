namespace Playground.Script.Abilities.Effects
{
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public class RegenerationEffect : EffectBase
    {
        public RegenerationEffect(int duration = 3, int stacks = 1, bool permanent = false) : base(effect: Effects.Regeneration,
                duration,
                stacks,
                permanent)
        {
            Modifier = new CurrentHealthModifier(ModifierType.Flat, 80, this, ModifierPriorities.Buffs);
        }

        public override void OnTick(ICharacter character)
        {
            base.OnTick(character);
            character.Health.Heal(Modifier.Value);
        }
    }
}
