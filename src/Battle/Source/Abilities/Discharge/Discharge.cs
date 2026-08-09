namespace Battle.Source.Abilities.Discharge
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Enums;

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

        /// <summary>L3 upgrade point: overkill damage jumps to a random other enemy.</summary>
        public bool OverkillToRandom { get; set; }

        /// <summary>L3 upgrade point: the cast consumes MANA instead of barrier.</summary>
        public bool ConsumeManaInstead { get; set; }

        public static class Parameters
        {
            public const string BarrierMultiplier = nameof(BarrierMultiplier);
            public const string StageTwoMultiplierBonus = nameof(StageTwoMultiplierBonus);
            public const string StageThreeBarrierRestore = nameof(StageThreeBarrierRestore);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            RegisterDamageParameters(parameters);
            parameters.RegisterDefault(Parameters.BarrierMultiplier, 1.5f);
            parameters.RegisterDefault(Parameters.StageTwoMultiplierBonus, 0.3f);
            parameters.RegisterDefault(Parameters.StageThreeBarrierRestore, 0.45f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new Discharge(Data)
        {
            AlwaysIgnoreResistances = AlwaysIgnoreResistances,
            OverkillToRandom = OverkillToRandom,
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
                    plan.DamageType = DamageType.Pure;
                    break;
            }
        }

        protected override async Task ExecutePlan(DischargePlan plan, IFightable owner, IBattleField field)
        {
            float absorbed = ConsumeResourcePool(owner);
            plan.Damage += absorbed * plan.BarrierMultiplier;

            foreach (IFightable target in plan.Targets.Where(t => t.IsAlive).ToList())
            {
                float healthBefore = target.CurrentHealth;
                float barrierBefore = target.CurrentBarrier;

                var hit = await DealPlanDamage(plan, owner, target);
                if (plan.BarrierRestorePercent > 0)
                    owner.CurrentBarrier += hit.Damage * plan.BarrierRestorePercent;
                await TrySplashOverkill(plan, owner, field, target, hit.Damage, healthBefore + barrierBefore);

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

        /// <summary>L3 upgrade: whatever exceeded the victim's remaining pool jumps to a random other enemy.
        /// Nobody aimed the leap and it exists only because the strike had already landed — splash by the
        /// letter of the dictionary, and its victim is a touched target like any other.</summary>
        private async Task TrySplashOverkill(DischargePlan plan, IFightable owner, IBattleField field, IFightable victim, float dealt, float victimPool)
        {
            if (!OverkillToRandom || victim.IsAlive) return;
            float overkill = dealt - victimPool;
            if (overkill <= 0) return;

            var others = field.GetEnemies(owner).Where(enemy => enemy.IsAlive && !enemy.IsSame(victim.InstanceId)).ToList();
            if (others.Count == 0) return;

            var context = new DamageContext
            {
                Source = owner,
                Cause = DamageCause.Ability,
                CastId = CastId,
                IgnoreResistances = plan.IgnoreResistances
            };
            context.Add(plan.DamageType, overkill);
            IFightable neighbour = others[CombatRandom.Rolls.RandIntRange(0, others.Count - 1)];
            await neighbour.TakeDamage(context);
            await ApplyImpactRiders(new AbilityImpact(owner, neighbour, field, Succeeded: true, IsCritical: false, context.TotalDamage)
            {
                Source = this,
                Kind = ImpactKind.Splash
            });
        }
    }
}
