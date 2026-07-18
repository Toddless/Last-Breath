namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Modifiers.Context;

    /// <summary>"Печать крови": while sealed, abilities are paid with HEALTH instead of their own resource.</summary>
    public class BloodSeal(int duration, int maxStacks = 1)
        : ActivationModifierEffect(id: "Effect_Seal_Of_Blood",
            duration,
            maxStacks,
            modifierFactory: () => new CostTypeActivationContextModifier(Costs.Health),
            statusEffect: StatusEffects.Cursed)
    {
        public override IEffect Copy() => new BloodSeal(Duration, MaxStacks);
    }
}
