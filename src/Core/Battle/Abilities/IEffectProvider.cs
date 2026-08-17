namespace Core.Battle.Abilities
{
    using System.Collections.Generic;
    using Skills;

    /// <summary>
    /// The one place an effect is built from an id and numbers out of data — item grants today, catalog
    /// records next. A factory DECLARES the property keys it reads, so a typo in data is refused with
    /// names instead of quietly producing a mis-tuned effect. See <c>Docs/PLAN-Augments.md §4b</c>.
    /// </summary>
    public interface IEffectProvider
    {
        /// <summary>Effect ids the provider can build — what a record may name.</summary>
        IReadOnlyCollection<string> KnownIds { get; }

        /// <summary>Property keys the effect is built from, or null when no factory answers the id.</summary>
        IReadOnlyCollection<string>? KeysOf(string effectId);

        /// <summary>The effect, or null with a report: unknown id, unknown key or missing key.</summary>
        IEffect? CreateEffect(string id, RecordProperties properties);

        /// <summary>How many stacks of an effect the canon balances it at, or null for an id the canon
        /// deliberately leaves out — a state of an ability rather than a figure anybody balances (§4f).
        /// What a cast LAYS is held to this, however high a record drives the ability's own stacks key.</summary>
        int? StackCeilingOf(string effectId);
    }
}
