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
        public DamageType DamageType { get; set; }
        public List<IFightable> Targets { get; set; } = [];
    }
}
