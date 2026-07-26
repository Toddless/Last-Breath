namespace Core.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>Identity of a player-facing item line for duplicate control — WHAT the line is about, never
    /// the rolled values. An atom is a single <see cref="ModifierKey"/>; a composite is the sorted multiset
    /// of its parts' keys ("+X% Mana, +Y% Health" is the same line whatever the numbers roll, and a one-part
    /// composite reads exactly like the atom, so it deliberately shares the atom's identity). Multi-part
    /// bundles keep the old exemption implicitly: their identity is the WHOLE set, so a part never blocks a
    /// standalone atom and vice versa — only a full set match is a duplicate. Grants and operation entries
    /// are not lines and have no identity. Future conditional lines extend this with their condition.</summary>
    public sealed class LineIdentity : IEquatable<LineIdentity>
    {
        private readonly ModifierKey[] _keys; // sorted: identity is a multiset, part order is presentation
        private readonly int _hash;

        private LineIdentity(ModifierKey[] sortedKeys)
        {
            _keys = sortedKeys;
            var hash = new HashCode();
            foreach (var key in sortedKeys) hash.Add(key);
            _hash = hash.ToHashCode();
        }

        public static LineIdentity Of(ModifierKey key) => new([key]);

        public static LineIdentity OfKeys(IEnumerable<ModifierKey> keys) =>
            new(keys.OrderBy(key => key.Channel).ThenBy(key => key.Parameter).ThenBy(key => key.ValueType).ToArray());

        /// <summary>Identity of a pool entry; false when the entry does not materialize into lines —
        /// a grant, an operation, or a composite hiding one of those among its parts. Every roll that
        /// swaps or adds LINES must gate its candidates through this.</summary>
        public static bool TryFrom(IModifierDescriptor descriptor, out LineIdentity identity)
        {
            var keys = new List<ModifierKey>();
            if (!Collect(descriptor, keys) || keys.Count == 0)
            {
                identity = null!;
                return false;
            }

            identity = OfKeys(keys);
            return true;
        }

        public bool Equals(LineIdentity? other) =>
            other != null && _hash == other._hash && _keys.AsSpan().SequenceEqual(other._keys);

        public override bool Equals(object? obj) => obj is LineIdentity other && Equals(other);

        public override int GetHashCode() => _hash;

        public override string ToString() => string.Join("|", _keys);

        private static bool Collect(IModifierDescriptor descriptor, List<ModifierKey> keys)
        {
            if (descriptor is CompositeDescriptor composite)
                return composite.Parts.All(part => Collect(part, keys));

            if (!ModifierKey.TryFrom(descriptor, out var key)) return false;
            keys.Add(key);
            return true;
        }
    }
}
