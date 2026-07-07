namespace Battle.Source.Abilities.Fireball
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Enums;
    using Core.Localization;
    using Godot;

    public class Fireball : DamagingAbility
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

        protected override Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) => throw new System.NotImplementedException();

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
