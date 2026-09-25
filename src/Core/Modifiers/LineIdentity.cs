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
    /// standalone atom and vice versa — only a full set match is a duplicate. The predicate a line is held
    /// up by is part of every key it is built from, so "+10% armor" and "+10% armor while wounded" are two
    /// lines: one item may wear both and a reroll may trade one for the other. Grants and operation entries
    /// are not lines and have no identity.</summary>
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
            new(keys.OrderBy(key => key.Channel).ThenBy(key => key.Parameter).ThenBy(key => key.ValueType)
                .ThenBy(key => key.Condition, StringComparer.Ordinal).ToArray());

        /// <summary>Identity of a pool entry; false when the entry does not materialize into lines —
        /// a grant, an operation, or a composite hiding one of those among its parts. Every roll that
        /// swaps or adds LINES must gate its candidates through this.</summary>
        public static bool TryFrom(IModifierDescriptor descriptor, out LineIdentity identity)
        {
            var keys = new List<ModifierKey>();
            if (!Collect(descriptor, keys, null) || keys.Count == 0)
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

        /// <summary>Gathers the keys the entry's lines will be born with. A composite's predicate sits on its
        /// root and is stamped onto every part when the bundle is minted, so it travels down here too — the
        /// keys of a gated bundle must be the ones its worn parts answer by, or the item would wear a line
        /// the pool no longer recognizes as the same. The stamp of the OUTERMOST root wins all the way down,
        /// exactly as the mint applies it: one bundle is one line and one line has one gate.</summary>
        private static bool Collect(IModifierDescriptor descriptor, List<ModifierKey> keys, string? condition)
        {
            if (descriptor is CompositeDescriptor composite)
                return composite.Parts.All(part => Collect(part, keys, condition ?? composite.Condition));

            if (!ModifierKey.TryFrom(descriptor, condition, out var key)) return false;
            keys.Add(key);
            return true;
        }
    }
}
