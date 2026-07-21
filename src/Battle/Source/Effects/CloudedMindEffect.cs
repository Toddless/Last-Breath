namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;

    /// <summary>"Clouded Mind": ability costs are increased by <c>value</c> (0.25 = +25%) per stack.</summary>
    public class CloudedMindEffect(int duration, int maxStacks, float value)
        : ActivationModifierEffect(id: "Effect_Clouded_Mind",
            duration,
            maxStacks,
            modifierFactory: () => new CostScaleActivationContextModifier(1 + value))
    {
        public override IEffect Copy() => new CloudedMindEffect(Duration, MaxStacks, value);
    }
}
