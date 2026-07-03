namespace LastBreath
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Entity;
    using Godot;
    using Utilities;

    public record DamageContext : IDamageContext
    {
        private readonly Dictionary<DamageType, float> _components = [];
        public IReadOnlyDictionary<DamageType, float> DamageComponents => _components;
        public required IFightable Source { get; init; }
        public float TotalDamage => _components.Values.Sum();
        public DamageCause Cause { get; set; }
        public bool IsCrit { get; set; } = false;
        public string? CastId { get; set; }
        public float AbsorbedByBarrier { get; set; }

        public void Add(DamageType type, float amount) => _components[type] = _components.GetValueOrDefault(type, 0f) + amount;

        /// <summary>Moves a fraction (0..1) of the CURRENT remaining <paramref name="from"/> component into <paramref name="to"/>.
        /// Sequential conversions therefore compound: two Convert(.., 0.5f) calls leave 25% of the original.</summary>
        public void Convert(DamageType from, DamageType to, float fraction)
        {
            if (!_components.TryGetValue(from, out float damageToConvert))
            {
                Tracker.TrackNotFound($"Damage component '{from}' to convert into '{to}'", this);
                return;
            }

            float converted = damageToConvert * Mathf.Clamp(fraction, 0f, 1f);
            _components[from] -= converted;
            _components[to] = _components.GetValueOrDefault(to, 0f) + converted;
        }

        public void Set(DamageType type, float amount) => _components[type] = amount;
    }
}
