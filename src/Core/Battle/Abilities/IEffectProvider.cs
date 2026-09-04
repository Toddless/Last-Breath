namespace Core.Battle.Abilities
{
    using System.Collections.Generic;

    /// <summary>
    /// The one place an effect is built from an id and numbers out of data — item grants today, catalog
    /// records next. A factory DECLARES the property keys it reads, so a typo in data is refused with
    /// names instead of quietly producing a mis-tuned effect. See <c>Docs/PLAN-Augments.md §4b</c>.
    /// <para>A registry is also a reading of the canon (<see cref="IEffectCanonCatalog"/>): the figures it
    /// builds an effect with and the figures a description prints are the same rows. Stated as inheritance
    /// so a surface that only READS those rows — a card, an authoring tool — asks the canon and is served
    /// by either, instead of the registry restating the same three questions.</para>
    /// </summary>
    public interface IEffectProvider : IEffectCanonCatalog
    {
        /// <summary>Effect ids the provider can build — what a record may name.</summary>
        IReadOnlyCollection<string> KnownIds { get; }

        /// <summary>Property keys the effect is built from, or null when no factory answers the id.</summary>
        IReadOnlyCollection<string>? KeysOf(string effectId);

        /// <summary>The effect, or null with a report: unknown id, unknown key or missing key.</summary>
        IEffect? CreateEffect(string id, RecordProperties properties);
    }
}
