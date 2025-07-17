namespace Playground.Script.Abilities.Effects
{
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public class PrecisionEffect : EffectBase
    {
        public PrecisionEffect(int duration = 3, int stacks = 1, bool permanent = false) : base(effect: Effects.IncreasedAdditionalHit, duration, stacks, permanent)
        {
            Modifier = new AdditionalHitModifier(ModifierType.Flat, 0.2f, this, ModifierPriorities.Buffs);
        }
    }
}
