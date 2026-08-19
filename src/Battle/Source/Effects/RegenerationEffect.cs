namespace Battle.Source.Effects
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Godot;

    public class RegenerationEffect(
        EffectValue amount,
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

        /// <summary>Health one turn gives back — a share of the maximum when <see cref="IsPercent"/>,
        /// a flat figure otherwise.</summary>
        public float Amount => Effective(amount);

        public override void TurnEnd()
        {
            Target?.Heal(!isPercent ? new HealContext(Target, Target) { Amount = Amount } : new HealContext(Target, Target) { Amount = Target.Parameters.MaxHealth * Amount });
            base.TurnEnd();
        }


        // The AUTHORED figure, never the effective one: a copy built from what this instance came to
        // would be scaled a second time the moment it landed.
        public override IEffect Copy() => new RegenerationEffect(amount, Duration, MaxStacks, IsPercent, Status);
    }
}
