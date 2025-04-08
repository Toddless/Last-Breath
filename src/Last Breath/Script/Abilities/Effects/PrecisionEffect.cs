namespace Playground.Script.Abilities.Effects
{
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;

    public class PrecisionEffect( int duration = 3, int stacks = 1, bool permanent = false)
        : EffectBase(effect:Effects.IncreasedAdditionalHit,
            modifier: new AdditionalHitModifier(ModifierType.Additive, 0.5f, ModifierPriorities.Buffs),
            duration,
            stacks,
            permanent)
    {
    }
}
