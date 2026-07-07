namespace Battle.Source.Abilities.Overload
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Components;
    using Core.Components.Decorator;
    using Core.Components.Module;
    using Core.Entity;
    using Core.Enums;
    using Decorators;

    /// <summary>Cast plan of the Overload: the volley fields plus the mana-conversion knobs.</summary>
    public class OverloadPlan : CastPlan
    {
        public float ManaBurnPercent { get; set; }
        public float DamagePerMana { get; set; }
        public float CritManaRefund { get; set; }
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
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        float manaBurnPercent,
        float damagePerMana,
        float stageTwoDamagePerMana,
        float stageThreeCritRefund,
        Costs costType = Costs.Mana)
        : MulticastVolleyAbility(id: "Ability_Overload", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
    {
        private float this[Parameters parameter] => AbilityParameterDecorator.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParameterDecorator
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
            var copy = new Overload(Tags, (int)Cooldown, CostValue, Damage, WeaponDamageScale, SpellDamageScale,
                ManaBurnPercent, DamagePerMana, stageTwoDamagePerMana, stageThreeCritRefund, CostType) { ResetCooldownOnKill = ResetCooldownOnKill };
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override CastPlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field) =>
            new OverloadPlan
            {
                ProjectilesCount = 1,
                Damage = Damage,
                WeaponDamageScale = WeaponDamageScale,
                SpellDamageScale = SpellDamageScale,
                DamageType = DamageType.Lightning,
                Targets = targets,
                ManaBurnPercent = ManaBurnPercent,
                DamagePerMana = DamagePerMana
            };

        protected override void ApplyStage(int stage, CastPlan plan, IFightable owner, IBattleField field)
        {
            if (plan is not OverloadPlan overload) return;
            switch (stage)
            {
                case 2:
                    overload.DamagePerMana = stageTwoDamagePerMana;
                    break;
                case 3:
                    overload.CritManaRefund = stageThreeCritRefund;
                    break;
                case 4:
                    overload.DamageType = DamageType.Pure;
                    break;
            }
        }

        protected override async Task ExecutePlan(CastPlan plan, IFightable owner)
        {
            var overload = (OverloadPlan)plan;
            float baseDamage = plan.Damage;
            foreach (IFightable target in plan.Targets.Where(t => t.IsAlive).ToList())
            {
                float burned = target.CurrentMana * overload.ManaBurnPercent;
                target.CurrentMana -= burned;

                // Per-target flat damage: the base plus the converted mana. Restored after the hit.
                plan.Damage = baseDamage + (burned * overload.DamagePerMana);
                var hit = await DealProjectileDamage(plan, owner, target);
                plan.Damage = baseDamage;

                if (hit.IsCritical && overload.CritManaRefund > 0) owner.CurrentMana += burned * overload.CritManaRefund;
                if (ResetCooldownOnKill && !target.IsAlive) CooldownLeft = 0;
                foreach (var rider in plan.OnHitRiders)
                    rider(hit);
            }
        }
    }
}
