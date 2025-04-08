namespace Playground.Script.Abilities.Effects
{
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;

    public class GoliathEffect(int duration = 3, int stacks = 1, bool permanent = false)
        : EffectBase(effect: Effects.HealthIncrease,
              modifier: new MaxHealthModifier(ModifierType.Multiplicative, 0.1f, ModifierPriorities.Buffs),
              duration,
              stacks,
              permanent)
    {
    }
}
