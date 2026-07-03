namespace Battle.Source.Abilities.IceShards
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Decorators;
    using Module;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Entity;

    /// <summary>
    /// Intelligence stance. Fires shards of ice at the target; activation stages are cumulative:
    /// 2 — empowered shards, 3 — shards hit every enemy, 4 — critical shards burst into shrapnel.
    /// All numbers live in <see cref="Parameters"/> modules, so upgrades are plain decorators.
    /// </summary>
    public class IceShards(
        string[] tags,
        int cooldown,
        int costValue,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        int projectiles,
        float shrapnelDamage,
        float shrapnelWeaponDamageScale,
        float shrapnelSpellDamageScale,
        float secondStageDamage,
        float secondStageWeaponDamageScale,
        float secondStageSpellDamageScale,
        Costs costType = Costs.Mana)
        : MulticastVolleyAbility(id: "Ability_Ice_Shards", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
    {
        private const int EmpoweredShardsStage = 2;
        private const int AllTargetsStage = 3;
        private const int ShrapnelBurstStage = 4;

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParameterDecorator
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.Shards] = new Module<Parameters>(() => projectiles, Parameters.Shards),
                    [Parameters.ShrapnelDamage] = new Module<Parameters>(() => shrapnelDamage, Parameters.ShrapnelDamage),
                    [Parameters.ShrapnelWeaponDamageScale] = new Module<Parameters>(() => shrapnelWeaponDamageScale, Parameters.ShrapnelWeaponDamageScale),
                    [Parameters.ShrapnelSpellDamageScale] = new Module<Parameters>(() => shrapnelSpellDamageScale, Parameters.ShrapnelSpellDamageScale),
                    [Parameters.SecondStageDamage] = new Module<Parameters>(() => secondStageDamage, Parameters.SecondStageDamage),
                    [Parameters.SecondStageWeaponDamageScale] = new Module<Parameters>(() => secondStageWeaponDamageScale, Parameters.SecondStageWeaponDamageScale),
                    [Parameters.SecondStageSpellDamageScale] = new Module<Parameters>(() => secondStageSpellDamageScale, Parameters.SecondStageSpellDamageScale),
                    // Zero by default; the L3 upgrade raises it with a decorator — the ability knows nothing about the upgrade
                    [Parameters.ShrapnelBarrierLeach] = new Module<Parameters>(() => 0f, Parameters.ShrapnelBarrierLeach),
                });
                return field;
            }
        }

        private float this[Parameters parameter] => AbilityParameterDecorator.GetModule(parameter).GetValue();

        public enum Parameters : byte
        {
            Shards,
            ShrapnelDamage,
            ShrapnelWeaponDamageScale,
            ShrapnelSpellDamageScale,
            ShrapnelBarrierLeach,
            SecondStageDamage,
            SecondStageWeaponDamageScale,
            SecondStageSpellDamageScale
        }

        public int Shards => (int)this[Parameters.Shards];
        public float ShrapnelBarrierLeach => this[Parameters.ShrapnelBarrierLeach];

        /// <summary>Set by the L3 upgrade: an effect stack applied by every landed shard.</summary>
        public Func<IEffect>? ShardEffectFactory { get; set; }

        public override void AddParameterDecorator<T>(IModuleDecorator<T, IParameterModule<T>> decorator)
        {
            if (decorator is not AbilityParameterDecorator<Parameters> parameterDecorator)
            {
                base.AddParameterDecorator(decorator);
                return;
            }

            AbilityParameterDecorator.AddDecorator(parameterDecorator);
        }

        public override void RemoveParameterDecorator<T>(string id, T key)
        {
            if (key is not Parameters parameter)
            {
                base.RemoveParameterDecorator(id, key);
                return;
            }

            AbilityParameterDecorator.RemoveDecorator(id, parameter);
        }

        public override IAbility Copy()
        {
            var copy = new IceShards(Tags, (int)Cooldown, CostValue,
                Damage, WeaponDamageScale, SpellDamageScale,
                Shards,
                shrapnelDamage, shrapnelWeaponDamageScale, shrapnelSpellDamageScale,
                secondStageDamage, secondStageWeaponDamageScale, secondStageSpellDamageScale, CostType) { ShardEffectFactory = ShardEffectFactory };
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override CastPlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            var plan = new CastPlan
            {
                ProjectilesCount = Shards,
                Damage = Damage,
                WeaponDamageScale = WeaponDamageScale,
                SpellDamageScale = SpellDamageScale,
                DamageType = DamageType.Cold,
                Targets = targets
            };

            if (ShardEffectFactory != null)
                plan.OnHitRiders.Add(hit => ApplyShardEffect(owner, hit.Target));

            return plan;
        }

        protected override void ApplyStage(int stage, CastPlan plan, IFightable owner, IBattleField field)
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
                    plan.OnHitRiders.Add(hit => DealShrapnelBurst(hit, owner, field));
                    break;
            }
        }

        private void ApplyShardEffect(IFightable owner, IFightable target)
        {
            ShardEffectFactory?.Invoke().Apply(new EffectApplyingContext { Caster = owner, Target = target, Source = InstanceId });
        }

        /// <summary>Stage 4: a critical shard bursts, damaging every enemy on the field.</summary>
        private void DealShrapnelBurst(ProjectileHit hit, IFightable owner, IBattleField field)
        {
            if (!hit.IsCritical) return;

            float damage = this[Parameters.ShrapnelDamage]
                           + owner.Parameters.Damage * this[Parameters.ShrapnelWeaponDamageScale]
                           + owner.Parameters.SpellDamage * this[Parameters.ShrapnelSpellDamageScale];

            float totalDealt = 0;
            foreach (IFightable enemy in field.GetEnemies(owner))
            {
                var context = new DamageContext { Source = owner, Cause = DamageCause.Ability };
                context.Add(DamageType.Cold, damage);
                _ = enemy.TakeDamage(context);
                totalDealt += context.TotalDamage;
            }

            if (ShrapnelBarrierLeach > 0)
                owner.CurrentBarrier += totalDealt * ShrapnelBarrierLeach;
        }
    }
}
