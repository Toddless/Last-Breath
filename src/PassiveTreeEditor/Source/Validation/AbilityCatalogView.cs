namespace PassiveTreeEditor.Source.Validation
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The two answers the rules need about an ability id: whether the catalog holds it at all, and
    /// whether a tree node is allowed to name it. Handed to the validator instead of read there, so
    /// the rules stay a function of their arguments — checking them needs two lists of strings and no
    /// data pipeline, no catalog and no Godot.
    /// </summary>
    public sealed class AbilityCatalogView
    {
        private readonly HashSet<string> _known;
        private readonly HashSet<string> _selectable;

        /// <param name="known">Every ability id the catalog holds, hidden ones included.</param>
        /// <param name="selectable">The subset a tree node may reference.</param>
        public AbilityCatalogView(IEnumerable<string> known, IEnumerable<string> selectable)
        {
            _known = new HashSet<string>(known, StringComparer.Ordinal);
            _selectable = new HashSet<string>(selectable, StringComparer.Ordinal);
        }

        /// <summary>Nothing was read: every reference would be reported as unknown, which says more
        /// about the tool's data than about the tree, so the ability rules stand aside.</summary>
        public bool IsEmpty => _known.Count == 0;

        public bool Knows(string abilityId) => _known.Contains(abilityId);

        public bool CanBeReferenced(string abilityId) => _selectable.Contains(abilityId);
    }
}
