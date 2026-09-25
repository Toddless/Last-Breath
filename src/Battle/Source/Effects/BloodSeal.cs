namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Modifiers.Context;

    /// <summary>"Печать крови": while sealed, abilities are paid with HEALTH instead of their own resource.</summary>
    public class BloodSeal(int duration, int maxStacks = 1)
        : ActivationModifierEffect(id: "Effect_Seal_Of_Blood", duration, maxStacks, statusEffect: StatusEffects.Cursed)
    {
        public override bool IsHarmful => true;

        public override IEffect Copy() => new BloodSeal(Duration, MaxStacks);

        /// <summary>The seal swaps which resource pays and names no figure, so there is nothing here for
        /// effectiveness to multiply.</summary>
        protected override IAbilityActivationModifier CreateModifier() => new CostTypeActivationContextModifier(Costs.Health);
    }
}
