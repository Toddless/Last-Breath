namespace Battle.Source.Abilities.Overload
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

    /// <summary>Cast plan of the Overload: the volley fields plus the mana-conversion knobs.</summary>
    public class OverloadPlan : DamagingCastPlan
    {
        public float ManaBurnPercent { get; set; }
        public float DamagePerMana { get; set; }
        public float CritManaRefund { get; set; }
        public List<Action<ProjectileHit>> OnHitRiders { get; } = [];
    }

    /// <summary>
    /// Burns a share of the target's CURRENT mana and converts every burned point into damage.
    /// Stage 2 doubles the conversion, stage 3 refunds part of the burned mana on a crit,
    /// stage 4 turns the damage pure.
    /// </summary>
    public class Overload(AbilityBaseData data) : MulticastAbility<OverloadPlan>(data)
    {
        public float Damage => this[AbilityParameter.Damage];
        public float WeaponDamageScale => this[AbilityParameter.WeaponDamageScale];
        public float SpellDamageScale => this[AbilityParameter.SpellDamageScale];

        public float ManaBurnPercent => this[Parameters.ManaBurnPercent];
        public float DamagePerMana => this[Parameters.DamagePerMana];

        /// <summary>L3 upgrade point: a kill by this cast resets the cooldown.</summary>
        public bool ResetCooldownOnKill { get; set; }

        public static class Parameters
        {
            public const string ManaBurnPercent = nameof(ManaBurnPercent);
            public const string DamagePerMana = nameof(DamagePerMana);
            public const string StageTwoDamagePerMana = nameof(StageTwoDamagePerMana);
            public const string StageThreeCritRefund = nameof(StageThreeCritRefund);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            RegisterDamageParameters(parameters);
            parameters.RegisterDefault(Parameters.ManaBurnPercent, 0.25f);
            parameters.RegisterDefault(Parameters.DamagePerMana, 1.5f);
            parameters.RegisterDefault(Parameters.StageTwoDamagePerMana, 2f);
            parameters.RegisterDefault(Parameters.StageThreeCritRefund, 0.15f);
        }

        public override IAbility Copy() =>
            CopyUpgradesTo(new Overload(Data) { ResetCooldownOnKill = ResetCooldownOnKill });

        protected override OverloadPlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field) =>
            new OverloadPlan
            {
                Damage = Damage,
                WeaponDamageScale = WeaponDamageScale,
                SpellDamageScale = SpellDamageScale,
                DamageType = DamageType.Lightning,
                Targets = targets,
                ManaBurnPercent = ManaBurnPercent,
                DamagePerMana = DamagePerMana
            };

        protected override void ApplyStage(int stage, OverloadPlan plan, IFightable owner, IBattleField field)
        {
            switch (stage)
            {
                case 2:
                    plan.DamagePerMana = this[Parameters.StageTwoDamagePerMana];
                    break;
                case 3:
                    plan.CritManaRefund = this[Parameters.StageThreeCritRefund];
                    break;
                case 4:
                    plan.DamageType = DamageType.Pure;
                    break;
            }
        }


        protected override async Task ExecutePlan(OverloadPlan plan, IFightable owner, IBattleField field)
        {
            float baseDamage = plan.Damage;
            foreach (IFightable target in plan.Targets.Where(t => t.IsAlive).ToList())
            {
                float burned = target.CurrentMana * plan.ManaBurnPercent;
                target.CurrentMana -= burned;

                // Per-target flat damage: the base plus the converted mana. Restored after the hit.
                plan.Damage = baseDamage + (burned * plan.DamagePerMana);
                var hit = await DealPlanDamage(plan, owner, target);
                plan.Damage = baseDamage;

                if (hit.IsCritical && plan.CritManaRefund > 0)
                    owner.RestoreMana(new ManaRecoveryContext(owner, owner) { Amount = burned * plan.CritManaRefund });
                if (ResetCooldownOnKill && !target.IsAlive) CooldownLeft = 0;
                foreach (var rider in plan.OnHitRiders)
                    rider(hit);
                await ApplyImpactRiders(new AbilityImpact(owner, target, field, Succeeded: true, hit.IsCritical, hit.Damage));
            }
        }
    }
}
