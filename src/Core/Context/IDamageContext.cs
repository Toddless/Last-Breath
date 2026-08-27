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

        /// <summary>The fighter the hit lands on. Named by <see cref="Calculations.ApplyDamageModifiers"/>,
        /// the one point every damage road passes, so a line about damage TAKEN reads its receiver instead of
        /// inferring him from the source — an inference self-inflicted damage always answered wrong.
        /// Until a hit is handed to a fighter it belongs to nobody but the one who made it.</summary>
        IFightable Target { get; set; }

        /// <summary>Sum of all damage components.</summary>
        float TotalDamage { get; }

        DamageCause Cause { get; set; }
        bool IsCrit { get; set; }

        /// <summary>Elemental resistances (Fire/Cold/Lightning) are skipped in mitigation for this hit.</summary>
        bool IgnoreResistances { get; set; }

        /// <summary>The target's barrier does not absorb this hit (the Soulless passive).</summary>
        bool IgnoreBarrier { get; set; }

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

        /// <summary>Overkill prevented by a staged boss's transition floor (anti-oneshot).</summary>
        float PreventedByStageGuard { get; set; }

        /// <summary>Resistance penetration THIS hit carries, beyond what the source's own parameter holds,
        /// asked for by the penetration parameter answering the resistance. Mitigation adds the two and caps
        /// the sum, so a rule written for one blow and a line the fighter always wears reach the target's
        /// resistance as one figure rather than two.</summary>
        float ResistancePenetrationOf(EntityParameter penetration);

        /// <summary>Adds to what this hit penetrates, additively — the way the parameter it joins is built.</summary>
        void AddResistancePenetration(EntityParameter penetration, float amount);

        void Add(DamageType type, float amount);
        void Convert(DamageType from, DamageType to, float fraction);
        void Set(DamageType type, float amount);
    }
}
