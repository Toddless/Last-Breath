namespace Battle.Source.Abilities
{
    using Module;
    using Core.Enums;
    using Decorators;
    using Core.Modifiers;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Core.Interfaces.Components;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Events.GameEvents;

    public class ManaDevour(
        string[] tags,
        int availablePoints,
        int costValue,
        float cooldown,
        float percentManaToConsume,
        float increaseBonusPerManaConsumed,
        List<IEffect> effects,
        List<IEffect> casterEffects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        IStanceMastery? mastery = null,
        Costs costType = Costs.Mana,
        AbilityType type = AbilityType.SelfCast) : Ability(id: "Ability_Mana_Devour", tags, availablePoints, effects, casterEffects, upgrades, mastery, type)
    {
        public float PercentToConsume { get; } = percentManaToConsume;
        public float IncreaseBonusPerManaConsumed { get; } = increaseBonusPerManaConsumed;

        public override async Task Activate(List<IEntity> targets)
        {
            if (Owner == null) return;

            float manaConsumed = Owner.CurrentMana * PercentToConsume;
            float increase = manaConsumed / 100 / IncreaseBonusPerManaConsumed;
            var modifier = new ModifierInstance(EntityParameter.SpellDamage, ModifierType.Increase, increase, Id);
            Owner.Modifiers.AddPermanentModifier(modifier);
            Owner.CombatEvents.Subscribe<AbilityActivatedGameEvent>(OnAbilityActivated);
            await base.Activate(targets);
        }

        private void OnAbilityActivated(AbilityActivatedGameEvent obj)
        {
            Owner?.CombatEvents.Unsubscribe<AbilityActivatedGameEvent>(OnAbilityActivated);
            Owner?.Modifiers.RemovePermanentModifierBySource(Id);
        }

        protected override IModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator> CreateModuleManager() =>
            new ModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator>(new Dictionary<AbilityParameter, IParameterModule<AbilityParameter>>
            {
                [AbilityParameter.Cooldown] = new Module<AbilityParameter>(() => cooldown, AbilityParameter.Cooldown),
                [AbilityParameter.CostValue] = new Module<AbilityParameter>(() => costValue, AbilityParameter.CostValue),
                [AbilityParameter.CostType] = new Module<AbilityParameter>(() => (float)costType, AbilityParameter.CostType)
            });
    }
}
