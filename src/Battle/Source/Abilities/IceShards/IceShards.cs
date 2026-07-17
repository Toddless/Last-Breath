namespace Battle.Source.Abilities.IceShards
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

    /// <summary>
    /// Intelligence stance. Fires shards of ice at the target; activation stages are cumulative:
    /// 2 — empowered shards, 3 — shards hit every enemy, 4 — critical shards burst into shrapnel.
    /// All numbers live in <see cref="Parameters"/> modules, so upgrades are plain decorators.
    /// </summary>
    public class IceShards(AbilityBaseData data) : MulticastAbility<VolleyCastPlan>(data)
    {
        public float Damage => this[AbilityParameter.Damage];
        public float WeaponDamageScale => this[AbilityParameter.WeaponDamageScale];
        public float SpellDamageScale => this[AbilityParameter.SpellDamageScale];

        private const int EmpoweredShardsStage = 2;
        private const int AllTargetsStage = 3;
        private const int ShrapnelBurstStage = 4;

        public static class Parameters
        {
            public const string Shards = nameof(Shards);
            public const string ShrapnelDamage = nameof(ShrapnelDamage);
            public const string ShrapnelWeaponDamageScale = nameof(ShrapnelWeaponDamageScale);
            public const string ShrapnelSpellDamageScale = nameof(ShrapnelSpellDamageScale);
            public const string ShrapnelBarrierLeach = nameof(ShrapnelBarrierLeach);
            public const string SecondStageDamage = nameof(SecondStageDamage);
            public const string SecondStageWeaponDamageScale = nameof(SecondStageWeaponDamageScale);
            public const string SecondStageSpellDamageScale = nameof(SecondStageSpellDamageScale);
        }

        public int Shards => (int)this[Parameters.Shards];
        public float ShrapnelBarrierLeach => this[Parameters.ShrapnelBarrierLeach];

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            RegisterDamageParameters(parameters);
            parameters.RegisterDefault(Parameters.Shards, 3);
            parameters.RegisterDefault(Parameters.ShrapnelDamage, 50f);
            parameters.RegisterDefault(Parameters.ShrapnelWeaponDamageScale, 0.15f);
            parameters.RegisterDefault(Parameters.ShrapnelSpellDamageScale, 0.55f);
            parameters.RegisterDefault(Parameters.SecondStageDamage, 120f);
            parameters.RegisterDefault(Parameters.SecondStageWeaponDamageScale, 0.35f);
            parameters.RegisterDefault(Parameters.SecondStageSpellDamageScale, 1.2f);
            // Zero by default; the L3 upgrade raises it with a decorator — the ability knows nothing about the upgrade
            parameters.RegisterDefault(Parameters.ShrapnelBarrierLeach, 0f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new IceShards(Data));

        protected override VolleyCastPlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field) =>
            new()
            {
                ProjectilesCount = Shards,
                Damage = Damage,
                WeaponDamageScale = WeaponDamageScale,
                SpellDamageScale = SpellDamageScale,
                DamageType = DamageType.Cold,
                Targets = targets
            };

        protected override void ApplyStage(int stage, VolleyCastPlan plan, IFightable owner, IBattleField field)
        {
            switch (stage)
            {
                case EmpoweredShardsStage:
                    plan.Damage = Damage;
                    plan.WeaponDamageScale = WeaponDamageScale;
                    plan.SpellDamageScale = SpellDamageScale;
                    break;
                case AllTargetsStage:
                    plan.Targets = field.GetEnemies(owner).ToList();
                    break;
                case ShrapnelBurstStage:
                    plan.OnHitRiders.Add(hit => _ = DealShrapnelBurst(hit, owner, field));
                    break;
            }
        }

        /// <summary>Every landed shard is a full impact: plan riders (shrapnel) AND the ability's
        /// impact riders (fragility, buff-granted riders) fire per shard per target.</summary>
        protected override async Task ExecutePlan(VolleyCastPlan plan, IFightable owner, IBattleField field)
        {
            for (int projectile = 0; projectile < plan.ProjectilesCount; projectile++)
            {
                foreach (IFightable target in plan.Targets.Where(t => t.IsAlive).ToList())
                {
                    var hit = await DealPlanDamage(plan, owner, target);
                    foreach (var rider in plan.OnHitRiders)
                        rider(hit);
                    await ApplyImpactRiders(new AbilityImpact(owner, target, field, Succeeded: true, hit.IsCritical, hit.Damage));
                }
            }
        }

        /// <summary>Stage 4: a critical shard bursts, damaging every enemy on the field. Each victim is
        /// a regular impact (fragility/rider effects apply); bursts never spawn further bursts.</summary>
        private async Task DealShrapnelBurst(ProjectileHit hit, IFightable owner, IBattleField field)
        {
            if (!hit.IsCritical) return;

            float damage = this[Parameters.ShrapnelDamage]
                           + owner.Parameters.Damage * this[Parameters.ShrapnelWeaponDamageScale]
                           + owner.Parameters.SpellDamage * this[Parameters.ShrapnelSpellDamageScale];

            float totalDealt = 0;
            foreach (IFightable enemy in field.GetEnemies(owner))
            {
                var context = new DamageContext { Source = owner, Cause = DamageCause.Ability, CastId = CastId };
                context.Add(DamageType.Cold, damage);
                await enemy.TakeDamage(context);
                totalDealt += context.TotalDamage;
                await ApplyImpactRiders(new AbilityImpact(owner, enemy, field, Succeeded: true, IsCritical: false, context.TotalDamage));
            }

            if (ShrapnelBarrierLeach > 0)
                owner.CurrentBarrier += totalDealt * ShrapnelBarrierLeach;
        }
    }
}
