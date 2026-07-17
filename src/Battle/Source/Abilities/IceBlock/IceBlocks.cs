namespace Battle.Source.Abilities.IceBlock
{
    using System;
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
    using Effects;

    /// <summary>Cast plan of the Ice Block: the volley fields plus the stage-mutable stun length.</summary>
    public class IceBlockPlan : DamagingCastPlan
    {
        public int StunDuration { get; set; }
        public List<Action<ProjectileHit>> OnHitRiders { get; } = [];
    }

    /// <summary>
    /// Drops a huge ice block on the target: one heavy cold hit that stuns. Stage 2 extends the stun,
    /// stage 3 adds a Withering Curse stack, stage 4 drops three extra blocks at half damage.
    /// </summary>
    public class IceBlocks(AbilityBaseData data) : MulticastAbility<IceBlockPlan>(data)
    {
        public float Damage => this[AbilityParameter.Damage];
        public float WeaponDamageScale => this[AbilityParameter.WeaponDamageScale];
        public float SpellDamageScale => this[AbilityParameter.SpellDamageScale];
        public int StunDuration => (int)this[Parameters.StunDuration];
        public int ExtraBlocks => (int)this[Parameters.ExtraBlocks];

        public static class Parameters
        {
            public const string StunDuration = nameof(StunDuration);
            public const string WitheringDuration = nameof(WitheringDuration);
            public const string WitheringMaxStacks = nameof(WitheringMaxStacks);
            public const string WitheringValue = nameof(WitheringValue);
            public const string ExtraBlocks = nameof(ExtraBlocks);
            public const string ExtraBlockDamagePercent = nameof(ExtraBlockDamagePercent);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            RegisterDamageParameters(parameters);
            parameters.RegisterDefault(Parameters.StunDuration, 1);
            parameters.RegisterDefault(Parameters.WitheringDuration, 3);
            parameters.RegisterDefault(Parameters.WitheringMaxStacks, 3);
            parameters.RegisterDefault(Parameters.WitheringValue, 0.15f);
            parameters.RegisterDefault(Parameters.ExtraBlocks, 3);
            parameters.RegisterDefault(Parameters.ExtraBlockDamagePercent, 0.5f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new IceBlocks(Data));

        /// <summary>One heavy block per target: the hit stuns (base plan rider), stage riders and the
        /// ability's impact riders fire per crushed target.</summary>
        protected override async Task ExecutePlan(IceBlockPlan plan, IFightable owner, IBattleField field)
        {
            foreach (IFightable target in plan.Targets.Where(t => t.IsAlive).ToList())
            {
                var hit = await DealPlanDamage(plan, owner, target);
                foreach (var rider in plan.OnHitRiders)
                    rider(hit);
                await ApplyImpactRiders(new AbilityImpact(owner, target, field, Succeeded: true, hit.IsCritical, hit.Damage));
            }
        }

        protected override IceBlockPlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            var plan = new IceBlockPlan
            {
                Damage = Damage,
                WeaponDamageScale = WeaponDamageScale,
                SpellDamageScale = SpellDamageScale,
                DamageType = DamageType.Cold,
                Targets = targets,
                StunDuration = StunDuration
            };
            // The rider closes over the plan: stage 2 mutations to StunDuration are picked up automatically.
            plan.OnHitRiders.Add(hit => _ = new StunEffect(plan.StunDuration)
                .Apply(new EffectApplyingContext { Caster = owner, Target = hit.Target, Source = InstanceId }));
            return plan;
        }

        protected override void ApplyStage(int stage, IceBlockPlan plan, IFightable owner, IBattleField field)
        {
            switch (stage)
            {
                case 2:
                    plan.StunDuration += 1;
                    break;
                case 3:
                    plan.OnHitRiders.Add(hit => _ = new WitheringCurseEffect(
                            (int)this[Parameters.WitheringDuration], (int)this[Parameters.WitheringMaxStacks], this[Parameters.WitheringValue])
                        .Apply(new EffectApplyingContext { Caster = owner, Target = hit.Target, Source = InstanceId }));
                    break;
                case 4:
                    plan.OnHitRiders.Add(hit => DropExtraBlocks(plan, owner, hit.Target));
                    break;
            }
        }



        /// <summary>Stage 4: three more blocks crash down, each at a share of the main block's damage.</summary>
        private void DropExtraBlocks(IceBlockPlan plan, IFightable owner, IFightable target)
        {
            float blockDamage = CalculateHitDamage(plan, owner) * this[Parameters.ExtraBlockDamagePercent];
            for (int i = 0; i < ExtraBlocks; i++)
            {
                if (!target.IsAlive) return;
                var context = new DamageContext { Source = owner, Cause = DamageCause.Ability, CastId = CastId };
                context.Add(DamageType.Cold, blockDamage);
                _ = target.TakeDamage(context);
            }
        }
    }
}
