namespace Battle.Source.Abilities.IncreasingPressure
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Entity.Components.Decorator;
    using Core.Entity.Components.Module;
    using Core.Enums;

    public class IncreasingPressure(
        string[] tags,
        int costValue,
        int cooldown,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        int maxAttacks,
        float increaseAttackDamageStep,
        Costs costType = Costs.Mana)
        : DamagingAbility(id: "Ability_Increasing_Pressure", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
    {
        private float this[Parameters parameters] => AbilityParametersModuleManager.GetModule(parameters).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParametersModuleManager
        {
            get
            {
                if (field != null) return field;

                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(
                    new Dictionary<Parameters, IParameterModule<Parameters>>
                    {
                        [Parameters.Attacks] = new Module<Parameters>(() => maxAttacks, Parameters.Attacks),
                        [Parameters.AttackDamageStepMultiplier] = new Module<Parameters>(() => increaseAttackDamageStep, Parameters.AttackDamageStepMultiplier),
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

        public AttackModifierPipeline AttackModifiers { get; } = new();
        public float Attacks => this[Parameters.Attacks];
        public float AttackDamageMultiplier => this[Parameters.AttackDamageStepMultiplier];
        public IIpExecutionStrategy ExecutionStrategy = new IpDefaultExecutionStrategy();

        /// <summary>The ability's bonus damage added on top of the owner's basic attack: flat + weapon- and spell-scaled.</summary>
        public float BonusDamage(IFightable owner) =>
            Damage + owner.Parameters.Damage * WeaponDamageScale + owner.Parameters.SpellDamage * SpellDamageScale;

        /// <summary>Full damage of a single hit at the given escalation multiplier — the owner's basic weapon attack plus
        /// <see cref="BonusDamage"/>. Single source of truth for every execution strategy.</summary>
        public float PerHitDamage(IFightable owner, float increase) =>
            (owner.Parameters.Damage + BonusDamage(owner)) * increase;

        public enum Parameters : byte
        {
            Attacks,
            AttackDamageStepMultiplier
        }

        public override void AddParameterDecorator<T>(IModuleDecorator<T, IParameterModule<T>> decorator)
        {
            if (decorator is not AbilityParameterDecorator<Parameters> abilityParameterDecorator)
            {
                base.AddParameterDecorator(decorator);
                return;
            }

            AbilityParametersModuleManager.AddDecorator(abilityParameterDecorator);
        }

        public override void RemoveParameterDecorator<T>(string id, T key)
        {
            if (key is not Parameters pKey)
            {
                base.RemoveParameterDecorator(id, key);
                return;
            }

            AbilityParametersModuleManager.RemoveDecorator(id, pKey);
        }

        public override IAbility Copy()
        {
            var copy = new IncreasingPressure(Tags, CostValue, (int)Cooldown, Damage, WeaponDamageScale, SpellDamageScale,
                maxAttacks, AttackDamageMultiplier, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        public void AddAttackModifier(IAttackModifier modifier) => AttackModifiers.Add(modifier);
        public void RemoveAttackModifier(string id) => AttackModifiers.Remove(id);

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) =>
            await ExecutionStrategy.Execute(this, owner, targets, field);
    }
}
