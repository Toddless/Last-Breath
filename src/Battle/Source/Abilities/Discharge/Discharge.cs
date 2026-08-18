namespace Battle.Source.Abilities.Discharge
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Enums;
    using Effects;

    /// <summary>Cast plan of the Discharge: the barrier-conversion knobs.</summary>
    public class DischargePlan : DamagingCastPlan
    {
        public float BarrierMultiplier { get; set; }
        public float BarrierRestorePercent { get; set; }
    }

    /// <summary>
    /// Consumes the caster's ENTIRE current barrier and converts it into one lightning strike:
    /// barrier × multiplier + spell-damage scaling. Stage 2 raises the multiplier, stage 3 refunds
    /// part of the dealt damage as barrier, stage 4 turns the damage pure.
    /// </summary>
    public class Discharge(AbilityBaseData data) : MulticastAbility<DischargePlan>(data)
    {
        /// <summary>L3 upgrade point: the strike ignores elemental resistances.</summary>
        public bool AlwaysIgnoreResistances { get; set; }

        /// <summary>L3 upgrade point: the cast consumes MANA instead of barrier.</summary>
        public bool ConsumeManaInstead { get; set; }

        public static class Parameters
        {
            /// <summary>What one point of spent barrier is worth as damage — the rate that CREATES the
            /// damage, not a multiplier on damage already there.</summary>
            public const string BarrierMultiplier = nameof(BarrierMultiplier);

            public const string StageTwoMultiplierBonus = nameof(StageTwoMultiplierBonus);
            public const string StageThreeBarrierRestore = nameof(StageThreeBarrierRestore);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            RegisterDamageParameters(parameters);
            // The ability wears the 'recovery' tag; the stage-3 refund below is what it now scales.
            parameters.RegisterDefault(AbilityParameter.Effectiveness, 1f);
            parameters.RegisterDefault(Parameters.BarrierMultiplier, 1.5f);
            parameters.RegisterDefault(Parameters.StageTwoMultiplierBonus, 0.3f);
            parameters.RegisterDefault(Parameters.StageThreeBarrierRestore, 0.45f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new Discharge(Data)
        {
            AlwaysIgnoreResistances = AlwaysIgnoreResistances,
            ConsumeManaInstead = ConsumeManaInstead
        });

        protected override DischargePlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field) =>
            new()
            {
                Damage = this[AbilityParameter.Damage],
                WeaponDamageScale = this[AbilityParameter.WeaponDamageScale],
                SpellDamageScale = this[AbilityParameter.SpellDamageScale],
                DamageType = DamageType.Lightning,
                Targets = targets,
                BarrierMultiplier = this[Parameters.BarrierMultiplier],
                IgnoreResistances = AlwaysIgnoreResistances
            };

        protected override void ApplyStage(int stage, DischargePlan plan, IFightable owner, IBattleField field)
        {
            switch (stage)
            {
                case 2:
                    plan.BarrierMultiplier += this[Parameters.StageTwoMultiplierBonus];
                    break;
                case 3:
                    plan.BarrierRestorePercent = this[Parameters.StageThreeBarrierRestore];
                    break;
                case 4:
                    plan.DamageType = DamageType.Sacred;
                    break;
            }
        }

        protected override async Task ExecutePlan(DischargePlan plan, IFightable owner, IBattleField field)
        {
            float absorbed = ConsumeResourcePool(owner);
            plan.Damage += absorbed * plan.BarrierMultiplier;

            foreach (IFightable target in plan.Targets.Where(t => t.IsAlive).ToList())
            {
                var hit = await DealPlanDamage(plan, owner, target);
                if (plan.BarrierRestorePercent > 0)
                    await new BarrierFromDamageEffect(plan.BarrierRestorePercent, hit.Damage).Apply(Laying(owner));

                await ApplyImpactRiders(new AbilityImpact(owner, target, field, Succeeded: true, hit.IsCritical, hit.Damage)
                {
                    Source = this,
                    Kind = ImpactKind.Hit
                });
            }
        }

        private float ConsumeResourcePool(IFightable owner)
        {
            if (ConsumeManaInstead)
            {
                float mana = owner.CurrentMana;
                owner.CurrentMana = 0;
                return mana;
            }

            float barrier = owner.CurrentBarrier;
            owner.CurrentBarrier = 0;
            return barrier;
        }
    }
}
