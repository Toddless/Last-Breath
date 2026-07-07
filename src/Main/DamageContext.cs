namespace LastBreath
{
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Interfaces;
    using Godot;

    public record DamageContext : IDamageContext
    {
        private readonly Dictionary<DamageType, float> _damageComponents = [];
        public IReadOnlyDictionary<DamageType, float> DamageComponents => _damageComponents;
        public required IFightable Source { get; init; }
        public float TotalDamage => _damageComponents.Values.Sum();
        public DamageCause Cause { get; set; }
        public bool IsCrit { get; set; }
        public bool IgnoreResistances { get; set; }
        public string? SourceAbilityId { get; set; }
        public string? CastId { get; set; }
        public float AbsorbedByBarrier { get; set; }

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
