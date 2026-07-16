namespace Battle.Source.Abilities.Overload
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Entity.Components.Decorator;
    using Core.Entity.Components.Module;
    using Core.Enums;

    /// <summary>Cast plan of the Overload: the volley fields plus the mana-conversion knobs.</summary>
    public class OverloadPlan : DamagingCastPlan
    {
        public float ManaBurnPercent { get; set; }
        public float DamagePerMana { get; set; }
        public float CritManaRefund { get; set; }
        public List<Action<TargetHit>> OnHitRiders { get; } = [];
    }

    /// <summary>
    /// Burns a share of the target's CURRENT mana and converts every burned point into damage.
    /// Stage 2 doubles the conversion, stage 3 refunds part of the burned mana on a crit,
    /// stage 4 turns the damage pure.
    /// </summary>
    public class Overload(
        string[] tags,
        int cooldown,
        int costValue,
        float manaBurnPercent,
        float damagePerMana,
        float stageTwoDamagePerMana,
        float stageThreeCritRefund,
        Costs costType = Costs.Mana)
        : MulticastAbility<OverloadPlan>(id: "Ability_Overload", tags, cooldown, costValue, damage: 0, weaponDamageScale: 0, spellDamageScale: 0, costType)
    {

        private float this[Parameters parameter] => AbilityParametersModuleManager.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParametersModuleManager
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.ManaBurnPercent] = new Module<Parameters>(() => manaBurnPercent, Parameters.ManaBurnPercent),
                    [Parameters.DamagePerMana] = new Module<Parameters>(() => damagePerMana, Parameters.DamagePerMana)
                });
                return field;
            }
        }

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                AddModuleValues(values, AbilityParametersModuleManager);
                return values;
            }
        }

        public float ManaBurnPercent => this[Parameters.ManaBurnPercent];
        public float DamagePerMana => this[Parameters.DamagePerMana];

        /// <summary>L3 upgrade point: a kill by this cast resets the cooldown.</summary>
        public bool ResetCooldownOnKill { get; set; }

        public enum Parameters : byte
        {
            ManaBurnPercent,
            DamagePerMana
        }

        public override void AddParameterDecorator<T>(IModuleDecorator<T, IParameterModule<T>> decorator)
        {
            if (decorator is not AbilityParameterDecorator<Parameters> parameterDecorator)
            {
                base.AddParameterDecorator(decorator);
                return;
            }

            AbilityParametersModuleManager.AddDecorator(parameterDecorator);
        }

        public override void RemoveParameterDecorator<T>(string id, T key)
        {
            if (key is not Parameters parameter)
            {
                base.RemoveParameterDecorator(id, key);
                return;
            }

            AbilityParametersModuleManager.RemoveDecorator(id, parameter);
        }


        public override IAbility Copy()
        {
            var copy = new Overload(Tags, (int)Cooldown, CostValue, ManaBurnPercent, DamagePerMana, stageTwoDamagePerMana, stageThreeCritRefund, CostType)
            {
                ResetCooldownOnKill = ResetCooldownOnKill
            };
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

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
                    plan.DamagePerMana = stageTwoDamagePerMana;
                    break;
                case 3:
                    plan.CritManaRefund = stageThreeCritRefund;
                    break;
                case 4:
                    plan.DamageType = DamageType.Pure;
                    break;
            }
        }


        protected override async Task ExecutePlan(OverloadPlan plan, IFightable owner)
        {
            float baseDamage = plan.Damage;
            foreach (IFightable target in plan.Targets.Where(t => t.IsAlive).ToList())
            {
                float burned = target.CurrentMana * ManaBurnPercent;
                target.CurrentMana -= burned;

                // Per-target flat damage: the base plus the converted mana. Restored after the hit.
                plan.Damage = baseDamage + (burned * DamagePerMana);
                var hit = await DealTargetDamage(plan, owner, target);
                plan.Damage = baseDamage;

                if (hit.IsCritical && plan.CritManaRefund > 0)
                    owner.RestoreMana(new ManaRecoveryContext(owner, owner) { Amount = burned * plan.CritManaRefund });
                if (ResetCooldownOnKill && !target.IsAlive) CooldownLeft = 0;
                foreach (var rider in plan.OnHitRiders)
                    rider(hit);
            }
        }

        private async Task<TargetHit> DealTargetDamage(OverloadPlan plan, IFightable owner, IFightable target)
        {
            float damage = CalculateHitDamage(plan, owner);
            bool isCritical = RollCritical(owner);
            if (isCritical)
            {
                damage *= owner.Parameters.CriticalDamage + CriticalDamageBonus;
            }

            var context = new DamageContext { Source = owner, Cause = DamageCause.Ability, IsCrit = isCritical };
            context.Add(plan.DamageType, damage);
            await target.TakeDamage(context);
            return new TargetHit(target, isCritical, damage);
        }
    }
}
