namespace Battle.Source.Abilities
{
    using System.Collections.Generic;
    using Core.Entity;
    using Core.Enums;

    /// <summary>
    /// Mutable plan of a multicast cast. The base stage fills it from the ability's current
    /// parameters; every activation stage below the rolled one mutates it (numbers, targets, etc).
    /// </summary>
    public abstract class DamagingCastPlan
    {
        public float Damage { get; set; }
        public float WeaponDamageScale { get; set; }
        public float SpellDamageScale { get; set; }
        /// <summary>Bucket every hit of the plan lands in. Required rather than defaulted: a plan that
        /// forgot to name its type would pool damage into <c>(DamageType)0</c>, which no rule mitigates.</summary>
        public required DamageType DamageType { get; set; }
        public List<IFightable> Targets { get; set; } = [];

        /// <summary>Every hit of the plan skips elemental resistances.</summary>
        public bool IgnoreResistances { get; set; }

        /// <summary>Only CRITICAL hits of the plan skip elemental resistances (an Ice Shards augment).</summary>
        public bool CritIgnoresResistances { get; set; }
    }
}
