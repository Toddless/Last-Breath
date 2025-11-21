namespace Battle.TestData.Abilities
{
    using Godot;
    using Source;
    using Core.Enums;
    using Decorators;
    using Source.Module;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Core.Interfaces.Components;
    using Core.Interfaces.Battle.Module;

    public class Fireball(
        float cooldown,
        float targets,
        float baseDamage,
        float baseCriticalChance,
        string id,
        string[] tags,
        int availablePoints,
        Texture2D? icon,
        List<IEffect> effects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        IAbilityCost cost,
        IStanceMastery mastery) : Ability(id, tags, availablePoints, icon, effects, upgrades, cost, mastery)
    {
        private IEntity? _owner;

        // с ростом уровня мастерства мне необходимо апгрейдить базовые показатели способности (урон, шансы и т.п)
        public float Damage => this[AbilityParameter.Damage];

        // each ability has it own base critical chance, we take all increases and multipliers from entity
        // and calculate it
        public float CriticalChance => this[AbilityParameter.CriticalChance];

        public void SetOwner(IEntity? owner) => _owner = owner;

        public override void Activate(AbilityContext context)
        {
        }

        protected override IModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator> CreateModuleManager() =>
            new ModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator>(
                new Dictionary<AbilityParameter, IParameterModule<AbilityParameter>>
                {
                    [AbilityParameter.CostValue] = new Module<AbilityParameter>(() => Cost.Value, AbilityParameter.CostValue),
                    [AbilityParameter.CostType] = new Module<AbilityParameter>(() => (float)Cost.Resource, AbilityParameter.CostType),
                    [AbilityParameter.Cooldown] = new Module<AbilityParameter>(() => cooldown, AbilityParameter.Cooldown),
                    [AbilityParameter.Target] = new Module<AbilityParameter>(() => targets, AbilityParameter.Target),
                    [AbilityParameter.Damage] = new Module<AbilityParameter>(() => Mastery.GetValueForParameter(baseDamage), AbilityParameter.Damage),
                    [AbilityParameter.CriticalChance] = new Module<AbilityParameter>(GetCurrentCriticalChance, AbilityParameter.CriticalChance),
                });


        private float GetCurrentCriticalChance() =>
            _owner == null ? baseCriticalChance : _owner.Parameters.CalculateForBase(EntityParameter.CriticalChance, Mastery.GetValueForParameter(baseCriticalChance));
    }
}
