namespace Battle.Source.Effects
{
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Modifiers.Context;

    /// <summary>"Curse": every ability activation costs a flat <c>costIncrease</c> more.</summary>
    public class CurseEffect(int duration, int maxStacks, EffectValue costIncrease = default)
        : ActivationModifierEffect(id: "Effect_Curse", duration, maxStacks, statusEffect: StatusEffects.Cursed)
    {
        public override bool IsHarmful => true;

        /// <summary>What each activation actually costs on top. A curse is not a seal — the load is a
        /// price and not a count of turns, so it is not rounded down to anything.</summary>
        public float CostIncrease => Effective(Authored);

        /// <summary>The default stands in for the parameterless struct default.</summary>
        private EffectValue Authored { get; } = costIncrease.Authored == 0f ? 150f : costIncrease;

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values[nameof(costIncrease)] = CostIncrease;
                return values;
            }
        }

        public override IEffect Copy() => new CurseEffect(Duration, MaxStacks, Authored);

        protected override IAbilityActivationModifier CreateModifier() => new FlatCostActivationContextModifier(CostIncrease);
    }
}
