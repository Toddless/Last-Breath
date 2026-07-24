namespace Battle.Source.Effects
{
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Modifiers.Context;

    /// <summary>"Curse": every ability activation costs a flat <c>costIncrease</c> more per stack.</summary>
    public class CurseEffect(int duration, int maxStacks, float costIncrease = 150)
        : ActivationModifierEffect(id: "Effect_Curse",
            duration,
            maxStacks,
            modifierFactory: () => new FlatCostActivationContextModifier(costIncrease),
            statusEffect: StatusEffects.Cursed)
    {
        public override bool IsHarmful => true;

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values[nameof(costIncrease)] = costIncrease;
                return values;
            }
        }

        public override IEffect Copy() => new CurseEffect(Duration, MaxStacks, costIncrease);
    }
}
