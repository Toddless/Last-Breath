namespace Battle.Source.Abilities.SeriesOfAttacks
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

    public class SeriesOfAttacks(
        string[] tags,
        int cooldown,
        int costValue,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        int minAttacks,
        int maxAttacks,
        float damageMultiplier = 1,
        Costs costType = Costs.Mana)
        : DamagingAbility(id: "Ability_Series_Of_Attacks", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
    {
        private float this[Parameters parameter] => AbilityParametersModuleManager.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParametersModuleManager
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.DamageMultiplier] = new Module<Parameters>(() => damageMultiplier, Parameters.DamageMultiplier),
                    [Parameters.MinAttacks] = new Module<Parameters>(() => minAttacks, Parameters.MinAttacks),
                    [Parameters.MaxAttacks] = new Module<Parameters>(() => maxAttacks, Parameters.MaxAttacks)
                });
                field.ModuleChanges += OnModuleChanges;
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

        public int MinAttacks => (int)this[Parameters.MinAttacks];
        public int MaxAttacks => (int)this[Parameters.MaxAttacks];
        public float DamageMultiplier => this[Parameters.DamageMultiplier];
        public ISoAExecutionStrategy ExecutionStrategy { get; set; } = new SoAsDefaultExecutionStrategy();
        public AttackModifierPipeline AttackModifiers { get; } = new();

        public enum Parameters : byte
        {
            DamageMultiplier,
            MinAttacks,
            MaxAttacks
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

        public void AddAttackModifier(IAttackModifier modifier) => AttackModifiers.Add(modifier);
        public void RemoveAttackModifier(string id) => AttackModifiers.Remove(id);

        public override IAbility Copy()
        {
            var copy = new SeriesOfAttacks(Tags, (int)Cooldown, CostValue, Damage, WeaponDamageScale, SpellDamageScale, MinAttacks, MaxAttacks, DamageMultiplier, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) =>
            await ExecutionStrategy.Execute(this, owner, targets, field);
    }
}
