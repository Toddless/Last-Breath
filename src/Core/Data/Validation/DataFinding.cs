namespace Core.Data.Validation
{
    /// <summary>
    /// What one rule over the game's own catalogs has to say. Every kind is a fact of a different sort,
    /// because what an author does about one is different: a knob written two ways is one bonus an item
    /// can end up wearing twice, a filter nothing answers is a kill that quietly hands out one item
    /// fewer, and a repeatable quest promising an artefact is a second copy of a thing the world holds
    /// exactly one of.
    /// </summary>
    /// <remarks>These are the rules that need the MEANING of the data and not only its shape — a value
    /// type the pipeline ignores but the duplicate rule counts, a rarity scale that runs downward, an
    /// item catalog that says which ids are one of a kind. The rules over the shape alone belong to
    /// whoever reads the schemas.</remarks>
    public enum DataFindingKind
    {
        /// <summary>One context knob is written in more than one value type across the pools an item's
        /// lines are rolled from. The binding is picked by the parameter and reads the number, so both
        /// entries do the very same thing — but a line's identity counts the value type, so the duplicate
        /// rule sees two lines, an item may roll both, and the two bonuses multiply.</summary>
        SplitValueType,

        /// <summary>A loot position names a set of augments by a filter, and no shipped record declares a
        /// band covering it. The set is drawn when the item is minted rather than at load, so the seat
        /// takes its price out of the budget and hands nothing back, for the rest of the game.</summary>
        UnansweredAugmentBand,

        /// <summary>A repeatable quest hands out a one-of-a-kind item: every turn-in mints another copy
        /// of an artefact the world was written to hold one of.</summary>
        RepeatedUniqueReward,

        /// <summary>A list the record is read by is written empty, and nothing else in the record answers
        /// for what it was to say. The reader takes such a record all the same and falls back to a default
        /// nobody chose, so the record ships meaning something other than what it says.</summary>
        EmptyRequiredList,

        /// <summary>A quest hands out an id no item catalog declares. The mint has nothing to build, and
        /// the uniqueness rule cannot judge an id it does not know — an unrecognised reward must not pass
        /// by being unrecognised.</summary>
        UndeclaredReward,
    }

    /// <summary>
    /// One thing a rule found: what sort it is, the catalog and the record it belongs to, the word it is
    /// about, the address it stands at, and what is wrong there.
    /// </summary>
    /// <param name="Catalog">The catalog the finding is in; empty for one about the run itself — a knob
    /// split across two catalogs belongs to neither.</param>
    /// <param name="Record">The id of the record it stands in; empty where it belongs to no record.</param>
    /// <param name="Named">The word the finding is about — the knob, the filter, the item handed over.</param>
    /// <param name="Where">Where it stands, for a finding that stands anywhere at all.</param>
    /// <remarks>Shaped after the finding of the shape rules on purpose: an authoring tool lists both sets
    /// in one panel, and two shapes would be two ways of reading one row.</remarks>
    public sealed record DataFinding(
        DataFindingKind Kind,
        string Catalog,
        string Record,
        string Named,
        string Where,
        string Message);
}
