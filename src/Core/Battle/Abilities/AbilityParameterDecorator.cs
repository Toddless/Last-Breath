namespace Core.Battle.Abilities
{
    using System;
    using Enums;

    /// <summary>
    /// A single value transformation on top of an ability parameter (upgrades, temporary effects).
    /// Lives in the ability's <see cref="AbilityParameterSet"/>; chained by <see cref="Priority"/>.
    /// </summary>
    /// <param name="rank">What the record behind this move is worth when the move itself cannot be
    /// weighed — the tier of the augment that laid it. Read ONLY where there is no distance to measure
    /// (see <see cref="AbilityEffectDirection.Replace"/>); everywhere else the amount decides and the
    /// tier has no business in it. Nought for anything that arrives without one.</param>
    public abstract class AbilityParameterDecorator(string parameter, Priority priority, string id, string source, int rank = 0)
    {
        public string Id { get; } = id;
        public string Source { get; } = source;
        public string Parameter { get; } = parameter;
        public Priority Priority { get; } = priority;

        /// <summary>The tier of the record this move came from; see the constructor.</summary>
        public int Rank { get; } = rank;

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
        /// resource than something else is. What the player can see instead is what he paid: the deeper
        /// tier is the rarer record and the scarcer slot, so a tier-three swap outranks a tier-two one
        /// and the answer means something to the man reading his own board. Records of one tier are
        /// settled by the id, which decides nothing about them except that it decides the same thing
        /// whatever order the sockets were filled in. The id also settles two measurable moves that
        /// turned out to be exactly as strong as each other; the tier stays out of that, because there
        /// the amount is the whole question and a tier tie-break would sell strength by price.
        /// </summary>
        /// <param name="other">The decorator of the same identity this one is measured against.</param>
        /// <param name="baseValue">The parameter's undecorated value — the common ground both are read on.</param>
        public bool IsStrongerThan(AbilityParameterDecorator other, float baseValue)
        {
            if (Direction == AbilityEffectDirection.Replace) return Outranks(other);

            float mine = Math.Abs(Decorate(baseValue) - baseValue);
            float theirs = Math.Abs(other.Decorate(baseValue) - baseValue);

            return mine == theirs ? SortsBefore(other) : mine > theirs;
        }

        /// <summary>The deeper tier, and the id where the tiers are the same.</summary>
        private bool Outranks(AbilityParameterDecorator other) =>
            Rank == other.Rank ? SortsBefore(other) : Rank > other.Rank;

        private bool SortsBefore(AbilityParameterDecorator other) => string.CompareOrdinal(Id, other.Id) < 0;
    }
}
