namespace Battle.Source.Abilities
{
    using Source;
    using Module;
    using Services;
    using Core.Enums;
    using Decorators;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Battle;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components;
    using System.Collections.Generic;
    using Core.Interfaces.Components.Module;

    public class Fireball(
        float cooldown,
        float maxTargets,
        float baseDamage,
        float baseCriticalChance,
        string[] tags,
        int availablePoints,
        int costValue,
        List<IEffect> effects,
        List<IEffect> casterEffects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        IStanceMastery? mastery = null,
        Costs costType = Costs.Mana)
        : Ability(id: "Ability_Fireball", tags, availablePoints, effects, casterEffects, upgrades, mastery)
    {
        public float CriticalChanceDetermination => this[AbilityParameter.CriticalChanceDetermination];

        public float Damage => this[AbilityParameter.Damage];

        public float CriticalChanceValue => this[AbilityParameter.CriticalChanceValue];

        public override async Task Activate(List<IEntity> targets)
        {
            if (Owner == null) return;

            var context = new EffectApplyingContext { Caster = Owner, Source = Id };

            foreach (IEntity target in targets)
            {
                context.Target = target;
                float damage = ApplyConditionalModifiers(context, AbilityParameter.Damage, Damage + Owner.Parameters.SpellDamage);
                float criticalChance = ApplyConditionalModifiers(context, AbilityParameter.CriticalChanceValue, CriticalChanceValue);
                bool isCritical = ApplyConditionalModifiers(context, AbilityParameter.CriticalChanceDetermination, CriticalChanceDetermination) <= criticalChance;
                context.IsCritical = isCritical;
                context.Damage = damage;

                ApplyTargetEffects(context);

                target.TakeDamage(Owner, damage, DamageType.Normal, DamageSource.Ability, isCritical);
            }

            ApplyCasterEffects(context);
            StartCooldown();
            ConsumeResource();
            await Owner.Animations.PlayAnimationAsync(Id);
        }

        protected override IModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator> CreateModuleManager() =>
            new ModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator>(
                new Dictionary<AbilityParameter, IParameterModule<AbilityParameter>>
                {
                    [AbilityParameter.CostValue] = new Module<AbilityParameter>(() => costValue, AbilityParameter.CostValue),
                    [AbilityParameter.CostType] = new Module<AbilityParameter>(() => (float)costType, AbilityParameter.CostType),
                    [AbilityParameter.Cooldown] = new Module<AbilityParameter>(() => cooldown, AbilityParameter.Cooldown),
                    [AbilityParameter.Target] = new Module<AbilityParameter>(() => maxTargets, AbilityParameter.Target),
                    [AbilityParameter.Damage] = new Module<AbilityParameter>(() => baseDamage, AbilityParameter.Damage),
                    [AbilityParameter.CriticalChanceValue] = new Module<AbilityParameter>(GetCurrentCriticalChance, AbilityParameter.CriticalChanceValue),
                    [AbilityParameter.CriticalChanceDetermination] =
                        new Module<AbilityParameter>(() => StaticRandomNumberGenerator.Rnd.Randf(), AbilityParameter.CriticalChanceDetermination),
                });

        private float GetCurrentCriticalChance() => Owner == null
            ? baseCriticalChance
            : Owner.Parameters.CalculateForBase(EntityParameter.CriticalChance, baseCriticalChance);

        // Not sure about this. Modifiers will be apply twice. Once for entity spell damage parameter and once for ability spell damage
        private float GetCurrentDamage() => Owner == null
            ? baseDamage
            : Owner.Parameters.CalculateForBase(EntityParameter.SpellDamage, baseDamage);
    }
}
