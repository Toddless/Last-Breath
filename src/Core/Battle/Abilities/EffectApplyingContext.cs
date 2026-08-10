namespace Core.Battle.Abilities
{
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
        public float Damage { get; set; }
        public bool IsCritical { get; set; }
        public string Source { get; init; }

        /// <summary>Multiplier of the cast laying this (<see cref="AbilityParameter.Effectiveness"/>);
        /// one for a passive, an item grant or a boss stage.</summary>
        public float Effectiveness { get; init; } = 1f;

        /// <summary>Set on extra copies granted by application mutators — they skip the pipeline (see Effect.Apply).</summary>
        public bool IsBonusStack { get; init; }
    }
}
