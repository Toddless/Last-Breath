namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using System;
    using Module;
    using Decorators;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Core.Interfaces.Components;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Components.Decorator;

    public class SeriesOfAttacks(
        string id,
        string[] tags,
        int cooldown,
        int costValue,
        int minAttacks,
        int maxAttacks,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        List<IEffect> effects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        float damageMultiplier = 1,
        Costs costType = Costs.Mana,
        AbilityType abilityType = AbilityType.Target)
        : Ability(id, tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, effects, upgrades, costType, abilityType)
    {
        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParameterDecorator
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

        private float this[Parameters parameter] => AbilityParameterDecorator.GetModule(parameter).GetValue();
        public int MinAttacks => (int)this[Parameters.MinAttacks];
        public int MaxAttacks => (int)this[Parameters.MaxAttacks];
        public float DamageMultiplier => this[Parameters.DamageMultiplier];
        public ISoAExecutionStrategy ExecutionStrategy { get; set; } = new SoAsDefaultExecutionStrategy();

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

        protected override async Task ExecuteInternal(List<IEntity> targets, IEntity owner) => await ExecutionStrategy.Execute(this, owner, targets);
    }
}
