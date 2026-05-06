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
            Dictionary<int, List<IAbilityUpgrade>> upgrades,
            Costs costType = Costs.Mana) : base(id: "Ability_Fireball", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, upgrades, costType)
        {
            _damage = damage;
            _baseCriticalChance = baseCriticalChance;
            _rnd = new RandomNumberGenerator();
            _rnd.Randomize();
        }

        public float Damage => this[AbilityParameter.Damage];


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
