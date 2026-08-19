namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;

    /// <summary>"Дар чародейства": ability costs are reduced by <c>value</c> (0.25 = −25%) per stack.</summary>
    public class SorceryGiftEffect(int duration, int maxStacks, EffectValue value)
        : ActivationModifierEffect(id: "Effect_Sorcery_Gift", duration, maxStacks)
    {
        public override IEffect Copy() => new SorceryGiftEffect(Duration, MaxStacks, value);

        /// <summary>The figure is what the cost LOSES, so the scale is one minus it — and the shape stops
        /// at nothing left, where the hand-rolled subtraction it replaces went on to pay the caster.</summary>
        protected override IAbilityActivationModifier CreateModifier() =>
            new CostScaleActivationContextModifier(Effective(value, EffectValueShape.ShareLost));
    }
}
