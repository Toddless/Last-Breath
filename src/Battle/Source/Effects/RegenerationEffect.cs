namespace Battle.Source.Effects
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Godot;

    public class RegenerationEffect(
        float amount,
        int duration,
        int maxStacks,
        bool isPercent = false,
        StatusEffects statusEffect = StatusEffects.Regeneration)
        : Effect(id: "Effect_Regeneration", duration, maxStacks, statusEffect)
    {

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                float totalRegeneration = Target?.Effects.GetBy(effect => effect.Id == Id).Cast<RegenerationEffect>().Sum(effect => effect.Amount) ?? Amount;
                values["Amount"] = Mathf.RoundToInt(totalRegeneration);
                return values;
            }
        }

        public bool IsPercent => isPercent;
        public float Amount { get; } = amount;

        public override void TurnEnd()
        {
            Target?.Heal(!isPercent ? new HealContext(Target, Target) { Amount = Amount } : new HealContext(Target, Target) { Amount = Target.Parameters.MaxHealth * Amount });
            base.TurnEnd();
        }


        public override IEffect Copy() => new RegenerationEffect(Amount, Duration, MaxStacks, IsPercent, Status);
    }
}
