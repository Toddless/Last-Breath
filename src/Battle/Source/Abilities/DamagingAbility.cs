namespace Battle.Source.Abilities
{
    using Core.Enums;
    using System.Collections.Generic;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components.Module;
    using Module;

    public abstract class DamagingAbility(
        string id,
        string[] tags,
        int cooldown,
        int costValue,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        Costs costsType = Costs.Mana)
        : Ability(id, tags, cooldown, costValue, costsType), IDamagingAbility
    {
        public float Damage => this[AbilityParameter.Damage];
        public float WeaponDamageScale => this[AbilityParameter.WeaponDamageScale];
        public float SpellDamageScale => this[AbilityParameter.SpellDamageScale];
        public bool IsEvadable { get; set; }

        protected override Dictionary<AbilityParameter, IParameterModule<AbilityParameter>> CreateBaseModules()
        {
            var moduleManager = base.CreateBaseModules();
            moduleManager[AbilityParameter.Damage] = new Module<AbilityParameter>(() => damage, AbilityParameter.Damage);
            moduleManager[AbilityParameter.WeaponDamageScale] = new Module<AbilityParameter>(() => weaponDamageScale, AbilityParameter.WeaponDamageScale);
            moduleManager[AbilityParameter.SpellDamageScale] = new Module<AbilityParameter>(() => spellDamageScale, AbilityParameter.SpellDamageScale);
            return moduleManager;
        }
    }
}
