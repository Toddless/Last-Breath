namespace Core.Interfaces
{
    using System.Collections.Generic;
    using Entity;
    using Enums;

    public interface IDamageContext
    {
        /// <summary>Damage split by type. Populate via <see cref="Add"/>; mitigation rewrites values via <see cref="Set"/>.</summary>
        IReadOnlyDictionary<DamageType, float> DamageComponents { get; }

        IFightable Source { get; }

        /// <summary>Sum of all damage components.</summary>
        float TotalDamage { get; }

        DamageCause Cause { get; set; }
        bool IsCrit { get; set; }

        /// <summary>Amount of this hit that was soaked by the target's barrier. Health damage = <see cref="TotalDamage"/> - this.</summary>
        float AbsorbedByBarrier { get; set; }

        void Add(DamageType type, float amount);
        void Convert(DamageType from, DamageType to, float fraction);
        void Set(DamageType type, float amount);
    }
}
