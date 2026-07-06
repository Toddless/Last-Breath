namespace Core.Items
{
    using Enums;
    using Interfaces.Items;

    public class WeaponItem : EquipItem, IWeaponItem
    {
        public WeaponItem(WeaponType weaponType, Handedness handedness, float baseDamage, float criticalChance, float criticalDamage, string id, string[] tags)
            : base(EquipmentPiece.Weapon, id, tags)
        {
            WeaponType = weaponType;
            Handedness = handedness;
            BaseDamage = baseDamage;
            CriticalChance = criticalChance;
            CriticalDamage = criticalDamage;
        }

        protected WeaponItem(WeaponItem source) : base(source)
        {
            WeaponType = source.WeaponType;
            Handedness = source.Handedness;
            BaseDamage = source.BaseDamage;
            CriticalChance = source.CriticalChance;
            CriticalDamage = source.CriticalDamage;
        }

        public WeaponType WeaponType { get; }
        public Handedness Handedness { get; }
        public float BaseDamage { get; }
        public float CriticalChance { get; }
        public float CriticalDamage { get; }

        public float Damage
        {
            get
            {
                float flat = 0f, increase = 0f, multiplier = 0f;
                foreach (var modifier in AllModifiers)
                {
                    if (modifier.Scope != ModifierScope.Local || modifier.EntityParameter != EntityParameter.Damage) continue;
                    switch (modifier.ModifierValueType)
                    {
                        case ModifierValueType.Flat: flat += modifier.Value; break;
                        case ModifierValueType.Increase: increase += modifier.Value; break;
                        case ModifierValueType.Multiplicative: multiplier += modifier.Value; break;
                    }
                }

                return ((BaseDamage * UpdateMultiplier) + flat) * (1f + increase) * (1f + multiplier);
            }
        }

        protected override EquipItem CreateCopy() => new WeaponItem(this);

        protected override bool IsLocalBucketExternal(EntityParameter parameter) => parameter == EntityParameter.Damage;
    }
}
