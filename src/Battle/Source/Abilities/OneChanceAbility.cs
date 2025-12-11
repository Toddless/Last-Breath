namespace Battle.Source.Abilities
{
    using System.Collections.Generic;
    using Source;
    using Module;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Entity;
    using Decorators;

    public class OneChanceAbility(
        string id,
        string[] tags,
        int availablePoints,
        int cooldown,
        int maxTargets,
        int costValue,
        List<IEffect> effects,
        List<IEffect> casterEffects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        IStanceMastery mastery,
        Costs costType = Costs.Mana) : Ability(id, tags, availablePoints, effects, casterEffects, upgrades, mastery)
    {
        public override void Activate(List<IEntity> targets)
        {
        }

        protected override IModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator> CreateModuleManager() =>
            new ModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator>(new Dictionary<AbilityParameter, IParameterModule<AbilityParameter>>
            {
                [AbilityParameter.CostValue] = new Module<AbilityParameter>(() => costValue, AbilityParameter.CostValue),
                [AbilityParameter.CostType] = new Module<AbilityParameter>(() => (float)costType, AbilityParameter.CostType),
                [AbilityParameter.Cooldown] = new Module<AbilityParameter>(() => cooldown, AbilityParameter.Cooldown),
                [AbilityParameter.Target] = new Module<AbilityParameter>(() => maxTargets, AbilityParameter.Target),
            });
    }
}
