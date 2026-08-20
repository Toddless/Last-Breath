namespace Battle.Source.Effects
{
    using System;
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;

    /// <summary>"Печать замедления": every ability activation starts with +<c>amount</c> cooldown.</summary>
    public class SlownessSeal(int duration, int maxStacks, EffectValue amount)
        : ActivationModifierEffect(id: "Effect_Seal_Of_Slowness", duration, maxStacks)
    {
        public override bool IsHarmful => true;

        /// <summary>Turns the seal actually adds. Effectiveness multiplies the load a seal carries like
        /// any other, but turns are counted and not measured: the product is taken DOWN to whole turns,
        /// so half a turn of extra cooldown is no turn at all.</summary>
        public float Amount => MathF.Floor(Effective(amount));

        public override IEffect Copy() => new SlownessSeal(Duration, MaxStacks, amount);

        protected override IAbilityActivationModifier CreateModifier() => new CooldownIncreaseActivationContextModifier(Amount);
    }
}
