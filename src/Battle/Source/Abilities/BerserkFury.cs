namespace Battle.Source.Abilities
{
    using Godot;
    using System;
    using Source;
    using Module;
    using Services;
    using Utilities;
    using Core.Enums;
    using Decorators;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components;
    using System.Collections.Generic;
    using Core.Interfaces.Components.Module;

    public class BerserkFury(
        float cooldown,
        float maxTargets,
        string[] tags,
        int availablePoints,
        int costValue,
        List<IEffect> effects,
        List<IEffect> casterEffects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        IStanceMastery mastery,
        Costs costType = Costs.Mana) : Ability(id: "Ability_Berserk_Fury", tags, availablePoints, effects, casterEffects, upgrades, mastery)
    {
        public override void Activate(List<IEntity> targets)
        {
            try
            {
                base.Activate(targets);

                PerformMultipleAttacks(targets);
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to activate", ex, this);
            }
        }

        private void PerformMultipleAttacks(List<IEntity> targets)
        {
            if (Owner == null) return;
            while (true)
            {
                if (Owner.CurrentHealth <= 1) return;

                foreach (IEntity target in targets)
                {
                }

                float chance = Mathf.Clamp(1f - (Owner.CurrentHealth / Owner.Parameters.MaxHealth), 0.05f, 0.7f);

                if (StaticRandomNumberGenerator.Rnd.Randf() > chance)
                    break;
            }
        }

        protected override IModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator> CreateModuleManager() =>
            new ModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator>(new Dictionary<AbilityParameter, IParameterModule<AbilityParameter>>
            {
                [AbilityParameter.Target] = new Module<AbilityParameter>(() => maxTargets, AbilityParameter.Target),
                [AbilityParameter.Cooldown] = new Module<AbilityParameter>(() => cooldown, AbilityParameter.Cooldown),
                [AbilityParameter.CostValue] = new Module<AbilityParameter>(() => costValue, AbilityParameter.CostValue),
                [AbilityParameter.CostType] = new Module<AbilityParameter>(() => (float)costType, AbilityParameter.CostType)
            });
    }
}
