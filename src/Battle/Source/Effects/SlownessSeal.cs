namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;

    /// <summary>"Печать замедления": every ability activation starts with +<c>amount</c> cooldown per stack.</summary>
    public class SlownessSeal(int duration, int maxStacks, float amount = 1)
        : ActivationModifierEffect(id: "Effect_Seal_Of_Slowness",
            duration,
            maxStacks,
            modifierFactory: () => new CooldownIncreaseActivationContextModifier(amount))
    {
        public override IEffect Copy() => new SlownessSeal(Duration, MaxStacks, amount);
    }
}
