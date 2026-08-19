namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;

    /// <summary>"Clouded Mind": ability costs are increased by <c>value</c> (0.25 = +25%) per stack.</summary>
    public class CloudedMindEffect(int duration, int maxStacks, EffectValue value)
        : ActivationModifierEffect(id: "Effect_Clouded_Mind", duration, maxStacks)
    {
        public override bool IsHarmful => true;

        public override IEffect Copy() => new CloudedMindEffect(Duration, MaxStacks, value);

        protected override IAbilityActivationModifier CreateModifier() =>
            new CostScaleActivationContextModifier(Effective(value, EffectValueShape.ShareGained));
    }
}
