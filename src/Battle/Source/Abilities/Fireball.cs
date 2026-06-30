namespace Battle.Source.Abilities
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Godot;
    using Utilities;

    public class Fireball : AttackAbility
    {
        private readonly float _damage;
        private readonly float _baseCriticalChance;
        private readonly RandomNumberGenerator _rnd;

        public Fireball(string[] tags,
            int cooldown,
            float damage,
            float weaponDamageScale,
            float spellDamageScale,
            float baseCriticalChance,
            int costValue,
            Costs costType = Costs.Mana) : base(id: "Ability_Fireball", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
        {
            _damage = damage;
            _baseCriticalChance = baseCriticalChance;
            _rnd = new RandomNumberGenerator();
            _rnd.Randomize();
        }

        public float Damage => this[AbilityParameter.Damage];


        public override IAbility Copy()
        {
            var copy = new Fireball(Tags, (int)Cooldown, Damage, WeaponDamageScale, SpellDamageScale, _baseCriticalChance, CostValue, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override Task ExecuteInternal(List<IEntity> targets, IEntity owner, IBattleField field) => throw new System.NotImplementedException();

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, Damage);

        private float GetCurrentCriticalChance() => Owner == null
            ? _baseCriticalChance
            : Owner.Parameters.CalculateForBase(EntityParameter.CriticalChance, _baseCriticalChance);

        // Not sure about this. Modifiers will be apply twice. Once for entity spell damage parameter and once for ability spell damage
        private float GetCurrentDamage() => Owner == null
            ? _damage
            : Owner.Parameters.CalculateForBase(EntityParameter.SpellDamage, _damage);
    }
}
