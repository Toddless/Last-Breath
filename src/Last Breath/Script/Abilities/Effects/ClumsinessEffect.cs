namespace Playground.Script.Abilities.Effects
{
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;

    public class ClumsinessEffect(int duration = 3, int stacks = 1, bool permanent = false)
        :EffectBase(effect: Effects.ReducedEvasion,
            modifier: new DodgeModifier(ModifierType.Multiplicative, 1.1f, ModifierPriorities.Debuffs),
            duration,
            stacks,
            permanent)
    {
    }
}
