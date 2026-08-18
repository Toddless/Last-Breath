namespace Core.Context
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enums;

    /// <summary>Damage of one blow frozen by type — what a damage-over-time pool is taken from after the
    /// hit resolved. <see cref="Total"/> is summed once, at capture.</summary>
    public readonly struct DamageSnapshot : IEquatable<DamageSnapshot>
    {
        private static readonly Dictionary<DamageType, float> s_empty = [];
        private readonly IReadOnlyDictionary<DamageType, float>? _components;

        /// <summary>Sum of every component of the blow.</summary>
        public float Total { get; }

        /// <summary>What the blow carried of one type; nothing when it carried none.</summary>
        public float this[DamageType type] => _components?.GetValueOrDefault(type) ?? 0f;

        private DamageSnapshot(IReadOnlyDictionary<DamageType, float> components)
        {
            _components = components;
            Total = components.Values.Sum();
        }

        /// <summary>Freezes what a resolved context carries: the caller may keep reshaping it afterwards.</summary>
        public static DamageSnapshot From(IDamageContext context) => From(context.DamageComponents);

        /// <summary>Freezes a split the caller owns — the components are copied, not borrowed.</summary>
        public static DamageSnapshot From(IReadOnlyDictionary<DamageType, float> components) =>
            new(new Dictionary<DamageType, float>(components));

        /// <summary>A blow of a single type — the form a figure authored by the source itself takes.</summary>
        public static DamageSnapshot Of(DamageType type, float amount) =>
            new(new Dictionary<DamageType, float> { [type] = amount });

        public static bool operator ==(DamageSnapshot left, DamageSnapshot right) => left.Equals(right);

        public static bool operator !=(DamageSnapshot left, DamageSnapshot right) => !left.Equals(right);

        /// <summary>Component by component: two snapshots of the same split are the same blow, whichever
        /// dictionaries they were built over. Records carrying one (an impact, a projectile hit) compare
        /// through this, so a reference comparison would call equal blows different.</summary>
        public bool Equals(DamageSnapshot other)
        {
            IReadOnlyDictionary<DamageType, float> mine = _components ?? s_empty;
            IReadOnlyDictionary<DamageType, float> theirs = other._components ?? s_empty;
            return mine.Count == theirs.Count
                   && mine.All(part => theirs.TryGetValue(part.Key, out float amount) && amount.Equals(part.Value));
        }

        public override bool Equals(object? obj) => obj is DamageSnapshot other && Equals(other);

        /// <summary>Combined without order, the way <see cref="Equals"/> reads the components. The sum
        /// cannot serve: it is added in whatever order a dictionary enumerates, so two equal splits can
        /// part on the last bit and hash apart.</summary>
        public override int GetHashCode() =>
            (_components ?? s_empty).Aggregate(0, (hash, part) => hash ^ HashCode.Combine(part.Key, part.Value));
    }
}
