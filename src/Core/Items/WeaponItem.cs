namespace Core.Items
{
    using Enums;

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
                (float flat, float increase, float multiplier) = LocalBucket(EntityParameter.Damage);
                return ((BaseDamage * UpdateMultiplier) + flat) * (1f + increase) * (1f + multiplier);
            }
        }

        /// <summary>Damage folds its whole local bucket around the sharpened base (see <see cref="Damage"/>);
        /// the crit pair receives exactly the resolved flat the owner would get from the item's lines.</summary>
        public (float Base, float LocalBonus) GetStatBreakdown(EntityParameter parameter) => parameter switch
        {
            EntityParameter.Damage => (BaseDamage * UpdateMultiplier, Damage - (BaseDamage * UpdateMultiplier)),
            EntityParameter.CriticalChance => (CriticalChance, ResolvedLocalFlat(parameter)),
            EntityParameter.CriticalDamage => (CriticalDamage, ResolvedLocalFlat(parameter)),
            _ => (0f, 0f),
        };

        protected override EquipItem CreateCopy() => new WeaponItem(this);

        protected override bool IsLocalBucketExternal(EntityParameter parameter) => parameter == EntityParameter.Damage;
    }
}
