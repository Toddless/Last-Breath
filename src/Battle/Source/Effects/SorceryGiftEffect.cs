namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;

    /// <summary>"Дар чародейства": ability costs are reduced by <c>value</c> (0.25 = −25%) per stack.</summary>
    public class SorceryGiftEffect(int duration, int maxStacks, float value)
        : ActivationModifierEffect(id: "Effect_Sorcery_Gift",
            duration,
            maxStacks,
            modifierFactory: () => new CostScaleActivationContextModifier(1 - value))
    {
        public override IEffect Copy() => new SorceryGiftEffect(Duration, MaxStacks, value);
    }
}
