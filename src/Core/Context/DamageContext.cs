namespace Core.Context
{
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Entity;
    using Enums;
    using Godot;

    public record DamageContext : IDamageContext
    {
        private readonly Dictionary<DamageType, float> _damageComponents = [];

        /// <summary>What this one blow pierces on top of the source's own parameters. Empty for the hits
        /// nobody wrote a per-blow rule for, which is nearly all of them.</summary>
        private readonly Dictionary<EntityParameter, float> _resistancePenetration = [];
        private IFightable? _target;
        public IReadOnlyDictionary<DamageType, float> DamageComponents => _damageComponents;
        public required IFightable Source { get; init; }

        /// <summary>Unnamed until the hit reaches a fighter, and until then it is the source's own.</summary>
        public IFightable Target
        {
            get => _target ?? Source;
            set => _target = value;
        }

        public float TotalDamage => _damageComponents.Values.Sum();
        public DamageCause Cause { get; set; }
        public bool IsCrit { get; set; }
        public bool IgnoreResistances { get; set; }
        public bool IgnoreBarrier { get; set; }
        public string? CastId { get; set; }
        public string? SourceAbilityId { get; set; }
        public float AbsorbedByBarrier { get; set; }
        public float AbsorbedByShield { get; set; }
        public float PreventedByStageGuard { get; set; }

        public float ResistancePenetrationOf(EntityParameter penetration) => _resistancePenetration.GetValueOrDefault(penetration);

        public void AddResistancePenetration(EntityParameter penetration, float amount) =>
            _resistancePenetration[penetration] = ResistancePenetrationOf(penetration) + amount;

        public void Add(DamageType type, float amount) => _damageComponents[type] = _damageComponents.GetValueOrDefault(type, 0f) + amount;

        public void Set(DamageType type, float amount) => _damageComponents[type] = amount;

        /// <summary>Moves a fraction (0..1) of the CURRENT remaining <paramref name="from"/> component into <paramref name="to"/>.
        /// Sequential conversions therefore compound: two Convert(.., 0.5f) calls leave 25% of the original.</summary>
        public void Convert(DamageType from, DamageType to, float fraction)
        {
            if (!_damageComponents.TryGetValue(from, out float damageToConvert))
            {
                Tracker.TrackNotFound($"Damage component '{from}' to convert into '{to}'", this);
                return;
            }

            float converted = damageToConvert * Mathf.Clamp(fraction, 0f, 1f);
            _damageComponents[from] -= converted;
            _damageComponents[to] = _damageComponents.GetValueOrDefault(to, 0f) + converted;
        }
    }
}
