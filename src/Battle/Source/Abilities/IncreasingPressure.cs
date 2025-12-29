namespace Battle.Source.Abilities
{
    using Module;
    using Services;
    using Core.Enums;
    using Decorators;
    using Core.Interfaces.Battle;
    using System.Threading.Tasks;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components;
    using System.Collections.Generic;
    using Core.Interfaces.Components.Module;

    public class IncreasingPressure(
        string[] tags,
        int availablePoints,
        int costValue,
        int cooldown,
        List<IEffect> effects,
        List<IEffect> casterEffects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        IStanceMastery? mastery = null,
        Costs costType = Costs.Mana)
        : Ability(id: "Ability_Increasing_Pressure", tags, availablePoints, effects, casterEffects, upgrades, mastery)
    {
        public override async Task Activate(List<IEntity> targets)
        {
            await base.Activate(targets);
            await PerformAttacks(targets);
        }

        private async Task PerformAttacks(List<IEntity> targets)
        {
            if (Owner == null) return;
            foreach (IEntity target in targets)
            {
                float increase = 1f;
                // TODO: Can this ability have more then 5 attacks??
                for (int i = 0; i < 5; i++)
                {
                    if (!target.IsAlive) return;
                    float damage = Owner.GetDamage();
                    damage += Owner.Parameters.SpellDamage;
                    damage *= increase;
                    float chance = StaticRandomNumberGenerator.Rnd.Randf();
                    bool isCrit = chance <= Owner.Parameters.CriticalChance;
                    if (isCrit)
                        damage *= Owner.Parameters.CriticalDamage;
                    await target.TakeDamage(Owner, damage, DamageType.Normal, DamageSource.Ability, isCrit);
                    // TODO: this increase can have different values??
                    increase += 0.2f;
                }
            }
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
