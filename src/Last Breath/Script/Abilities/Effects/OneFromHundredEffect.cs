namespace Playground.Script.Abilities.Effects
{
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;

    public class OneFromHundredEffect : EffectBase
    {
        public OneFromHundredEffect(int duration = 3, int stacks = 1, bool permanent = false)
            : base(effect: Effects.None, duration, stacks, permanent)
        {
            // for 3 turns character have 99% chance to evade but only 1 health
            Modifier = new EvadeModifier(ModifierType.Additive, 0.99f, this);
        }

        public override void OnApply(ICharacter character)
        {
            base.OnApply(character);
            character.Health.CurrentHealth = 1;
        }
    }
}
