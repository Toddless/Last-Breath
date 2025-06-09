namespace Playground.Script.Abilities.Effects
{
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;

    public class ClumsinessEffect : EffectBase
    {
        public ClumsinessEffect(int duration = 3, int stacks = 1, bool permanent = false)
            : base(effect: Effects.ReducedEvasion,
                duration,
                stacks,
                permanent)
        {
            Modifier = new EvadeModifier(ModifierType.Multiplicative, 1.1f, this, ModifierPriorities.Debuffs);
        }
    }
}
