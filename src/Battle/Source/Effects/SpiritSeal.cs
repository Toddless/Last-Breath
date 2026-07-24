namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Modifiers.Context;

    /// <summary>"Spirit Seal": while sealed, abilities are paid with BARRIER instead of their own resource.</summary>
    public class SpiritSeal(int duration, int maxStacks = 1)
        : ActivationModifierEffect(id: "Effect_Seal_Of_Spirit",
            duration,
            maxStacks,
            modifierFactory: () => new CostTypeActivationContextModifier(Costs.Barrier),
            statusEffect: StatusEffects.Cursed)
    {
        public override bool IsHarmful => true;

        public override IEffect Copy() => new SpiritSeal(Duration, MaxStacks);
    }
}
