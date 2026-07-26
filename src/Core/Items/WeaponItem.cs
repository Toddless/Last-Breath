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

        public float Damage => FoldAroundBase(EntityParameter.PhysicalDamage, BaseDamage);

        /// <summary>Every weapon base scales by the ONE multiplier (sharpening × ascension) — the
        /// owner's rule: the scales raise every numeric value, bases included — and every base folds
        /// its whole LOCAL bucket the same way: (base + flat) × (1 + inc) × (1 + multi). A local
        /// "+X% increased crit" therefore amplifies the weapon's own crit base, exactly like local
        /// damage lines always amplified the damage.</summary>
        public (float Base, float LocalBonus) GetStatBreakdown(EntityParameter parameter)
        {
            float rawBase = parameter switch
            {
                EntityParameter.PhysicalDamage => BaseDamage,
                EntityParameter.CriticalChance => CriticalChance,
                EntityParameter.CriticalDamage => CriticalDamage,
                _ => float.NaN,
            };
            if (float.IsNaN(rawBase)) return (0f, 0f);

            float scaledBase = rawBase * LineMultiplier;
            return (scaledBase, FoldAroundBase(parameter, rawBase) - scaledBase);
        }

        private float FoldAroundBase(EntityParameter parameter, float baseValue)
        {
            (float flat, float increase, float multiplier) = LocalBucket(parameter);
            return ((baseValue * LineMultiplier) + flat) * (1f + increase) * (1f + multiplier);
        }

        protected override EquipItem CreateCopy() => new WeaponItem(this);

        // The weapon triple consumes its own local buckets (the folds above) — none of the three
        // may ALSO resolve into synthetic flats, or every local line would land twice.
        protected override bool IsLocalBucketExternal(EntityParameter parameter) =>
            parameter is EntityParameter.PhysicalDamage or EntityParameter.CriticalChance or EntityParameter.CriticalDamage
            || base.IsLocalBucketExternal(parameter);
    }
}
