namespace Battle.Source.Abilities
{
    using Module;
    using Services;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Battle;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Utilities;

    public class Fireball : Ability
    {
        private readonly float _baseDamage;
        private readonly float _baseCriticalChance;

        public Fireball(string[] tags,
            int cooldown,
            float baseDamage,
            float baseCriticalChance,
            int costValue,
            List<IEffect> effects,
            List<IEffect> casterEffects,
            Dictionary<int, List<IAbilityUpgrade>> upgrades,
            IStanceMastery? mastery = null,
            int maxTargets = 1,
            Costs costType = Costs.Mana) : base(id: "Ability_Fireball", tags, cooldown, costValue, maxTargets, effects, casterEffects, upgrades, mastery, costType)
        {
            _baseDamage = baseDamage;
            _baseCriticalChance = baseCriticalChance;
            ModuleManager.AddBaseModule(AbilityParameter.Damage, new Module<AbilityParameter>(() => baseDamage, AbilityParameter.Damage));
            ModuleManager.AddBaseModule(AbilityParameter.CriticalChanceDetermination,
                new Module<AbilityParameter>(() => StaticRandomNumberGenerator.Rnd.Randf(), AbilityParameter.CriticalChanceDetermination));
            ModuleManager.AddBaseModule(AbilityParameter.CriticalChanceValue, new Module<AbilityParameter>(GetCurrentCriticalChance, AbilityParameter.CriticalChanceValue));
        }

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

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, Damage);

        private float GetCurrentCriticalChance() => Owner == null
            ? _baseCriticalChance
            : Owner.Parameters.CalculateForBase(EntityParameter.CriticalChance, _baseCriticalChance);

        // Not sure about this. Modifiers will be apply twice. Once for entity spell damage parameter and once for ability spell damage
        private float GetCurrentDamage() => Owner == null
            ? _baseDamage
            : Owner.Parameters.CalculateForBase(EntityParameter.SpellDamage, _baseDamage);
    }
}
