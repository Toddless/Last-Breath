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
    using Services;

    public class Fireball : Ability
    {
        private readonly int _costValue;
        private readonly float _cooldown;
        private readonly Costs _costType;
        private readonly float _maxTargets;
        private readonly float _baseDamage;
        private readonly float _baseCriticalChance;

        public float CriticalChanceDetermination => this[AbilityParameter.CriticalChanceDetermination];

        public float Damage => this[AbilityParameter.Damage];

        public float CriticalChanceValue => this[AbilityParameter.CriticalChanceValue];

        public Fireball(float cooldown,
            float maxTargets,
            float baseDamage,
            float baseCriticalChance,
            string[] tags,
            int availablePoints,
            int costValue,
            List<IEffect> effects,
            List<IEffect> casterEffects,
            Dictionary<int, List<IAbilityUpgrade>> upgrades,
            IStanceMastery mastery,
            Costs costType = Costs.Mana) : base(id: "Ability_Fireball", tags, availablePoints, effects, casterEffects, upgrades, mastery)
        {
            _cooldown = cooldown;
            _maxTargets = maxTargets;
            _baseDamage = baseDamage;
            _baseCriticalChance = baseCriticalChance;
            _costValue = costValue;
            _costType = costType;
            //Rnd.Randomize();
        }

        public void SetOwner(IEntity? owner) => Owner = owner;

        public override void Activate(List<IEntity> targets)
        {
            if (Owner == null) return;

            var context = new EffectApplyingContext { Caster = Owner, Source = this };

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
        }

        protected override IModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator> CreateModuleManager() =>
            new ModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator>(
                new Dictionary<AbilityParameter, IParameterModule<AbilityParameter>>
                {
                    [AbilityParameter.CostValue] = new Module<AbilityParameter>(() => _costValue, AbilityParameter.CostValue),
                    [AbilityParameter.CostType] = new Module<AbilityParameter>(() => (float)_costType, AbilityParameter.CostType),
                    [AbilityParameter.Cooldown] = new Module<AbilityParameter>(() => _cooldown, AbilityParameter.Cooldown),
                    [AbilityParameter.Target] = new Module<AbilityParameter>(() => _maxTargets, AbilityParameter.Target),
                    [AbilityParameter.Damage] = new Module<AbilityParameter>(() => _baseDamage, AbilityParameter.Damage),
                    [AbilityParameter.CriticalChanceValue] = new Module<AbilityParameter>(GetCurrentCriticalChance, AbilityParameter.CriticalChanceValue),
                    [AbilityParameter.CriticalChanceDetermination] =
                        new Module<AbilityParameter>(() => StaticRandomNumberGenerator.Rnd.Randf(), AbilityParameter.CriticalChanceDetermination),
                });

        private float GetCurrentCriticalChance() => Owner == null
            ? _baseCriticalChance
            : Owner.Parameters.CalculateForBase(EntityParameter.CriticalChance, _baseCriticalChance);

        // Not sure about this. Modifiers will be apply twice. Once for entity spell damage parameter and once for ability spell damage
        private float GetCurrentDamage() => Owner == null
            ? _baseDamage
            : Owner.Parameters.CalculateForBase(EntityParameter.SpellDamage, _baseDamage);
    }
}
