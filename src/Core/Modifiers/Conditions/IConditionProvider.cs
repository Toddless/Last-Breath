namespace Core.Modifiers.Conditions
{
    using Interfaces;

    /// <summary>
    /// The catalog of conditions: one definition per id, written once and named by everyone who needs
    /// it — a passive node, a line of an item. A consumer stores the id and nothing else, so "health
    /// below 30%" exists in the game exactly once and is edited in one place.
    /// </summary>
    public interface IConditionProvider
    {
        /// <summary>
        /// True when the id produced a predicate, or when there is no id at all (null or blank — the
        /// line is simply unconditional and <paramref name="condition"/> comes back null). False means
        /// the catalog holds nothing under that id: it has already been reported and the caller must
        /// DROP the whole line rather than let it apply unconditionally.
        /// A hit is a fresh unattached instance every time (<see cref="ICondition.Copy"/>): a predicate
        /// holds the state of one owner, so two consumers of one id must never share one.
        /// </summary>
        bool TryResolve(string? id, out ICondition? condition);
    }
}
