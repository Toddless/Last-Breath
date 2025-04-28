namespace Playground.Script.Abilities.Effects
{
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;

    public class GoliathEffect : EffectBase
    {
        public GoliathEffect(int duration = 3, int stacks = 1, bool permanent = false)
            : base(effect: Effects.HealthIncrease,
                  duration,
                  stacks,
                  permanent)
        {
            Modifier = new MaxHealthModifier(ModifierType.Multiplicative, 1.1f, this ,ModifierPriorities.Buffs);
        }
    }
}
