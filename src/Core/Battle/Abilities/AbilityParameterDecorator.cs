namespace Core.Battle.Abilities
{
    using System;
    using Enums;

    /// <summary>
    /// A single value transformation on top of an ability parameter (upgrades, temporary effects).
    /// Lives in the ability's <see cref="AbilityParameterSet"/>; chained by <see cref="Priority"/>.
    /// </summary>
    public abstract class AbilityParameterDecorator(string parameter, Priority priority, string id, string source)
    {
        public string Id { get; } = id;
        public string Source { get; } = source;
        public string Parameter { get; } = parameter;
        public Priority Priority { get; } = priority;

        /// <summary>What this decorator does, which is what makes two of them the same effect and
        /// therefore rivals rather than neighbours — see <see cref="AbilityEffectIdentity"/>.</summary>
        public AbilityEffectIdentity Identity => AbilityEffectIdentity.Of(this);

        /// <summary>Which way <see cref="Decorate"/> leaves the parameter. Declared rather than measured
        /// off a probe value: a share of a base of nothing comes to nothing, so a decorator asked what
        /// it did to a sample number could answer that it does nothing at all.</summary>
        public abstract AbilityEffectDirection Direction { get; }

        public abstract float Decorate(float value);

        /// <summary>
        /// Whether this decorator is the one of the two that works. Both are put to the parameter's own
        /// undecorated value and the one that leaves it furthest from where it started is the stronger:
        /// two decorators are only ever weighed against each other when they share an identity, so they
        /// move the same number the same way and further along is unambiguously more of the same thing.
        /// A flat cut and a cut stated as a share are answered alike, which is the point of reading the
        /// result instead of the amount.
        ///
        /// A categorical replacement has no distance worth measuring — nothing is more of another
        /// resource than something else is. The rivalry still has to name one winner, and it names the
        /// one whose id sorts first: an arbitrary rule, chosen because it is the same rule whatever
        /// order the player filled his sockets in. The same rule settles two moves of equal strength.
        /// </summary>
        /// <param name="other">The decorator of the same identity this one is measured against.</param>
        /// <param name="baseValue">The parameter's undecorated value — the common ground both are read on.</param>
        public bool IsStrongerThan(AbilityParameterDecorator other, float baseValue)
        {
            if (Direction == AbilityEffectDirection.Replace) return SortsBefore(other);

            float mine = Math.Abs(Decorate(baseValue) - baseValue);
            float theirs = Math.Abs(other.Decorate(baseValue) - baseValue);

            return mine == theirs ? SortsBefore(other) : mine > theirs;
        }

        private bool SortsBefore(AbilityParameterDecorator other) => string.CompareOrdinal(Id, other.Id) < 0;
    }
}
