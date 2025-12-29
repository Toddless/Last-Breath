namespace Battle.Source.Abilities
{
    using Module;
    using Decorators;
    using Core.Enums;
    using Core.Modifiers;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components;
    using System.Collections.Generic;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Events.GameEvents;

    public class Sacrifice(
        string[] tags,
        int availablePoints,
        float costValue,
        int cooldown,
        float percentHealthToSacrifice,
        List<IEffect> effects,
        List<IEffect> casterEffects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        IStanceMastery? mastery = null,
        Costs costType = Costs.Mana,
        AbilityType type = AbilityType.SelfCast)
        : Ability(id: "Ability_Sacrifice", tags, availablePoints, effects, casterEffects, upgrades, mastery, type)
    {
        public float PercentHealthToSacrifice { get; } = percentHealthToSacrifice;

        public override async Task Activate(List<IEntity> targets)
        {
            if (Owner == null) return;

            float sacrificedLife = Owner.CurrentHealth * PercentHealthToSacrifice;
            var modifier = new ModifierInstance(EntityParameter.Damage, ModifierType.Flat, sacrificedLife, Id);
            Owner.CurrentHealth -= (int)sacrificedLife;
            Owner.Modifiers.AddPermanentModifier(modifier);
            Owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
            await base.Activate(targets);
        }

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            Owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
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
