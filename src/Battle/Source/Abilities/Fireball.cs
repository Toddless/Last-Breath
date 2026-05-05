namespace Battle.Source.Abilities
{
    using Module;
    using Utilities;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Battle;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Godot;

    public class Fireball : Ability
    {
        private readonly float _baseDamage;
        private readonly float _baseCriticalChance;
        private readonly RandomNumberGenerator _rnd;

        public Fireball(string[] tags,
            int cooldown,
            float baseDamage,
            float baseCriticalChance,
            int costValue,
            List<IEffect> effects,
            List<IEffect> casterEffects,
            Dictionary<int, List<IAbilityUpgradeWrap<>>> upgrades,
            Costs costType = Costs.Mana) : base(id: "Ability_Fireball", tags, cooldown, costValue, effects, casterEffects, upgrades, costType)
        {
            _baseDamage = baseDamage;
            _baseCriticalChance = baseCriticalChance;
            _rnd = new RandomNumberGenerator();
            _rnd.Randomize();
        }

        public float Damage => this[AbilityParameter.Damage];

        public override async Task Execute(List<IEntity> targets)
        {
            if (Owner == null) return;

            var context = new EffectApplyingContext { Caster = Owner, Source = Id };

            foreach (IEntity target in targets)
            {
                context.Target = target;
                float damage = ApplyConditionalModifiers(context, AbilityParameter.Damage, Damage + Owner.Parameters.SpellDamage);
                context.Damage = damage;

                ApplyTargetEffects(context);

                target.TakeDamage(Owner, damage, DamageType.Normal, DamageSource.Ability, false);
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
