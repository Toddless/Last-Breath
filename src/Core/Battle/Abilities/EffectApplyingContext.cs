namespace Core.Battle.Abilities
{
    using Context;
    using Entity;

    public struct EffectApplyingContext
    {
        /// <summary>Exists so <see cref="Effectiveness"/> starts at one: a multiplier left at the struct
        /// default would zero every number of every effect.</summary>
        public EffectApplyingContext()
        {
        }

        public IFightable Caster { get; init; }
        public IFightable Target { get; set; }
        /// <summary>The blow this application feeds on, split by type: a damage-over-time effect takes its
        /// pool from the component of its own kind (see DamageOverTurnEffect).</summary>
        public DamageSnapshot Damage { get; set; }

        /// <summary>Says the pool is the WHOLE blow, whatever component the effect's kind would otherwise
        /// feed on — for sources whose figure stands for the entire hit rather than for one kind of it.</summary>
        public bool PoolFromWholeHit { get; init; }
        public bool IsCritical { get; set; }
        public string Source { get; init; }

        /// <summary>Multiplier of the cast laying this (<see cref="AbilityParameter.Effectiveness"/>);
        /// one for a passive, an item grant or a boss stage.</summary>
        public float Effectiveness { get; init; } = 1f;

        /// <summary>Set on extra copies granted by application mutators — they skip the pipeline (see Effect.Apply).</summary>
        public bool IsBonusStack { get; init; }
    }
}
