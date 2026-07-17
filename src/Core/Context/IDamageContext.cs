namespace Core.Context
{
    using System.Collections.Generic;
    using Enums;
    using Entity;

    public interface IDamageContext
    {
        /// <summary>Damage split by type. Populate via <see cref="Add"/>; mitigation rewrites values via <see cref="Set"/>.</summary>
        IReadOnlyDictionary<DamageType, float> DamageComponents { get; }

        IFightable Source { get; }

        /// <summary>Sum of all damage components.</summary>
        float TotalDamage { get; }

        DamageCause Cause { get; set; }
        bool IsCrit { get; set; }

        /// <summary>Elemental resistances (Fire/Cold/Lightning) are skipped in mitigation for this hit.</summary>
        bool IgnoreResistances { get; set; }

        /// <summary>Id of the ability whose ATTACK produced this damage (null otherwise) — presentation plays its impact VFX.</summary>
        string? SourceAbilityId { get; set; }

        /// <summary>
        /// Groups hits of one ability activation for presentation: the BattleDirector plays
        /// entries sharing a CastId as one parallel "chord". Null = ungrouped, plays sequentially.
        /// </summary>
        string? CastId { get; set; }

        /// <summary>Amount of this hit that was soaked by the target's barrier. Health damage = <see cref="TotalDamage"/> - this.</summary>
        float AbsorbedByBarrier { get; set; }

        /// <summary>Damage eaten by an <see cref="Battle.Abilities.IShieldEffect"/> layer (before the barrier).</summary>
        float AbsorbedByShield { get; set; }

        void Add(DamageType type, float amount);
        void Convert(DamageType from, DamageType to, float fraction);
        void Set(DamageType type, float amount);
    }
}
