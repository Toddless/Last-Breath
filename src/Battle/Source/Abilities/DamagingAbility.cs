namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;

    public abstract class DamagingAbility(AbilityBaseData data) : Ability(data), IDamagingAbility
    {
        public float Damage => this[AbilityParameter.Damage];
        public float WeaponDamageScale => this[AbilityParameter.WeaponDamageScale];
        public float SpellDamageScale => this[AbilityParameter.SpellDamageScale];
        // True by design: an evade interrupts an attack series; the "unevadable" upgrades set it to false.
        public bool IsEvadable { get; set; } = true;

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            RegisterDamageParameters(parameters);
        }
    }
}
